using System;

namespace KitchenDesign.Shared.Enums
{
    [Flags]
    public enum CutoutFaces
    {
        None = 0,
        XMax = 1,
        XMin = 2,
        YMax = 4,
        YMin = 8,
        ZMax = 16,
        ZMin = 32
    }
}
