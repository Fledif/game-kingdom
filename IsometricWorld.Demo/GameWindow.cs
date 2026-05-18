using System;
using System.Drawing;
using System.Windows.Forms;
using IsometricWorld.Core;
using IsometricWorld.Core.Generation;
using IsometricWorld.Core.UI;
using IsometricWorld.Core.Rendering;
using IsometricWorld.Core.World;

namespace IsometricWorld.Demo
{
    public class GameWindow : Form
    {
        private WorldEngine _engine;
        private AssetManager _assetManager;
        private UIManager _uiManager;
        private System.Windows.Forms.Timer _gameTimer;
        private DateTime _lastFrameTime;

        private bool _moveUp, _moveDown, _moveLeft, _moveRight;
        private bool _showGlobalMap = false;

        public GameWindow()
        {
            this.Text = "Isometric World Engine (WinForms)";
            this.ClientSize = new Size(1280, 720);
            this.FormBorderStyle = FormBorderStyle.FixedSingle;
            this.MaximizeBox = false;
            this.DoubleBuffered = true;

            _assetManager = new AssetManager();
            _assetManager.LoadAssets();

            _engine = new WorldEngine();
            _uiManager = new UIManager();
            _engine.Initialize(_assetManager, 40, this.ClientSize.Width, this.ClientSize.Height);

            WorldGenerator.Seed = Environment.TickCount;

            float startX = WorldGenerator.WorldSize / 2f;
            float startY = WorldGenerator.WorldSize / 2f;
            _engine.InitializePlayer("sprites", 8, startX, startY);
            _engine.Camera.CenterOnWorldTile((int)startX, (int)startY);

            this.KeyDown += OnKeyDown;
            this.KeyUp += OnKeyUp;

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

            if (_engine.Player != null)
            {
                _engine.Player.Update(deltaTime, _moveUp, _moveLeft, _moveDown, _moveRight);
                _engine.Camera.CenterOnWorldPosition(_engine.Player.WorldX, _engine.Player.WorldY);
            }
            else
            {
                float speed = 400f * deltaTime;
                if (_moveUp)    _engine.Camera.Move(0, -speed);
                if (_moveDown)  _engine.Camera.Move(0, speed);
                if (_moveLeft)  _engine.Camera.Move(-speed, 0);
                if (_moveRight) _engine.Camera.Move(speed, 0);
            }

            _engine.Update(deltaTime);
            this.Invalidate();
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);

            e.Graphics.Clear(Color.Black);
            _assetManager.CurrentGraphics = e.Graphics;

            if (_showGlobalMap)
            {
                _engine.DrawGlobalMap(e.Graphics, ClientSize.Width, ClientSize.Height);
            }
            else
            {
                _engine.Draw(null!);
            }

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
                _engine.TryAddBuilding(coords.worldX, coords.worldY, 1.0f);
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _engine.Player?.Animator?.Dispose();
                _assetManager.Dispose();
                _gameTimer.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}
