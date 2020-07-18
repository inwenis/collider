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
                @"c:\git\collider\input_big.csv",
            };

            Console.WriteLine($"{"file",40} {"seq",15} {"par",15}");
            foreach (var file in files)
            {
                var lines = File.ReadAllLines(file);
                CsvSerializer.ParseCsv(lines, out var options, out var outParticles);
                var particlesArr = outParticles.ToArray();
                var ppCollisions = Array2D.Create<float?>(particlesArr.Length, particlesArr.Length);
                WorkerArray_FindClosestPpCollisionSequential.SetAllPpCollisions(particlesArr, ppCollisions, 0);


                var (ts,x) = MeasureTime(particlesArr, ppCollisions, WorkerArray_FindClosestPpCollisionSequential.FindClosestPpCollision);
                var (tp,y) = MeasureTime(particlesArr, ppCollisions, WorkerArray_FindClosestPpCollisionParallel.FindClosestPpCollision);
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
    }
}
