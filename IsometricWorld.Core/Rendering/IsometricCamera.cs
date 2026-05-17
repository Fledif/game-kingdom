// Розробник А: Гриценко Богдан Олександрович
// Проєкт: Custom C# Engine (Free-form Building & UI)

namespace IsometricWorld.Core.Rendering
{
    /// <summary>
    /// Камера для ізометричного світу з підтримкою субпіксельного переміщення.
    /// </summary>
    public sealed class IsometricCamera
    {
        public float X { get; set; }
        public float Y { get; set; }
        public int ViewportWidth { get; set; }
        public int ViewportHeight { get; set; }

        public IsometricCamera(int viewportWidth, int viewportHeight)
        {
            ViewportWidth = viewportWidth;
            ViewportHeight = viewportHeight;
        }

        public void Move(float dx, float dy)
        {
            X += dx;
            Y += dy;
        }

        public void CenterOnWorldTile(int worldX, int worldY)
        {
            // Вираховуємо піксельну координату тайлу без урахування камери
            var (screenX, screenY) = IsometricRenderer.WorldToScreen(worldX, worldY, 0, 0);
            
            // Зсуваємо так, щоб ця координата була в центрі екрану
            X = screenX - ViewportWidth / 2f;
            Y = screenY - ViewportHeight / 2f;
        }

        public (float worldX, float worldY) ScreenToWorld(float screenX, float screenY, int screenWidth, int screenHeight)
        {
            float adjX = screenX + X;
            float adjY = screenY + Y;
            float worldX = (adjX / IsometricRenderer.HalfWidth + adjY / IsometricRenderer.HalfHeight) / 2f;
            float worldY = (adjY / IsometricRenderer.HalfHeight - adjX / IsometricRenderer.HalfWidth) / 2f;
            return (worldX, worldY);
        }
    }
}
