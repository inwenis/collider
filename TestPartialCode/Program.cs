using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
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

            Console.WriteLine("FindClosestPpCollision measurement");
            Console.WriteLine($"{"file",40} {"seq",15} {"par",15}");
            foreach (var file in files)
            {
                var lines = File.ReadAllLines(file);
                CsvSerializer.ParseCsv(lines, out var options, out var outParticles);
                var particlesArr = outParticles.ToArray();
                var ppCollisions = Array2D.Create<float?>(particlesArr.Length, particlesArr.Length);
                WorkerArray_FindClosestPpCollisionSequential.SetAllPpCollisions(particlesArr, ppCollisions, 0);

                var ts = MeasureFunction(particlesArr, ppCollisions, WorkerArray_FindClosestPpCollisionSequential.FindClosestPpCollision);
                var tp = MeasureFunction(particlesArr, ppCollisions, WorkerArray_FindClosestPpCollisionParallel.FindClosestPpCollision);

                var tsAvg = TimeSpan.FromMilliseconds(ts.Average(x => x.TotalMilliseconds));
                var tpAvg = TimeSpan.FromMilliseconds(tp.Average(x => x.TotalMilliseconds));

                Console.WriteLine($"{file,-40} {tsAvg,15:G} {tpAvg,15:G}");
            }

            Console.WriteLine("Complete app measurement");
            Console.WriteLine($"{"file",40} {"seq",15} {"par",15}");
            foreach (var file in files)
            {
                var lines = File.ReadAllLines(file);
                CsvSerializer.ParseCsv(lines, out var options, out var outParticles);
                var particlesArr = outParticles.ToArray();

                var ts = MeasureApp(particlesArr, options.NumberOfFrames, options.Size, () => new WorkerArray_FindClosestPpCollisionSequential(), 0);
                var tp = MeasureApp(particlesArr, options.NumberOfFrames, options.Size, () => new WorkerArray_FindClosestPpCollisionParallel(), 0);

                var tsAvg = TimeSpan.FromMilliseconds(ts.Average(x => x.TotalMilliseconds));
                var tpAvg = TimeSpan.FromMilliseconds(tp.Average(x => x.TotalMilliseconds));

                Console.WriteLine($"{file,-40} {tsAvg,15:G} {tpAvg,15:G}");
            }
        }

        private static List<TimeSpan> MeasureFunction(Particle[] particlesArr, float?[][] ppCollisions, Func<Particle[], float?[][], Collision> fun, int nWarmups = 10, int nTests = 1000)
        {
            var results = new List<TimeSpan>();
            var dump = new List<Collision>(); // add results to this list to make sure calls are actually executed

            // warmups
            for (int i = 0; i < nWarmups; i++)
            {
                var result = fun(particlesArr, ppCollisions);
                dump.Add(result);
            }

            // actual measurements
            for (int i = 0; i < nTests; i++)
            {
                var sw = new Stopwatch();
                sw.Start();
                var result = fun(particlesArr, ppCollisions);
                sw.Stop();
                results.Add(sw.Elapsed);
                dump.Add(result);
            }

            return results;
        }

        private static List<TimeSpan> MeasureApp(IReadOnlyCollection<Particle> particles, int nFrames, Size size, Func<IWorker> sutFactory, int nWarmups = 2, int nTests = 4)
        {
            void WithRedirectedConsoleOut(Action action)
            {
                var originalConsoleOut = Console.Out;
                using (var writer = new StringWriter())
                {
                    Console.SetOut(writer);
                    action();
                }
                Console.SetOut(originalConsoleOut);
            }

            var sut = sutFactory();
            List<Particle[]> dump;

            WithRedirectedConsoleOut(() =>
            {
                // warmups
                for (int i = 0; i < nWarmups; i++)
                {
                    var particlesClone = particles.Select(x => x.Clone()).ToArray();
                    dump = sut.Simulate(particlesClone, size).Take(nFrames).ToList();
                }
            });

            var results = new List<TimeSpan>();
            WithRedirectedConsoleOut(() =>
            {
                // actual measurements
                for (int i = 0; i < nTests; i++)
                {
                    var particlesClone = particles.Select(x => x.Clone()).ToArray();
                    var sw = new Stopwatch();
                    sw.Start();
                    dump = sut.Simulate(particlesClone, size).Take(nFrames).ToList();
                    sw.Stop();
                    results.Add(sw.Elapsed);
                }
            });

            return results;
        }
    }
}
