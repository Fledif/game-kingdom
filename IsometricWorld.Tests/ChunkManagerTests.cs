// Розробник А: Гриценко Богдан Олександрович
// Проєкт: Ізометрична чанкова система (Середньовіччя)

using Xunit;
using IsometricWorld.Core.World;

namespace IsometricWorld.Tests
{
    /// <summary>
    /// Юніт-тести для <see cref="ChunkManager"/>.
    /// Особлива увага — математика FloorDiv для від'ємних координат.
    /// </summary>
    public class ChunkManagerTests
    {
        private const int ChunkSize = 40;

        // ── Тест 1: Базове завантаження та читання ─────────────────────────────

        [Fact]
        public void GetTileAt_UnloadedChunk_ReturnsDefault()
        {
            var mgr = new ChunkManager(ChunkSize);
            Tile tile = mgr.GetTileAt(0, 0);
            Assert.Equal(TileType.Ocean, tile.Type);
        }

        [Fact]
        public void LoadChunk_ThenGetTile_ReturnsOceanDefault()
        {
            var mgr = new ChunkManager(ChunkSize);
            mgr.LoadChunk(0, 0);
            Tile tile = mgr.GetTileAt(0, 0);
            Assert.Equal(TileType.Ocean, tile.Type);
        }

        // ── Тест 2: Запис і читання тайлу ────────────────────────────────────

        [Fact]
        public void SetTile_ThenGet_ReturnsModifiedTile()
        {
            var mgr = new ChunkManager(ChunkSize);
            mgr.LoadChunk(0, 0);

            ref Tile t = ref mgr.GetTileRefAt(5, 7);
            t.Type     = TileType.Forest;
            t.Height   = 0.65f;
            t.Moisture = 0.8f;

            Tile result = mgr.GetTileAt(5, 7);
            Assert.Equal(TileType.Forest, result.Type);
            Assert.Equal(0.65f, result.Height, precision: 5);
        }

        // ── Тест 3: Координатна математика — КЛЮЧОВІ КЕЙСИ ───────────────────

        [Theory]
        // Позитивні координати — очевидні
        [InlineData(  0,   0,   0,  0,  0,  0)]  // перший тайл світу
        [InlineData( 39,   0,   0,  0, 39,  0)]  // останній тайл чанку (0,0) по X
        [InlineData( 40,   0,   1,  0,  0,  0)]  // перший тайл чанку (1,0)
        [InlineData( 80,   0,   2,  0,  0,  0)]  // чанк (2,0)
        [InlineData( 79,  79,   1,  1, 39, 39)]  // кут чанку (1,1)
        // Від'ємні координати — критична зона помилок
        [InlineData( -1,   0,  -1,  0, 39,  0)]  // -1 тайл → чанк -1, local 39
        [InlineData(-40,   0,  -1,  0,  0,  0)]  // рівно -ChunkSize → чанк -1, local 0
        [InlineData(-41,   0,  -2,  0, 39,  0)]  // -41 → чанк -2, local 39
        [InlineData( -1,  -1,  -1, -1, 39, 39)]  // обидві осі від'ємні
        [InlineData(-80,   0,  -2,  0,  0,  0)]  // -2*ChunkSize → чанк -2, local 0
        public void GetTileAt_CoordinateMapping_IsCorrect(
            int worldX,  int worldY,
            int expCX,   int expCY,
            int expLX,   int expLY)
        {
            var mgr = new ChunkManager(ChunkSize);
            mgr.LoadChunk(expCX, expCY);

            // Записуємо маркер у очікуваний локальний тайл
            var marker = new Tile(TileType.Mountain, 0.9f, 0.1f);
            mgr.LoadChunk(expCX, expCY);
            // Отримуємо чанк через публічний ref-доступ
            ref Tile t = ref mgr.GetTileRefAt(
                expCX * ChunkSize + expLX,
                expCY * ChunkSize + expLY);
            t = marker;

            // Читаємо через worldX/worldY — має повернути той самий тайл
            Tile result = mgr.GetTileAt(worldX, worldY);
            Assert.Equal(TileType.Mountain, result.Type);
        }

        // ── Тест 4: Межі чанку ────────────────────────────────────────────────

        [Fact]
        public void TileAtChunkBoundary_BelongsToCorrectChunk()
        {
            var mgr = new ChunkManager(ChunkSize);
            mgr.LoadChunk(0, 0);
            mgr.LoadChunk(1, 0);

            ref Tile boundary = ref mgr.GetTileRefAt(ChunkSize, 0); // worldX=40 → chunk(1,0), local(0,0)
            boundary.Type = TileType.River;

            // Тайл (40,0) має бути в чанку (1,0)
            Assert.Equal(TileType.River,  mgr.GetTileAt(ChunkSize, 0).Type);
            // Тайл (39,0) має залишитись у чанку (0,0) — Ocean
            Assert.Equal(TileType.Ocean,  mgr.GetTileAt(ChunkSize - 1, 0).Type);
        }

        // ── Тест 5: Завантаження / вивантаження ──────────────────────────────

        [Fact]
        public void UnloadChunk_RemovesFromMemory()
        {
            var mgr = new ChunkManager(ChunkSize);
            mgr.LoadChunk(3, 7);
            Assert.True(mgr.IsChunkLoaded(3, 7));
            mgr.UnloadChunk(3, 7);
            Assert.False(mgr.IsChunkLoaded(3, 7));
            Assert.Equal(0, mgr.LoadedChunkCount);
        }

        [Fact]
        public void LoadedChunkCount_TracksCorrectly()
        {
            var mgr = new ChunkManager(ChunkSize);
            mgr.LoadChunk(0, 0);
            mgr.LoadChunk(1, 0);
            mgr.LoadChunk(-1, -1);
            Assert.Equal(3, mgr.LoadedChunkCount);
        }

        // ── Тест 6: Tile struct size ──────────────────────────────────────────

        [Fact]
        public void TileStruct_HasExpectedSize()
        {
            // TileType (byte=1) + float Height (4) + float Moisture (4) = 9 bytes з Pack=1
            int size = System.Runtime.InteropServices.Marshal.SizeOf<Tile>();
            Assert.Equal(9, size);
        }
    }
}
