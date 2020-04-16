using System;
using System.Numerics;

namespace collider
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Hello World!");

            CheckCollision(new Vector2(1, 2), new Vector2(1, 2), new Vector2(3, 2), new Vector2(3, 2));
            CheckCollision(new Vector2(1, 1), new Vector2(0, 0), new Vector2(1, 10), new Vector2(0, -1));
        }

        // r - position
        // v - velocity
        private static void CheckCollision(Vector2 ri, Vector2 vi, Vector2 rj, Vector2 vj)
        {
            double t = 1; // current time

            double si = 1; // sigma, radius

            double sj = 1;

            Vector2 dr = rj - ri;
            Vector2 dv = vj - vi;

            var s_pow_2 = Math.Pow(si + sj, 2);

            var dvdr = dv.X * dr.X + dv.Y * dr.Y;
            var dvdv = Math.Pow(dv.X, 2) + Math.Pow(dv.Y, 2);
            var drdr = Math.Pow(dr.X, 2) + Math.Pow(dr.Y, 2);
            var d = Math.Pow(dvdr, 2) - dvdv * (drdr - s_pow_2);
            if (dvdr >= 0)
            {
                Console.WriteLine("no collision");
            }
            else if (d < 0)
            {
                Console.WriteLine("no collision");
            }
            else
            {
                var dt = - (dvdr + Math.Sqrt(d)) / dvdv;
                Console.WriteLine($"collision at {dt}");
            }
        }
    }
}
