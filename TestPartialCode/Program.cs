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
            var files = new []
            {
                @"c:\git\collider\f1000_n1000_800x800.csv",
                @"c:\git\collider\default_f10000_n800.csv",
                @"c:\git\collider\default_f10000_n400.csv",
                @"c:\git\collider\default_f10000.csv",
                @"c:\git\collider\default.csv",
                @"input.csv",
            };


            Console.WriteLine($"{"file",40} {"seq",15} {"par",15}");
            foreach (var file in files)
            {
                var lines = File.ReadAllLines(file);
                CsvSerializer.ParseCsv(lines, out var options, out var outParticles);
                var particlesArr = outParticles.ToArray();
                var ppCollisions = Array2D.Create<float?>(particlesArr.Length, particlesArr.Length);
                WorkerArray.SetAllPpCollisions(particlesArr, ppCollisions, 0);


                var (ts,x) = MeasureTime(particlesArr, ppCollisions, FindClosestPpCollision_Seq);
                var (tp,y) = MeasureTime(particlesArr, ppCollisions, FindClosestPpCollision_Parallel);
                Console.WriteLine($"{file,-40} {ts,15:G} {tp,15:G}");

                foreach (var (a, b) in x.Zip(y, (a, b) => (a, b)))
                {
                    if (a.Compare(b))
                    {
                        //Console.WriteLine("ok");
                    }
                    else
                    {
                        Console.WriteLine("what?!");
                    }
                }
            }
        }

        private static (TimeSpan, List<Collision> dump) MeasureTime(Particle[] particlesArr, float?[][] ppCollisions, Func<Particle[], float?[][], Collision> fun)
        {
            var results = new List<TimeSpan>();
            var dump = new List<Collision>();

            // warmups
            for (int i = 0; i < 10; i++)
            {
                //Console.WriteLine("----------------");
                var result = fun(particlesArr, ppCollisions);
                dump.Add(result);
                //Console.WriteLine(result);
            }

            // actual measurements
            for (int i = 0; i < 1000; i++)
            {
                //Console.WriteLine("----------------");
                var sw = new Stopwatch();

                sw.Start();
                var result = fun(particlesArr, ppCollisions);
                sw.Stop();

                dump.Add(result);
                results.Add(sw.Elapsed);
                //Console.WriteLine(result);
            }

            var average = results.Sum(x => x.TotalMilliseconds);
            return (TimeSpan.FromMilliseconds(average), dump);
        }

        private static Collision FindClosestPpCollision_Parallel(Particle[] particles, float?[][] ppCollisions)
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

        private static Collision FindClosestPpCollision_Seq(Particle[] particles, float?[][] ppCollisions)
        {
            // PP collision - particle - particle collision
            var minI = 0;
            var minJ = 0;
            var minDt = float.MaxValue;
            var collisionExists = false;
            for (var i = 0; i < particles.Length; i++)
            {
                for (var j = i + 1; j < particles.Length; j++)
                {
                    if (ppCollisions[i][j].HasValue && ppCollisions[i][j] < minDt)
                    {
                        minI = i;
                        minJ = j;
                        minDt = ppCollisions[i][j].Value;
                        collisionExists = true;
                    }
                }
            }

            return collisionExists
                ? new Collision(particles[minI], minI, particles[minJ], minJ, minDt)
                : null;
        }

    }
}
