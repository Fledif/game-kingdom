// Розробник А: Гриценко Богдан Олександрович
// Проєкт: Custom C# Engine (Планетарний масштаб 1_000_000)

using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using IsometricWorld.Core.World;

namespace IsometricWorld.Core.Generation
{
    public static class WorldGenerator
    {
        public static int Seed { get; set; } = 42;
        
        // Масштаб світу: 1000000x1000000
        public const int WorldSize = 1000000;

        private static PerlinNoise _heightNoise;
        private static int _lastSeed = -1;

        private static void EnsureInitialized()
        {
            if (_lastSeed != Seed || _heightNoise == null)
            {
                _heightNoise = new PerlinNoise(Seed);
                _lastSeed = Seed;
            }
        }

        private static float GetFractalNoise(float x, float y)
        {
            float scaleAdjust = 3000f / WorldSize;

            // Октава 1 (Основа материка): вага 1.0, частота 0.002
            float n1 = _heightNoise.Octave(x, y, 0.002f * scaleAdjust, 1, 1f, 1f);
            
            // Октава 2 (Пагорби та великі біоми): вага 0.5, частота 0.008
            float n2 = _heightNoise.Octave(x, y, 0.008f * scaleAdjust, 1, 1f, 1f);
            
            // Октава 3 (Деталізація берегової лінії): вага 0.15, частота 0.03
            float n3 = _heightNoise.Octave(x, y, 0.03f * scaleAdjust, 1, 1f, 1f);

            float totalValue = (n1 * 1.0f) + (n2 * 0.5f) + (n3 * 0.15f);
            float maxAmplitude = 1.0f + 0.5f + 0.15f;

            return totalValue / maxAmplitude;
        }

        public static (TileType type, float height, float moisture) GetTileDataAt(int worldX, int worldY)
        {
            EnsureInitialized();

            float worldHalf = WorldSize * 0.5f;
            float nx = (worldX - worldHalf) / worldHalf; 
            float ny = (worldY - worldHalf) / worldHalf;

            float dist = MathF.Sqrt(nx * nx + ny * ny);
            float t = Math.Clamp(dist, 0f, 1f);
            
            // Квадратичний Falloff
            float falloff = 1.0f - (t * t);

            float fractalNoise = GetFractalNoise(worldX, worldY);
            
            // Фінальна висота
            float height = fractalNoise * falloff;
            height = Math.Clamp(height, 0f, 1f);

            // Вологість
            float moisture = 0.5f;

            TileType type = DetermineBiome(height, moisture);
            return (type, height, moisture);
        }

        public static Tile[] GenerateChunk(int chunkX, int chunkY, int chunkSize, out Dictionary<(int, int), Prop> props)
        {
            var tiles = new Tile[chunkSize * chunkSize];
            props = new Dictionary<(int, int), Prop>();

            var rng = new Random(Seed ^ (chunkX * 73856093 ^ chunkY * 19349663));

            for (int localY = 0; localY < chunkSize; localY++)
            {
                for (int localX = 0; localX < chunkSize; localX++)
                {
                    int worldX = chunkX * chunkSize + localX;
                    int worldY = chunkY * chunkSize + localY;

                    var data = GetTileDataAt(worldX, worldY);

                    if (data.type == TileType.Forest && rng.NextDouble() < 0.40)
                    {
                        props.Add((localX, localY), new Prop(localX, localY, rng.NextDouble() < 0.5 ? PropType.Tree_Oak : PropType.Tree_Pine));
                    }
                    else if (data.type == TileType.Mountain && rng.NextDouble() < 0.30)
                    {
                        props.Add((localX, localY), new Prop(localX, localY, rng.NextDouble() < 0.5 ? PropType.Rock_Large : PropType.Rock_Small));
                    }

                    int idx = localY * chunkSize + localX;
                    tiles[idx] = new Tile(data.type, data.height, data.moisture);
                }
            }

            return tiles;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static TileType DetermineBiome(float height, float moisture = 0.5f)
        {
            if (height < 0.25f) return TileType.Ocean;
            if (height < 0.30f) return TileType.Beach;
            if (height < 0.50f) return TileType.Plains;
            if (height < 0.62f) return TileType.Forest;
            
            return TileType.Mountain;
        }
    }
}
