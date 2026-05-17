// Розробник А: Гриценко Богдан Олександрович
// Проєкт: Ізометрична чанкова система (Об'єкти та Рендер)

using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace IsometricWorld.Core.World
{
    /// <summary>
    /// Прямокутний блок тайлів фіксованого розміру <c>ChunkSize × ChunkSize</c>.
    /// </summary>
    public sealed class Chunk
    {
        public int ChunkX { get; }
        public int ChunkY { get; }
        public int ChunkSize { get; }

        private readonly Tile[] _tiles;
        
        /// <summary>
        /// Словник статичних об'єктів (Props). Ключ — локальні координати (LocalX, LocalY).
        /// Дозволяє швидко перевіряти наявність об'єкта на тайлі.
        /// </summary>
        public Dictionary<(int, int), Prop> Props { get; } = new();

        public Chunk(int chunkX, int chunkY, int chunkSize)
        {
            if (chunkSize < 1)
                throw new ArgumentOutOfRangeException(nameof(chunkSize));

            ChunkX    = chunkX;
            ChunkY    = chunkY;
            ChunkSize = chunkSize;

            _tiles = new Tile[chunkSize * chunkSize];
            Array.Fill(_tiles, Tile.Default);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ref Tile TileRef(int localX, int localY)
            => ref _tiles[localY * ChunkSize + localX];

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Tile GetTile(int localX, int localY)
            => _tiles[localY * ChunkSize + localX];

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void SetTile(int localX, int localY, in Tile tile)
            => _tiles[localY * ChunkSize + localX] = tile;

        public Span<Tile> RawSpan => _tiles.AsSpan();
    }
}
