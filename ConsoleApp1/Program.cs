using System;
using System.Numerics;

namespace ConsoleApp1
{
    class Program
    {
        static void Main(string[] args)
        {
            Vector2 v = Vector2.One;
            Console.WriteLine(v);
            Console.WriteLine(v * new Vector2(-1, 1));
        }
    }
}
