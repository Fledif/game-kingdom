// Розробник А: Гриценко Богдан Олександрович
// Проєкт: Custom C# Engine (WinForms Renderer)

using System;
using System.Windows.Forms;

namespace IsometricWorld.Demo
{
    static class Program
    {
        /// <summary>
        /// Головна точка входу для додатку.
        /// </summary>
        [STAThread]
        static void Main()
        {
            Application.SetHighDpiMode(HighDpiMode.SystemAware);
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            
            // Запуск головного ігрового вікна
            Application.Run(new GameWindow());
        }
    }
}
