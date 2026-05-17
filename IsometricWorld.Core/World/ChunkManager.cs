// Розробник А: Гриценко Богдан Олександрович
// Проєкт: Ізометрична чанкова система (Середньовіччя)

using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace IsometricWorld.Core.World
{
    /// <summary>
    /// Центральний менеджер чанків. Зберігає всі завантажені чанки та
    /// надає єдиний API для доступу до тайлів за світовими координатами.
    ///
    /// Складність доступу:
    ///   <c>GetTileAt</c>  — O(1) пошук у Dictionary + O(1) обчислення індексу.
    ///   <c>LoadChunk</c>  — O(chunkSize²) ініціалізація масиву тайлів.
    /// </summary>
    public sealed class ChunkManager
    {
        // ── Конфігурація ──────────────────────────────────────────────────────────

        /// <summary>
        /// Розмір сторони кожного чанку у тайлах.
        /// Однаковий для всіх чанків у межах одного ChunkManager.
        /// </summary>
        public int ChunkSize { get; }

        // ── Сховище чанків ────────────────────────────────────────────────────────

        /// <summary>
        /// Словник завантажених чанків.
        /// Ключ — пара координат чанку у сітці чанків, а не у тайлах.
        /// Кортежний ключ (ValueTuple) є value-type → нема алокацій при lookup.
        /// Доступ O(1) у середньому.
        /// </summary>
        private readonly Dictionary<(int cx, int cy), Chunk> _loadedChunks = new();

        // ── Конструктор ───────────────────────────────────────────────────────────

        /// <param name="chunkSize">
        /// Розмір сторони чанку у тайлах (наприклад, 40 → чанк 40×40).
        /// </param>
        public ChunkManager(int chunkSize = 40)
        {
            if (chunkSize < 1)
                throw new ArgumentOutOfRangeException(nameof(chunkSize));
            ChunkSize = chunkSize;
        }

        // ── Публічний API ─────────────────────────────────────────────────────────

        /// <summary>
        /// Повертає тайл за <b>світовими</b> координатами тайлу.
        ///
        /// Математика перетворення координат:
        /// <code>
        /// ┌─ Крок 1: Математичний поділ (Floor Division) ──────────────────────┐
        /// │  chunkX = FloorDiv(worldX, ChunkSize)                               │
        /// │  chunkY = FloorDiv(worldY, ChunkSize)                               │
        /// │                                                                     │
        /// │  ЧОМУ не просто worldX / ChunkSize?                                 │
        /// │  C# оператор / для int — це truncated division (обрізає до нуля).  │
        /// │  Приклад: worldX = -1, ChunkSize = 40                               │
        /// │    Truncated:  -1 / 40  = 0  (НЕПРАВИЛЬНО — це чанк 0, а не -1)   │
        /// │    Floor:      FloorDiv(-1, 40) = -1  (ПРАВИЛЬНО)                  │
        /// │                                                                     │
        /// │  Формула FloorDiv без гілок:                                        │
        /// │    (a >> 31) — знаковий біт a (0 або -1)                           │
        /// │    Для додатніх: (a >> 31) = 0, звичайний поділ                    │
        /// │    Для від'ємних: виправляємо зміщення на (b-1)                    │
        /// └─────────────────────────────────────────────────────────────────────┘
        ///
        /// ┌─ Крок 2: Локальний індекс тайлу всередині чанку ───────────────────┐
        /// │  localX = worldX - chunkX * ChunkSize                              │
        /// │  localY = worldY - chunkY * ChunkSize                              │
        /// │                                                                     │
        /// │  Це математично еквівалентно математичному mod (завжди ≥ 0),       │
        /// │  бо chunkX вже правильно обчислений через FloorDiv.                │
        /// └─────────────────────────────────────────────────────────────────────┘
        /// </code>
        /// </summary>
        /// <param name="worldX">Абсолютна координата тайлу по X у світовій сітці.</param>
        /// <param name="worldY">Абсолютна координата тайлу по Y у світовій сітці.</param>
        /// <returns>Копія тайлу. Якщо чанк не завантажений — <see cref="Tile.Default"/>.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Tile GetTileAt(int worldX, int worldY)
        {
            // ── Крок 1: Визначаємо координати чанку (Floor Division) ─────────────
            int chunkX = FloorDiv(worldX, ChunkSize);
            int chunkY = FloorDiv(worldY, ChunkSize);

            // ── Крок 2: Шукаємо чанк у словнику ─────────────────────────────────
            if (!_loadedChunks.TryGetValue((chunkX, chunkY), out Chunk? chunk))
                return Tile.Default; // Чанк не завантажений — повертаємо дефолт

            // ── Крок 3: Обчислюємо локальні координати тайлу ─────────────────────
            int localX = worldX - chunkX * ChunkSize; // завжди [0, ChunkSize)
            int localY = worldY - chunkY * ChunkSize;

            return chunk.GetTile(localX, localY);
        }

        /// <summary>
        /// Повертає <b>посилання</b> на тайл для прямої зміни без копіювання.
        /// Кидає <see cref="InvalidOperationException"/>, якщо чанк не завантажений.
        /// </summary>
        /// <exception cref="InvalidOperationException">Чанк не завантажений.</exception>
        public ref Tile GetTileRefAt(int worldX, int worldY)
        {
            int chunkX = FloorDiv(worldX, ChunkSize);
            int chunkY = FloorDiv(worldY, ChunkSize);

            if (!_loadedChunks.TryGetValue((chunkX, chunkY), out Chunk? chunk))
                throw new InvalidOperationException(
                    $"Чанк ({chunkX}, {chunkY}) не завантажений. " +
                    $"Викличте LoadChunk({chunkX}, {chunkY}) перед модифікацією тайлу.");

            int localX = worldX - chunkX * ChunkSize;
            int localY = worldY - chunkY * ChunkSize;

            return ref chunk.TileRef(localX, localY);
        }

        /// <summary>
        /// Перевіряє наявність об'єкта на вказаному тайлі та повертає його.
        /// </summary>
        public bool TryGetPropAt(int worldX, int worldY, out Prop prop)
        {
            int chunkX = FloorDiv(worldX, ChunkSize);
            int chunkY = FloorDiv(worldY, ChunkSize);

            if (_loadedChunks.TryGetValue((chunkX, chunkY), out Chunk? chunk))
            {
                int localX = worldX - chunkX * ChunkSize;
                int localY = worldY - chunkY * ChunkSize;
                return chunk.Props.TryGetValue((localX, localY), out prop);
            }

            prop = default;
            return false;
        }

        /// <summary>
        /// Завантажує (або повторно ініціалізує) чанк за координатами сітки чанків.
        /// Заглушка генератора: заповнює всі тайли дефолтним значенням (<c>Ocean</c>).
        ///
        /// TODO: замінити тіло на виклик реального ProceduralGenerator
        ///       або десеріалізатора збережених даних з диску.
        /// </summary>
        /// <param name="chunkX">Координата чанку по X (НЕ тайлова — сітка чанків).</param>
        /// <param name="chunkY">Координата чанку по Y (НЕ тайлова — сітка чанків).</param>
        public void LoadChunk(int chunkX, int chunkY)
        {
            // Створюємо чанк — конструктор автоматично заповнює Ocean-тайлами
            var chunk = new Chunk(chunkX, chunkY, ChunkSize);

            // Генеруємо тайли та пропси процедурно
            var generated = IsometricWorld.Core.Generation.WorldGenerator.GenerateChunk(chunkX, chunkY, ChunkSize, out var props);
            generated.AsSpan().CopyTo(chunk.RawSpan);

            foreach (var kvp in props)
            {
                chunk.Props[kvp.Key] = kvp.Value;
            }

            // Зберігаємо у словник (перезаписуємо, якщо вже існував)
            _loadedChunks[(chunkX, chunkY)] = chunk;
        }

        /// <summary>
        /// Вивантажує чанк з пам'яті.
        /// У повній реалізації тут має бути серіалізація на диск.
        /// </summary>
        /// <returns><c>true</c> якщо чанк був завантажений і вивантажений.</returns>
        public bool UnloadChunk(int chunkX, int chunkY)
            => _loadedChunks.Remove((chunkX, chunkY));

        /// <summary>Перевіряє, чи завантажений конкретний чанк.</summary>
        public bool IsChunkLoaded(int chunkX, int chunkY)
            => _loadedChunks.ContainsKey((chunkX, chunkY));

        /// <summary>Кількість чанків у пам'яті.</summary>
        public int LoadedChunkCount => _loadedChunks.Count;

        /// <summary>Повертає ключі всіх завантажених чанків (для стрімінгу).</summary>
        public System.Collections.Generic.IEnumerable<(int cx, int cy)> GetLoadedChunkKeys() => _loadedChunks.Keys;

        /// <summary>Додає вільну будівлю у відповідний чанк.</summary>
        public void AddBuilding(FreeBuilding building)
        {
            int chunkX = FloorDiv((int)Math.Floor(building.WorldX), ChunkSize);
            int chunkY = FloorDiv((int)Math.Floor(building.WorldY), ChunkSize);

            if (_loadedChunks.TryGetValue((chunkX, chunkY), out Chunk? chunk))
            {
                chunk.Buildings.Add(building);
            }
        }

        /// <summary>Отримує всі будівлі з активних чанків для рендеру та колізій.</summary>
        public IEnumerable<FreeBuilding> GetLoadedBuildings()
        {
            foreach (var chunk in _loadedChunks.Values)
            {
                foreach (var b in chunk.Buildings)
                {
                    yield return b;
                }
            }
        }

        // ── Приватна математика ───────────────────────────────────────────────────

        /// <summary>
        /// Математичне ділення з округленням до нуля мінус нескінченність (Floor Division).
        /// Гарантує правильний результат для від'ємних координат.
        ///
        /// Формула без умовних переходів (branchless для JIT):
        ///   FloorDiv(a, b) = (a - (((a % b) + b) % b)) / b
        ///
        /// Але наступна реалізація через знаковий біт швидша:
        ///   Для a ≥ 0: результат = a / b  (звичайне int-ділення)
        ///   Для a &lt; 0: результат = (a - b + 1) / b
        ///
        /// Таблиця прикладів (b = 40):
        /// <code>
        ///  a  =   80  →  chunkX =  2
        ///  a  =    0  →  chunkX =  0
        ///  a  =   -1  →  chunkX = -1  (не 0!)
        ///  a  =  -40  →  chunkX = -1
        ///  a  =  -41  →  chunkX = -2
        /// </code>
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static int FloorDiv(int a, int b)
        {
            // Обчислюємо ціле ділення C# (truncated toward zero)
            int q = a / b;
            // Якщо знаки різні ТА є залишок — коригуємо на -1
            // ((a ^ b) >> 31) == -1 лише коли знаки a та b різні
            // (a - q * b) != 0 — перевірка ненульового залишку
            return q + (((a ^ b) >> 31) & (((a - q * b) != 0) ? 1 : 0)) * -1;
        }
    }
}
