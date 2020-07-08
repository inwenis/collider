using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using WindowsFormsApp1;
using WindowsFormsApp1.Csv;

namespace TestPartialCode
{
    class Program
    {
        static void Main(string[] args)
        {
            var lines = File.ReadAllLines("input.csv");
            CsvSerializer.ParseCsv(lines, out var options, out var outParticles);
            var particlesArr = outParticles.ToArray();
            var ppCollisions = Array2D.Create<float?>(particlesArr.Length, particlesArr.Length);

            WorkerArray.SetAllPpCollisions(particlesArr, ppCollisions, 0);

            MeasureTime(particlesArr, ppCollisions);
        }

        private static void MeasureTime(Particle[] particlesArr, float?[][] ppCollisions)
        {
            var results = new List<TimeSpan>();

            // warmups
            for (int i = 0; i < 0; i++)
            {
                Console.WriteLine("----------------");
                var result = FindClosestPpCollision(particlesArr, ppCollisions);
                Console.WriteLine(result);
            }

            // actual measurements
            for (int i = 0; i < 1; i++)
            {
                Console.WriteLine("----------------");
                var sw = new Stopwatch();

                sw.Start();
                var result = FindClosestPpCollision(particlesArr, ppCollisions);
                sw.Stop();

                results.Add(sw.Elapsed);
                Console.WriteLine(result);
            }

            var average = results.Average(x => x.TotalMilliseconds);
            Console.WriteLine(TimeSpan.FromMilliseconds(average));
        }


        private static Collision FindClosestPpCollision(Particle[] particles, float?[][] ppCollisions)
        {
            // PP collision - particle - particle collision
            var minI = 0;
            var minJ = 0;
            var collisionExists = false;

            var temp = new (int, int, double?)[particles.Length];

            Parallel.For(0, particles.Length, i =>
            {
                var minDt = float.MaxValue;
                for (var j = i + 1; j < particles.Length; j++)
                {
                    if (ppCollisions[i][j].HasValue && ppCollisions[i][j] < minDt)
                    {
                        temp[i].Item1 = i;
                        temp[i].Item2 = j;
                        temp[i].Item3 = minDt = ppCollisions[i][j].Value;
                    }
                }
            });

            var accumulate = temp.Aggregate((0, 0, (double?)null), (acc, y) =>
            {
                if (!acc.Item3.HasValue && !y.Item3.HasValue)
                {
                    return acc;
                }

                if (!acc.Item3.HasValue)
                {
                    return y;
                }

                if (!y.Item3.HasValue)
                {
                    return acc;
                }

                if (y.Item3 < acc.Item3)
                {
                    return y;
                }
                return acc;
            });

            double? accumulateItem3 = accumulate.Item3;
            return accumulate.Item3.HasValue
                ? new Collision(particles[accumulate.Item1], accumulate.Item1, particles[accumulate.Item2], accumulate.Item2, (float)accumulateItem3.Value)
                : null;
        }

    }
}
