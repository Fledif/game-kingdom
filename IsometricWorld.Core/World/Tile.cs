// Розробник А: Гриценко Богдан Олександрович
// Проєкт: Ізометрична чанкова система (Середньовіччя)

using System.Runtime.InteropServices;

namespace IsometricWorld.Core.World
{
    /// <summary>
    /// Найменша одиниця ігрового світу — один тайл сітки.
    ///
    /// ОБОВ'ЯЗКОВО struct (не class):
    ///  • Мільйони тайлів зберігаються у масивах — struct дозволяє
    ///    розміщувати їх безпосередньо в пам'яті масиву (unboxed),
    ///    без окремих алокацій на купі (Heap) та без тиску на GC.
    ///  • Компактний, послідовний layout у пам'яті → CPU-кеш-дружній
    ///    доступ під час ітерації рядків чанку.
    ///  • Pack = 1: усуває padding-байти між полями (9 байт замість 12).
    /// </summary>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct Tile
    {
        /// <summary>Біом / тип поверхні цього тайлу.</summary>
        public TileType Type;

        /// <summary>
        /// Висота рельєфу [0.0 .. 1.0].
        /// 0.0 = рівень моря, 1.0 = найвища гора.
        /// Використовується генератором рельєфу (Perlin / Simplex Noise).
        /// </summary>
        public float Height;

        /// <summary>
        /// Вологість [0.0 .. 1.0].
        /// Разом з Height визначає кінцевий біом за таблицею Уіттекера.
        /// 0.0 = пустеля, 1.0 = тропічний ліс / болото.
        /// </summary>
        public float Moisture;

        /// <summary>
        /// Зручний конструктор для явної ініціалізації тайлу.
        /// </summary>
        public Tile(TileType type, float height, float moisture)
        {
            Type     = type;
            Height   = height;
            Moisture = moisture;
        }

        /// <summary>Дефолтний тайл — океан на рівні моря.</summary>
        public static readonly Tile Default = new(TileType.Ocean, 0f, 0f);
    }
}
