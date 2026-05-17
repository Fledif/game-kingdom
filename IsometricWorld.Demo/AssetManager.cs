// Розробник А: Гриценко Богдан Олександрович
// Проєкт: Custom C# Engine (Геометричний Ізометричний Рендер)

using System;
using System.Drawing;
using IsometricWorld.Core.Rendering;

namespace IsometricWorld.Demo
{
    public class AssetManager : IGraphicsDevice, IDisposable
    {
        public Graphics CurrentGraphics { get; set; } = null!;

        public void LoadAssets()
        {
            // Більше не завантажуємо текстури з диска.
            // Рендеринг повністю переведено на плоскі геометричні фігури.
        }

        public void Dispose()
        {
            // Порожньо, ресурси зображень більше не використовуються
        }
    }
}
