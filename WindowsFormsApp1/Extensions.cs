using System;

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
    }
}