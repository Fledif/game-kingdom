using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;

namespace IsometricWorld.Core.Rendering
{
    public enum FacingDirection
    {
        Down = 0,
        DownLeft = 1,
        Left = 2,
        UpLeft = 3,
        Up = 4,
        UpRight = 5,
        Right = 6,
        DownRight = 7
    }

    public sealed class CharacterAnimator : IDisposable
    {
        private struct FrameData
        {
            public Bitmap Image;
            public int PivotX;
        }

        private FrameData[] _downFrames;
        private FrameData[] _downRightFrames;
        private FrameData[] _rightFrames;
        private FrameData[] _upRightFrames;
        private FrameData[] _upFrames;

        private readonly int _frameCount;
        private float _currentFrameTime;

        public float FramesPerSecond { get; set; } = 10f;
        public float Scale { get; set; } = 0.2f;
        public FacingDirection Facing { get; set; } = FacingDirection.Down;

        public CharacterAnimator(string spritesFolder, int framesPerDirection)
        {
            _frameCount = framesPerDirection;
            string basePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, spritesFolder);
            if (!Directory.Exists(basePath))
                basePath = Path.GetFullPath(spritesFolder);

            _downFrames       = LoadFrames(basePath, "south");
            _downRightFrames  = LoadFrames(basePath, "southwest");
            _rightFrames      = LoadFrames(basePath, "left");
            _upRightFrames    = LoadFrames(basePath, "northeast");
            _upFrames         = LoadFrames(basePath, "north");
        }

        private FrameData[] LoadFrames(string basePath, string prefix)
        {
            var frames = new FrameData[_frameCount];
            for (int i = 0; i < _frameCount; i++)
            {
                string path = Path.Combine(basePath, $"{prefix}_walk_{i + 1:D2}.png");
                if (File.Exists(path))
                {
                    Bitmap bmp = new Bitmap(path);
                    int minX = bmp.Width, maxX = 0;
                    
                    for (int y = 0; y < bmp.Height; y++)
                    {
                        for (int x = 0; x < bmp.Width; x++)
                        {
                            if (bmp.GetPixel(x, y).A > 10)
                            {
                                if (x < minX) minX = x;
                                if (x > maxX) maxX = x;
                            }
                        }
                    }
                    
                    int pivot = minX <= maxX ? (minX + maxX) / 2 : bmp.Width / 2;
                    frames[i] = new FrameData { Image = bmp, PivotX = pivot };
                }
                else
                {
                    frames[i] = new FrameData { Image = new Bitmap(64, 64), PivotX = 32 };
                }
            }
            return frames;
        }

        public void Update(float deltaTime)
        {
            _currentFrameTime += deltaTime * FramesPerSecond;
            if (_currentFrameTime >= _frameCount)
                _currentFrameTime %= _frameCount;
        }

        public void ResetAnimation()
        {
            _currentFrameTime = 0;
        }

        public void Draw(Graphics g, float anchorX, float anchorY)
        {
            int frameIndex = (int)Math.Floor(_currentFrameTime) % _frameCount;

            FrameData frame;
            bool mirror = false;

            switch (Facing)
            {
                case FacingDirection.Down:      frame = _downFrames[frameIndex]; break;
                case FacingDirection.Up:        frame = _upFrames[frameIndex]; break;
                case FacingDirection.Right:     frame = _rightFrames[frameIndex]; break;
                case FacingDirection.DownRight: frame = _downRightFrames[frameIndex]; break;
                case FacingDirection.UpRight:   frame = _upRightFrames[frameIndex]; break;

                case FacingDirection.Left:      frame = _rightFrames[frameIndex]; mirror = true; break;
                case FacingDirection.DownLeft:  frame = _downRightFrames[frameIndex]; mirror = true; break;
                case FacingDirection.UpLeft:    frame = _upRightFrames[frameIndex]; mirror = true; break;

                default: frame = _downFrames[frameIndex]; break;
            }

            int drawW = (int)(frame.Image.Width * Scale);
            int drawH = (int)(frame.Image.Height * Scale);
            float scaledPivotX = frame.PivotX * Scale;

            int destX;
            int destY = (int)Math.Round(anchorY - drawH);

            if (mirror)
            {
                destX = (int)Math.Round(anchorX - drawW + scaledPivotX);
            }
            else
            {
                destX = (int)Math.Round(anchorX - scaledPivotX);
            }

            var oldInterp = g.InterpolationMode;
            var oldOffset = g.PixelOffsetMode;
            g.InterpolationMode = InterpolationMode.NearestNeighbor;
            g.PixelOffsetMode = PixelOffsetMode.Half;

            if (mirror)
            {
                var saved = g.Save();
                g.TranslateTransform(destX + drawW, destY);
                g.ScaleTransform(-1, 1);
                g.DrawImage(frame.Image, 0, 0, drawW, drawH);
                g.Restore(saved);
            }
            else
            {
                g.DrawImage(frame.Image, destX, destY, drawW, drawH);
            }

            g.InterpolationMode = oldInterp;
            g.PixelOffsetMode = oldOffset;
        }

        public void Dispose()
        {
            DisposeFrames(_downFrames);
            DisposeFrames(_downRightFrames);
            DisposeFrames(_rightFrames);
            DisposeFrames(_upRightFrames);
            DisposeFrames(_upFrames);
        }

        private static void DisposeFrames(FrameData[] frames)
        {
            if (frames == null) return;
            foreach (var f in frames) f.Image?.Dispose();
        }
    }
}
