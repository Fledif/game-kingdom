using System;
using System.Collections.Generic;
using System.Drawing;
using IsometricWorld.Core.Rendering;
using IsometricWorld.Core.World;
using IsometricWorld.Core.Generation;

namespace IsometricWorld.Core
{
    public sealed class WorldEngine
    {
        public ChunkManager ChunkManager { get; private set; } = null!;
        public IsometricRenderer Renderer { get; private set; } = null!;
        public IsometricCamera Camera { get; private set; } = null!;

        private const int ChunkLoadRadius = 2;
        private Bitmap _cachedGlobalMap = null;

        public PlayerEntity Player { get; private set; } = null!;

        public void Initialize(IGraphicsDevice graphicsDevice, int chunkSize, int viewportWidth, int viewportHeight)
        {
            ChunkManager = new ChunkManager(chunkSize);
            Renderer = new IsometricRenderer(graphicsDevice);
            Camera = new IsometricCamera(viewportWidth, viewportHeight);
        }

        public void InitializePlayer(string spritesFolder, int framesPerDirection, float startX, float startY)
        {
            var animator = new CharacterAnimator(spritesFolder, framesPerDirection);
            Player = new PlayerEntity(startX, startY, animator);
        }

        public void Update(float deltaTime)
        {
            float centerScreenX = Camera.X + Camera.ViewportWidth / 2f;
            float centerScreenY = Camera.Y + Camera.ViewportHeight / 2f;

            float worldXf = (centerScreenX / IsometricRenderer.HalfWidth + centerScreenY / IsometricRenderer.HalfHeight) / 2f;
            float worldYf = (centerScreenY / IsometricRenderer.HalfHeight - centerScreenX / IsometricRenderer.HalfWidth) / 2f;

            int worldX = (int)Math.Floor(worldXf);
            int worldY = (int)Math.Floor(worldYf);

            int centerChunkX = FloorDiv(worldX, ChunkManager.ChunkSize);
            int centerChunkY = FloorDiv(worldY, ChunkManager.ChunkSize);

            var activeChunks = new HashSet<(int, int)>();

            for (int cy = centerChunkY - ChunkLoadRadius; cy <= centerChunkY + ChunkLoadRadius; cy++)
            {
                for (int cx = centerChunkX - ChunkLoadRadius; cx <= centerChunkX + ChunkLoadRadius; cx++)
                {
                    activeChunks.Add((cx, cy));
                    
                    if (!ChunkManager.IsChunkLoaded(cx, cy))
                    {
                        ChunkManager.LoadChunk(cx, cy);
                    }
                }
            }

            var chunksToUnload = new List<(int cx, int cy)>();
            
            foreach (var key in ChunkManager.GetLoadedChunkKeys())
            {
                if (!activeChunks.Contains(key))
                {
                    chunksToUnload.Add(key);
                }
            }

            foreach (var key in chunksToUnload)
            {
                ChunkManager.UnloadChunk(key.cx, key.cy);
            }
        }

        public void ClearGlobalMapCache()
        {
            _cachedGlobalMap?.Dispose();
            _cachedGlobalMap = null;
        }

        public bool TryAddBuilding(float worldX, float worldY, float radius = 1.0f)
        {
            foreach (var b in ChunkManager.GetLoadedBuildings())
            {
                float dx = b.WorldX - worldX;
                float dy = b.WorldY - worldY;
                float distSq = dx * dx + dy * dy;
                float minDist = b.Radius + radius;

                if (distSq < minDist * minDist)
                {
                    return false;
                }
            }

            ChunkManager.AddBuilding(new FreeBuilding { WorldX = worldX, WorldY = worldY, Radius = radius });
            return true;
        }

        public void Draw(IGraphicsDevice graphics)
        {
            var activeBuildings = new List<FreeBuilding>(ChunkManager.GetLoadedBuildings());
            Renderer.DrawWorld(ChunkManager, activeBuildings, (int)Camera.X, (int)Camera.Y, Camera.ViewportWidth, Camera.ViewportHeight);

            if (Player != null)
            {
                var g = Renderer.GetGraphics();
                if (g != null)
                {
                    Player.Draw(g, (wx, wy) =>
                    {
                        float sx = (wx - wy) * IsometricRenderer.HalfWidth - Camera.X;
                        float sy = (wx + wy) * IsometricRenderer.HalfHeight - Camera.Y;
                        return new PointF(sx, sy);
                    });

                    string debugText = $"Facing: {Player.Animator.Facing}";
                    using var font = new Font("Consolas", 14, FontStyle.Bold);
                    using var bg = new SolidBrush(Color.FromArgb(180, 0, 0, 0));
                    using var fg = new SolidBrush(Color.Yellow);
                    var size = g.MeasureString(debugText, font);
                    g.FillRectangle(bg, 10, 10, size.Width + 10, size.Height + 6);
                    g.DrawString(debugText, font, fg, 15, 13);
                }
            }
        }

        public void DrawGlobalMap(Graphics g, int screenWidth, int screenHeight)
        {
            int mapResolution = 1000;
            int step = WorldGenerator.WorldSize / mapResolution;

            if (_cachedGlobalMap == null)
            {
                _cachedGlobalMap = new Bitmap(mapResolution, mapResolution);

                using (Graphics bg = Graphics.FromImage(_cachedGlobalMap))
                {
                    bg.Clear(Color.Black);
                    for (int py = 0; py < mapResolution; py++)
                    {
                        for (int px = 0; px < mapResolution; px++)
                        {
                            int worldX = px * step;
                            int worldY = py * step;

                            var data = WorldGenerator.GetTileDataAt(worldX, worldY);
                            Brush brush = data.type switch
                            {
                                TileType.Ocean => Brushes.Blue,
                                TileType.River => Brushes.DeepSkyBlue,
                                TileType.Beach => Brushes.Yellow,
                                TileType.Plains => Brushes.LimeGreen,
                                TileType.Forest => Brushes.Green,
                                TileType.Mountain => Brushes.Gray,
                                _ => Brushes.Magenta
                            };

                            bg.FillRectangle(brush, px, py, 1, 1);
                        }
                    }
                }
            }

            g.DrawImage(_cachedGlobalMap, 0, 0, screenWidth, screenHeight);

            float centerScreenX = Camera.X + screenWidth / 2f;
            float centerScreenY = Camera.Y + screenHeight / 2f;

            float worldXf = (centerScreenX / IsometricRenderer.HalfWidth + centerScreenY / IsometricRenderer.HalfHeight) / 2f;
            float worldYf = (centerScreenY / IsometricRenderer.HalfHeight - centerScreenX / IsometricRenderer.HalfWidth) / 2f;

            float dotX = worldXf / (float)step;
            float dotY = worldYf / (float)step;

            int camX = (int)((dotX / mapResolution) * screenWidth);
            int camY = (int)((dotY / mapResolution) * screenHeight);

            int camSize = 10;
            g.FillEllipse(Brushes.Red, camX - camSize / 2, camY - camSize / 2, camSize, camSize);
        }

        private static int FloorDiv(int a, int b)
        {
            int q = a / b;
            return q + (((a ^ b) >> 31) & (((a - q * b) != 0) ? 1 : 0)) * -1;
        }
    }
}
