// Розробник А: Гриценко Богдан Олександрович
// Проєкт: Custom C# Engine (Free-form Building & UI)

using System;
using System.Collections.Generic;
using System.Drawing;
using IsometricWorld.Core.Rendering;
using IsometricWorld.Core.World;
using IsometricWorld.Core.Generation;

namespace IsometricWorld.Core
{
    public struct FreeBuilding
    {
        public float WorldX;
        public float WorldY;
    }

    public sealed class WorldEngine
    {
        public ChunkManager ChunkManager { get; private set; } = null!;
        public IsometricRenderer Renderer { get; private set; } = null!;
        public IsometricCamera Camera { get; private set; } = null!;

        private const int ChunkLoadRadius = 2;
        private Bitmap _cachedGlobalMap = null;

        public List<FreeBuilding> Buildings = new();

        public void Initialize(IGraphicsDevice graphicsDevice, int chunkSize, int viewportWidth, int viewportHeight)
        {
            ChunkManager = new ChunkManager(chunkSize);
            Renderer = new IsometricRenderer(graphicsDevice);
            Camera = new IsometricCamera(viewportWidth, viewportHeight);
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

        public void Draw(IGraphicsDevice graphics)
        {
            Renderer.DrawWorld(ChunkManager, Buildings, (int)Camera.X, (int)Camera.Y, Camera.ViewportWidth, Camera.ViewportHeight);
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

            // Малюємо кешовану карту на весь екран
            g.DrawImage(_cachedGlobalMap, 0, 0, screenWidth, screenHeight);

            // Знаходимо позицію камери (пропорційно)
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
