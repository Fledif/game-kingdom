// Розробник А: Гриценко Богдан Олександрович
// Проєкт: Custom C# Engine (Колізії та Z-Сортування будівель)

using System;
using System.Drawing;
using System.Windows.Forms;
using IsometricWorld.Core;
using IsometricWorld.Core.Generation;
using IsometricWorld.Core.UI;

namespace IsometricWorld.Demo
{
    public class GameWindow : Form
    {
        private WorldEngine _engine;
        private AssetManager _assetManager;
        private UIManager _uiManager;
        private System.Windows.Forms.Timer _gameTimer;
        private DateTime _lastFrameTime;

        // Стан кнопок для плавного руху камери
        private bool _moveUp, _moveDown, _moveLeft, _moveRight;
        private bool _showGlobalMap = false;

        public GameWindow()
        {
            // 1. Налаштування вікна
            this.Text = "Isometric World Engine (WinForms)";
            this.ClientSize = new Size(1280, 720);
            this.FormBorderStyle = FormBorderStyle.FixedSingle;
            this.MaximizeBox = false;
            
            // КРИТИЧНО: Увімкнення подвійної буферизації для уникнення блимання (Flickering)
            this.DoubleBuffered = true;

            // 2. Ініціалізація рушія
            _assetManager = new AssetManager();
            _assetManager.LoadAssets();

            _engine = new WorldEngine();
            _uiManager = new UIManager();
            // Розмір чанку = 40. Viewport береться з розмірів вікна.
            _engine.Initialize(_assetManager, 40, this.ClientSize.Width, this.ClientSize.Height);

            // Генеруємо світ за динамічним сідом та центруємо камеру на материку
            WorldGenerator.Seed = Environment.TickCount;
            _engine.Camera.CenterOnWorldTile(WorldGenerator.WorldSize / 2, WorldGenerator.WorldSize / 2);

            // 3. Обробка Input (Клавіатура)
            this.KeyDown += OnKeyDown;
            this.KeyUp += OnKeyUp;

            // 4. Game Loop (через Timer, приблизно 60 FPS = 16ms)
            _lastFrameTime = DateTime.Now;
            _gameTimer = new System.Windows.Forms.Timer();
            _gameTimer.Interval = 16; 
            _gameTimer.Tick += GameLoop;
            _gameTimer.Start();
        }

        private void GameLoop(object? sender, EventArgs e)
        {
            var now = DateTime.Now;
            float deltaTime = (float)(now - _lastFrameTime).TotalSeconds;
            _lastFrameTime = now;

            // Плавний рух камери
            float speed = 400f * deltaTime; // 400 пікселів за секунду
            if (_moveUp)    _engine.Camera.Move(0, -speed);
            if (_moveDown)  _engine.Camera.Move(0, speed);
            if (_moveLeft)  _engine.Camera.Move(-speed, 0);
            if (_moveRight) _engine.Camera.Move(speed, 0);

            // Оновлення логіки (стрімінг чанків)
            _engine.Update(deltaTime);

            // Запит на перемальовку екрану (викликає OnPaint)
            this.Invalidate();
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);

            // Фон "за межами світу" або порожнеча океану
            e.Graphics.Clear(Color.Black);

            // Встановлюємо поточний Graphics контекст у AssetManager для IGraphicsDevice
            _assetManager.CurrentGraphics = e.Graphics;

            // Відмальовка всього світу або глобальної карти
            if (_showGlobalMap)
            {
                _engine.DrawGlobalMap(e.Graphics, ClientSize.Width, ClientSize.Height);
            }
            else
            {
                _engine.Draw(null!);
            }

            // Відмальовка користувацького інтерфейсу поверх усього
            _uiManager.Draw(e.Graphics, ClientSize.Width, ClientSize.Height);
        }

        private void OnKeyDown(object? sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.W || e.KeyCode == Keys.Up) _moveUp = true;
            if (e.KeyCode == Keys.S || e.KeyCode == Keys.Down) _moveDown = true;
            if (e.KeyCode == Keys.A || e.KeyCode == Keys.Left) _moveLeft = true;
            if (e.KeyCode == Keys.D || e.KeyCode == Keys.Right) _moveRight = true;
            if (e.KeyCode == Keys.M) _showGlobalMap = !_showGlobalMap;
            if (e.KeyCode == Keys.R)
            {
                WorldGenerator.Seed = Environment.TickCount;
                _engine.ClearGlobalMapCache();
            }
        }

        private void OnKeyUp(object? sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.W || e.KeyCode == Keys.Up) _moveUp = false;
            if (e.KeyCode == Keys.S || e.KeyCode == Keys.Down) _moveDown = false;
            if (e.KeyCode == Keys.A || e.KeyCode == Keys.Left) _moveLeft = false;
            if (e.KeyCode == Keys.D || e.KeyCode == Keys.Right) _moveRight = false;
        }

        protected override void OnMouseClick(MouseEventArgs e)
        {
            base.OnMouseClick(e);
            
            if (_uiManager.HandleMouseClick(e.X, e.Y, ClientSize.Width, ClientSize.Height))
            {
                return;
            }

            if (_uiManager.IsBuildModeReady)
            {
                var coords = _engine.Camera.ScreenToWorld(e.X, e.Y, ClientSize.Width, ClientSize.Height);

                // Викликаємо нову логіку перевірки колізій та додавання будівлі у чанк.
                // Радіус 1.0f означає, що будівля займає місце приблизно 2x2 тайли (діаметр 2.0).
                _engine.TryAddBuilding(coords.worldX, coords.worldY, 1.0f);
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _assetManager.Dispose();
                _gameTimer.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}
