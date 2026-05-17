// Розробник А: Гриценко Богдан Олександрович
// Проєкт: Ізометрична чанкова система (Об'єкти та Рендер)

using System.Runtime.InteropServices;

namespace IsometricWorld.Core.World
{
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct Prop
    {
        public int LocalX;
        public int LocalY;
        public PropType Type;

        public Prop(int localX, int localY, PropType type)
        {
            LocalX = localX;
            LocalY = localY;
            Type = type;
        }
    }
}
