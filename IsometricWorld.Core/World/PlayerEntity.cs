using System;
using System.Drawing;
using IsometricWorld.Core.Rendering;

namespace IsometricWorld.Core.World
{
    public sealed class PlayerEntity
    {
        public float WorldX { get; set; }
        public float WorldY { get; set; }

        public float VelocityX { get; private set; }
        public float VelocityY { get; private set; }

        public float Acceleration { get; set; } = 25.0f;
        public float MaxSpeed { get; set; } = 4.0f;
        public float Friction { get; set; } = 0.82f;

        public CharacterAnimator Animator { get; private set; }

        public PlayerEntity(float startX, float startY, CharacterAnimator animator)
        {
            WorldX = startX;
            WorldY = startY;
            Animator = animator;
        }

        public void Update(float deltaTime, bool w, bool a, bool s, bool d)
        {
            float worldDirX = 0;
            float worldDirY = 0;

            if (w) { worldDirX -= 1f; worldDirY -= 1f; }
            if (s) { worldDirX += 1f; worldDirY += 1f; }
            if (a) { worldDirX -= 1f; worldDirY += 1f; }
            if (d) { worldDirX += 1f; worldDirY -= 1f; }

            bool hasInput = (worldDirX != 0 || worldDirY != 0);

            if (hasInput)
            {
                float len = (float)Math.Sqrt(worldDirX * worldDirX + worldDirY * worldDirY);
                worldDirX /= len;
                worldDirY /= len;
            }

            VelocityX += worldDirX * Acceleration * deltaTime;
            VelocityY += worldDirY * Acceleration * deltaTime;

            if (!hasInput)
            {
                VelocityX *= Friction;
                VelocityY *= Friction;
            }

            float velSq = VelocityX * VelocityX + VelocityY * VelocityY;
            if (velSq > MaxSpeed * MaxSpeed)
            {
                float mag = (float)Math.Sqrt(velSq);
                VelocityX = (VelocityX / mag) * MaxSpeed;
                VelocityY = (VelocityY / mag) * MaxSpeed;
                velSq = MaxSpeed * MaxSpeed;
            }

            WorldX += VelocityX * deltaTime;
            WorldY += VelocityY * deltaTime;

            bool isMoving = velSq > 0.01f;

            if (hasInput)
            {
                Animator.Facing = GetScreenDirection(w, a, s, d);
            }

            if (isMoving)
            {
                Animator.Update(deltaTime);
            }
            else
            {
                VelocityX = 0;
                VelocityY = 0;
                Animator.ResetAnimation();
            }
        }

        private static FacingDirection GetScreenDirection(bool w, bool a, bool s, bool d)
        {
            if (w && a) return FacingDirection.UpLeft;
            if (w && d) return FacingDirection.UpRight;
            if (s && a) return FacingDirection.DownLeft;
            if (s && d) return FacingDirection.DownRight;
            if (w)      return FacingDirection.Up;
            if (s)      return FacingDirection.Down;
            if (a)      return FacingDirection.Left;
            if (d)      return FacingDirection.Right;
            return FacingDirection.Down;
        }

        public void Draw(Graphics g, Func<float, float, PointF> worldToScreen)
        {
            PointF screenPos = worldToScreen(WorldX, WorldY);
            Animator.Draw(g, screenPos.X, screenPos.Y);
        }
    }
}
