using System;
using System.Numerics;

namespace WindowsFormsApp1
{
    public static class Extensions
    {
        public static double NextDouble(this Random @this, double min, double max)
        {
            // TODO support min/max here
            var r = @this.NextDouble();
            r = r - .5;
            r = r * 2;
            r = r * (max - min) / 2;
            r = r + min + (max - min) / 2;
            return r;
        }

        public static Vector2 NextVector2(this Random @this, double minX, double maxX, double minY, double maxY)
        {
            return new Vector2((float) @this.NextDouble(minX, maxX), (float)@this.NextDouble(minY, maxY));
        }
    }
}