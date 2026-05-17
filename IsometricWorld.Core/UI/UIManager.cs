// Розробник А: Гриценко Богдан Олександрович
// Проєкт: Custom C# Engine (Free-form Building & UI)

using System;
using System.Drawing;

namespace IsometricWorld.Core.UI
{
    public class UIManager
    {
        public bool IsBuildMenuOpen { get; private set; } = false;
        public bool IsBuildModeReady { get; private set; } = false;

        public void Draw(Graphics g, int screenWidth, int screenHeight)
        {
            // Головна Панель
            int panelHeight = 100;
            int panelY = screenHeight - panelHeight;
            
            using (Brush bgBrush = new SolidBrush(Color.FromArgb(200, 30, 30, 30)))
            {
                g.FillRectangle(bgBrush, 0, panelY, screenWidth, panelHeight);
            }

            // Кнопка Будівництва
            int btnSize = 60;
            int btnX = 20;
            int btnY = screenHeight - 80;

            using (Brush btnBrush = new SolidBrush(Color.DarkSlateGray))
            {
                g.FillRectangle(btnBrush, btnX, btnY, btnSize, btnSize);
            }

            // Умовний "молоток"
            Color hammerColor = IsBuildModeReady ? Color.LimeGreen : Color.LightGray;
            using (Pen hammerPen = new Pen(hammerColor, 3f))
            using (Brush hammerBrush = new SolidBrush(hammerColor))
            {
                // Ручка
                g.DrawLine(hammerPen, btnX + 20, btnY + 40, btnX + 40, btnY + 20);
                // Бойок
                g.FillRectangle(hammerBrush, btnX + 33, btnY + 13, 12, 10);
            }

            // Місце для майбутніх ресурсів
            int resWidth = 200;
            int resHeight = 60;
            int resX = screenWidth - resWidth - 20;
            int resY = screenHeight - 80;
            
            using (Pen resPen = new Pen(Color.Gray, 2f))
            {
                g.DrawRectangle(resPen, resX, resY, resWidth, resHeight);
            }

            // UI Меню Будівництва
            if (IsBuildMenuOpen)
            {
                int menuW = 400;
                int menuH = 300;
                int menuX = (screenWidth - menuW) / 2;
                int menuY = (screenHeight - menuH) / 2;

                // Тінь
                using (Brush shadow = new SolidBrush(Color.FromArgb(100, 0, 0, 0)))
                {
                    g.FillRectangle(shadow, menuX + 5, menuY + 5, menuW, menuH);
                }

                // Фон меню (Градієнт)
                using (var gradient = new System.Drawing.Drawing2D.LinearGradientBrush(
                    new Rectangle(menuX, menuY, menuW, menuH), 
                    Color.DarkSlateBlue, Color.FromArgb(255, 30, 30, 30), 
                    System.Drawing.Drawing2D.LinearGradientMode.ForwardDiagonal))
                {
                    g.FillRectangle(gradient, menuX, menuY, menuW, menuH);
                }

                // Яскрава рамка
                using (Pen borderPen = new Pen(Color.Gold, 2f))
                {
                    g.DrawRectangle(borderPen, menuX, menuY, menuW, menuH);
                }

                // Кнопка "Базовий Блок"
                int blockBtnSize = 80;
                int blockBtnX = menuX + 20;
                int blockBtnY = menuY + 20;

                using (Brush blockBtnBrush = new SolidBrush(Color.SlateGray))
                {
                    g.FillRectangle(blockBtnBrush, blockBtnX, blockBtnY, blockBtnSize, blockBtnSize);
                }

                using (Font f = new Font("Arial", 12, FontStyle.Bold))
                using (Brush textBrush = new SolidBrush(Color.White))
                {
                    g.DrawString("Block", f, textBrush, blockBtnX + 15, blockBtnY + 30);
                }
            }
        }

        public bool HandleMouseClick(int mouseX, int mouseY, int screenWidth, int screenHeight)
        {
            if (IsBuildMenuOpen)
            {
                int menuW = 400;
                int menuH = 300;
                int menuX = (screenWidth - menuW) / 2;
                int menuY = (screenHeight - menuH) / 2;
                
                int blockBtnSize = 80;
                int blockBtnX = menuX + 20;
                int blockBtnY = menuY + 20;

                // Клік по кнопці "Block"
                if (mouseX >= blockBtnX && mouseX <= blockBtnX + blockBtnSize &&
                    mouseY >= blockBtnY && mouseY <= blockBtnY + blockBtnSize)
                {
                    IsBuildModeReady = true;
                    IsBuildMenuOpen = false;
                    return true;
                }

                // Клік в межах меню (щоб не переходити у світ)
                if (mouseX >= menuX && mouseX <= menuX + menuW &&
                    mouseY >= menuY && mouseY <= menuY + menuH)
                {
                    return true;
                }
            }

            int btnSize = 60;
            int btnX = 20;
            int btnY = screenHeight - 80;

            if (mouseX >= btnX && mouseX <= btnX + btnSize &&
                mouseY >= btnY && mouseY <= btnY + btnSize)
            {
                IsBuildMenuOpen = !IsBuildMenuOpen;
                if (IsBuildMenuOpen) IsBuildModeReady = false;
                return true;
            }

            return false;
        }
    }
}
