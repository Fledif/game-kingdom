namespace IsometricWorld.Core.Rendering
{
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
            var (screenX, screenY) = IsometricRenderer.WorldToScreen(worldX, worldY, 0, 0);
            X = screenX - ViewportWidth / 2f;
            Y = screenY - ViewportHeight / 2f;
        }

        public void CenterOnWorldPosition(float worldX, float worldY)
        {
            float screenX = (worldX - worldY) * IsometricRenderer.HalfWidth;
            float screenY = (worldX + worldY) * IsometricRenderer.HalfHeight;
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
