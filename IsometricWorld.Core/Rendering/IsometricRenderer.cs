// Розробник А: Гриценко Богдан Олександрович
// Проєкт: Custom C# Engine (Колізії та Z-Сортування будівель)

using System;
using System.Drawing;
using System.Linq;
using IsometricWorld.Core.World;

namespace IsometricWorld.Core.Rendering
{
    public interface IGraphicsDevice
    {
        Graphics CurrentGraphics { get; }
    }

    public class IsometricRenderer
    {
        public const int TileWidth = 64;
        public const int TileHeight = 32;
        public const int HalfWidth = TileWidth / 2;
        public const int HalfHeight = TileHeight / 2;

        private readonly IGraphicsDevice _graphicsDevice;

        public IsometricRenderer(IGraphicsDevice graphicsDevice)
        {
            _graphicsDevice = graphicsDevice;
        }

        /// <summary>Повертає поточний Graphics контекст для зовнішнього рендерингу.</summary>
        public Graphics? GetGraphics() => _graphicsDevice.CurrentGraphics;

        public static (int screenX, int screenY) WorldToScreen(int worldX, int worldY, int cameraX, int cameraY)
        {
            int screenX = (worldX - worldY) * HalfWidth - cameraX;
            int screenY = (worldX + worldY) * HalfHeight - cameraY;
            return (screenX, screenY);
        }

        private static (int worldX, int worldY) ScreenToWorld(int screenX, int screenY, int cameraX, int cameraY)
        {
            float adjX = screenX + cameraX;
            float adjY = screenY + cameraY;
            int worldX = (int)Math.Floor((adjX / HalfWidth + adjY / HalfHeight) / 2f);
            int worldY = (int)Math.Floor((adjY / HalfHeight - adjX / HalfWidth) / 2f);
            return (worldX, worldY);
        }

        public void DrawWorld(ChunkManager chunkManager, System.Collections.Generic.List<FreeBuilding> buildings, int cameraX, int cameraY, int screenWidth, int screenHeight)
        {
            var g = _graphicsDevice.CurrentGraphics;
            if (g == null) return;

            var topLeft = ScreenToWorld(0, 0, cameraX, cameraY);
            var topRight = ScreenToWorld(screenWidth, 0, cameraX, cameraY);
            var bottomLeft = ScreenToWorld(0, screenHeight, cameraX, cameraY);
            var bottomRight = ScreenToWorld(screenWidth, screenHeight, cameraX, cameraY);

            int minWorldX = Math.Min(Math.Min(topLeft.worldX, topRight.worldX), Math.Min(bottomLeft.worldX, bottomRight.worldX)) - 2;
            int maxWorldX = Math.Max(Math.Max(topLeft.worldX, topRight.worldX), Math.Max(bottomLeft.worldX, bottomRight.worldX)) + 2;
            int minWorldY = Math.Min(Math.Min(topLeft.worldY, topRight.worldY), Math.Min(bottomLeft.worldY, bottomRight.worldY)) - 2;
            int maxWorldY = Math.Max(Math.Max(topLeft.worldY, topRight.worldY), Math.Max(bottomLeft.worldY, bottomRight.worldY)) + 2;

            // Painter's Algorithm: йдемо згори вниз по екрану
            for (int y = minWorldY; y <= maxWorldY; y++)
            {
                for (int x = minWorldX; x <= maxWorldX; x++)
                {
                    Tile tile = chunkManager.GetTileAt(x, y);

                    var (screenX, screenY) = WorldToScreen(x, y, cameraX, cameraY);
                    int elevationOffset = (int)(tile.Height * 10f); 

                    // 1. Відмальовка тайлу
                    Brush brush = tile.Type switch
                    {
                        TileType.Ocean => Brushes.Blue,
                        TileType.River => Brushes.DeepSkyBlue,
                        TileType.Beach => Brushes.Khaki,
                        TileType.Plains => Brushes.LimeGreen,
                        TileType.Forest => Brushes.Green,
                        TileType.Mountain => Brushes.Gray,
                        _ => Brushes.Magenta
                    };

                    Point[] points = new Point[]
                    {
                        new Point(screenX, screenY - elevationOffset),
                        new Point(screenX + HalfWidth, screenY + HalfHeight - elevationOffset),
                        new Point(screenX, screenY + TileHeight - elevationOffset),
                        new Point(screenX - HalfWidth, screenY + HalfHeight - elevationOffset)
                    };
                    
                    g.FillPolygon(brush, points);

                    // 2. Відмальовка об'єкту (Prop), якщо він є на цьому тайлі
                    if (chunkManager.TryGetPropAt(x, y, out Prop prop))
                    {
                        int propBaseX = screenX;
                        int propBaseY = screenY + HalfHeight - elevationOffset;

                        if (prop.Type == PropType.Tree_Oak || prop.Type == PropType.Tree_Pine)
                        {
                            // Стовбур
                            g.FillRectangle(Brushes.SaddleBrown, propBaseX - 2, propBaseY - 10, 4, 10);
                            
                            if (prop.Type == PropType.Tree_Oak)
                            {
                                // Крона (Дуб) - Круг
                                g.FillEllipse(Brushes.DarkGreen, propBaseX - 8, propBaseY - 22, 16, 16);
                            }
                            else
                            {
                                // Крона (Сосна) - Трикутник
                                Point[] pinePoints = {
                                    new Point(propBaseX, propBaseY - 24),
                                    new Point(propBaseX + 8, propBaseY - 8),
                                    new Point(propBaseX - 8, propBaseY - 8)
                                };
                                g.FillPolygon(Brushes.DarkGreen, pinePoints);
                            }
                        }
                        else if (prop.Type == PropType.Rock_Small || prop.Type == PropType.Rock_Large)
                        {
                            int size = prop.Type == PropType.Rock_Large ? 12 : 6;
                            Point[] rockPoints = {
                                new Point(propBaseX, propBaseY - size),
                                new Point(propBaseX + size, propBaseY),
                                new Point(propBaseX - size, propBaseY)
                            };
                            g.FillPolygon(Brushes.DarkGray, rockPoints);
                        }
                    }
                }
            }

            // Відмальовка вільних будівель (Free Buildings) із Z-сортуванням
            var sortedBuildings = buildings.OrderBy(b => b.WorldX + b.WorldY).ToList();

            foreach (var b in sortedBuildings)
            {
                float sx = (b.WorldX - b.WorldY) * HalfWidth - cameraX;
                float sy = (b.WorldX + b.WorldY) * HalfHeight - cameraY;

                int h = 30; // висота блоку
                int w = HalfWidth; 
                int th = HalfHeight; 

                int px = (int)sx;
                int py = (int)sy + th; 

                Point[] topFace = {
                    new Point(px, py - h),
                    new Point(px + w, py - th - h),
                    new Point(px, py - 2 * th - h),
                    new Point(px - w, py - th - h)
                };
                g.FillPolygon(Brushes.LightGray, topFace);
                g.DrawPolygon(Pens.DarkGray, topFace);

                Point[] leftFace = {
                    new Point(px - w, py - th - h),
                    new Point(px, py - h),
                    new Point(px, py),
                    new Point(px - w, py - th)
                };
                g.FillPolygon(Brushes.Gray, leftFace);
                g.DrawPolygon(Pens.DarkGray, leftFace);

                Point[] rightFace = {
                    new Point(px, py - h),
                    new Point(px + w, py - th - h),
                    new Point(px + w, py - th),
                    new Point(px, py)
                };
                g.FillPolygon(Brushes.DimGray, rightFace);
                g.DrawPolygon(Pens.DarkGray, rightFace);
            }
        }
    }
}
