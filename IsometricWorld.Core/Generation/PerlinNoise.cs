// Розробник А: Гриценко Богдан Олександрович
// Проєкт: Ізометрична чанкова система (Генерація материка)

using System;
using System.Runtime.CompilerServices;

namespace IsometricWorld.Core.Generation
{
    /// <summary>
    /// Самодостатня реалізація Perlin Noise (класичний алгоритм Кена Перліна, 2002).
    /// Не потребує жодних зовнішніх бібліотек.
    ///
    /// Особливості:
    ///   • Використовує permutation table розміром 512 для уникнення артефактів на межах.
    ///   • Підтримує seed — кожен seed дає унікальну, але детерміновану карту.
    ///   • Метод <see cref="Octave"/> — FBM (Fractal Brownian Motion): сума N октав
    ///     з різними частотами та амплітудами для природного рельєфу.
    /// </summary>
    public sealed class PerlinNoise
    {
        // ── Permutation table (512 = подвоєна таблиця для wrap-around) ───────────

        private readonly int[] _p = new int[512];

        // Градієнтні вектори одиничного кола (12 граней куба, класика Перліна)
        private static readonly (float x, float y)[] Grad2D =
        {
            ( 1, 1), (-1, 1), ( 1,-1), (-1,-1),
            ( 1, 0), (-1, 0), ( 0, 1), ( 0,-1),
            ( 1, 1), (-1, 1), ( 0,-1), ( 1, 0),
        };

        // ── Конструктор ───────────────────────────────────────────────────────────

        /// <param name="seed">Початкове значення для PRNG. Однаковий seed = однакова карта.</param>
        public PerlinNoise(int seed)
        {
            // Заповнюємо базовий масив 0..255, потім перемішуємо за Fisher-Yates
            var src = new int[256];
            for (int i = 0; i < 256; i++) src[i] = i;

            var rng = new Random(seed);
            for (int i = 255; i > 0; i--)
            {
                int j = rng.Next(i + 1);
                (src[i], src[j]) = (src[j], src[i]);
            }

            // Подвоюємо для безшовного wrap-around
            for (int i = 0; i < 512; i++) _p[i] = src[i & 255];
        }

        // ── Raw Perlin [-1, 1] ────────────────────────────────────────────────────

        /// <summary>
        /// Базове значення Perlin Noise у точці (x, y).
        /// Повертає значення в діапазоні приблизно [-1, 1].
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public float Sample(float x, float y)
        {
            int xi = (int)MathF.Floor(x) & 255;
            int yi = (int)MathF.Floor(y) & 255;

            float xf = x - MathF.Floor(x);
            float yf = y - MathF.Floor(y);

            // Fade-функція Перліна: 6t⁵ − 15t⁴ + 10t³ (C² неперервна)
            float u = Fade(xf);
            float v = Fade(yf);

            int aa = _p[_p[xi    ] + yi    ];
            int ab = _p[_p[xi    ] + yi + 1];
            int ba = _p[_p[xi + 1] + yi    ];
            int bb = _p[_p[xi + 1] + yi + 1];

            float x1 = Lerp(Grad(aa, xf,     yf    ),
                            Grad(ba, xf - 1, yf    ), u);
            float x2 = Lerp(Grad(ab, xf,     yf - 1),
                            Grad(bb, xf - 1, yf - 1), u);

            return Lerp(x1, x2, v);
        }

        /// <summary>
        /// FBM (Fractal Brownian Motion) — сума <paramref name="octaves"/> октав шуму.
        /// Кожна наступна октава має вдвічі більшу частоту (lacunarity=2)
        /// та вдвічі меншу амплітуду (persistence=0.5).
        ///
        /// Результат нормалізований у [0, 1].
        /// </summary>
        /// <param name="x">Координата X у світовому просторі.</param>
        /// <param name="y">Координата Y у світовому просторі.</param>
        /// <param name="scale">Масштаб першої октави. Менше = крупніші риси рельєфу.</param>
        /// <param name="octaves">Кількість октав. 6–8 — оптимально для рельєфу.</param>
        /// <param name="persistence">Зменшення амплітуди між октавами (зазвичай 0.5).</param>
        /// <param name="lacunarity">Збільшення частоти між октавами (зазвичай 2.0).</param>
        public float Octave(float x, float y,
                            float scale       = 0.004f,
                            int   octaves     = 7,
                            float persistence = 0.50f,
                            float lacunarity  = 2.00f)
        {
            float value     = 0f;
            float amplitude = 1f;
            float frequency = scale;
            float maxValue  = 0f;   // для нормалізації

            for (int i = 0; i < octaves; i++)
            {
                value     += Sample(x * frequency, y * frequency) * amplitude;
                maxValue  += amplitude;
                amplitude *= persistence;
                frequency *= lacunarity;
            }

            // Нормалізуємо з [-maxValue, maxValue] → [0, 1]
            return (value / maxValue + 1f) * 0.5f;
        }

        // ── Допоміжні функції ─────────────────────────────────────────────────────

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static float Fade(float t) => t * t * t * (t * (t * 6 - 15) + 10);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static float Lerp(float a, float b, float t) => a + t * (b - a);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static float Grad(int hash, float x, float y)
        {
            var (gx, gy) = Grad2D[hash % 12];
            return gx * x + gy * y;
        }
    }
}
