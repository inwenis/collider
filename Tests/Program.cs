using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using Collider;
using Collider.Csv;

namespace Tests
{
    class Program
    {
        static void Main()
        {
            var files = new []
            {
                "input_f1000_s1000x1000_n0010.csv",
                "input_f1000_s1000x1000_n0020.csv",
                "input_f1000_s1000x1000_n0040.csv",
                "input_f1000_s1000x1000_n0080.csv",
                "input_f1000_s1000x1000_n0160.csv",
                "input_f1000_s1000x1000_n0320.csv",
                "input_f1000_s1000x1000_n0640.csv",
                "input_f1000_s1000x1000_n1280.csv",
                "input_f1000_s1000x1000_n2560.csv",
                //"input_f1000_s1000x1000_n5120.csv",
            };

            Console.WriteLine($"{"file",40} {"seq",15} {"par_justAggregate",15} {"par_AsParallel.Aggregate",15} {"par_WhereAggregate",15} {"par_AsParallelWhereAggregate",15} {"par_AggregateUsingFor",15}");
            var functions = new Func<Particle[], float?[][], Collision>[]
            {
                WorkerArray_FindClosestPpCollisionSequential.FindClosestPpCollision,
                WorkerArray_FindClosestPpCollisionParallel.FindClosestPpCollision,
                WorkerArray_FindClosestPpCollisionParallel_AsParallelAggregation.FindClosestPpCollision,
                WorkerArray_FindClosestPpCollisionParallel_WhereAggregate.FindClosestPpCollision,
                WorkerArray_FindClosestPpCollisionParallel_AsParallelWhereAggregate.FindClosestPpCollision,
                WorkerArray_FindClosestPpCollisionParallel2.FindClosestPpCollision,
            };

            var builders = new Func<IWorker>[]
            {
                () => new WorkerArray_FindClosestPpCollisionSequential(),
                () => new WorkerArray_FindClosestPpCollisionParallel(),
                () => new WorkerArray_FindClosestPpCollisionParallel_AsParallelAggregation(),
                () => new WorkerArray_FindClosestPpCollisionParallel_WhereAggregate(),
                () => new WorkerArray_FindClosestPpCollisionParallel_AsParallelWhereAggregate(),
                () => new WorkerArray_FindClosestPpCollisionParallel2(),
            };

            Console.WriteLine("FindClosestPpCollision() measurement (sum of x runs)");
            foreach (var file in files)
            {
                var lines = File.ReadAllLines(file);
                CsvSerializer.ParseCsv(lines, out _, out var outParticles);
                var particlesArr = outParticles.ToArray();
                var ppCollisions = Array2D.Create<float?>(particlesArr.Length, particlesArr.Length);
                WorkerArray_FindClosestPpCollisionSequential.SetAllPpCollisions(particlesArr, ppCollisions, 0);

                // use Sum() instead of average because measurements are so small
                var sums = functions
                    .Select(x => MeasureFunction(particlesArr, ppCollisions, x, 10, 10000))
                    .Select(results => TimeSpan.FromMilliseconds(results.Sum(x => x.TotalMilliseconds)))
                    .ToArray();

                Console.WriteLine(BuildLineWithColors(file, sums.ToArray()));
            }

            Console.WriteLine("Complete simulation measurement (average)");
            foreach (var file in files)
            {
                var lines = File.ReadAllLines(file);
                CsvSerializer.ParseCsv(lines, out var options, out var outParticles);
                var particlesArr = outParticles.ToArray();

                var averages = builders
                    .Select(x => MeasureApp(particlesArr, options.NumberOfFrames, options.Size, x, 0, 1))
                    .Select(results => TimeSpan.FromMilliseconds(results.Average(x => x.TotalMilliseconds)))
                    .ToArray();

                Console.WriteLine(BuildLineWithColors(file, averages));
            }

            Console.WriteLine("Comparing workers");
            foreach (var file in files)
            {
                var lines = File.ReadAllLines(file);
                CsvSerializer.ParseCsv(lines, out var options, out var outParticles);
                var particlesArr = outParticles.ToArray();

                var (framesWithDifferences, framesComparisons) = CompareFrames(
                    particlesArr,
                    options.NumberOfFrames,
                    options.Size,
                    () => new WorkerArray_FindClosestPpCollisionSequential(),
                    () => new WorkerArray_FindClosestPpCollisionParallel2());

                var message = framesWithDifferences.Any()
                    ? $"First diff in frame {framesWithDifferences.First()}"
                    : "OK (no difference)";

                Console.WriteLine($"{file,-40} {message,40}");

                foreach (var framesComparison in framesComparisons.OrderBy(x => x.Key).Select(x => x.Value))
                {
                    Console.WriteLine($"{framesComparison.TotalDiff}");
                }
            }
        }

        private static string BuildLineWithColors(string file, IReadOnlyCollection<TimeSpan> all)
        {
            const string green = "\u001b[32m";
            const string white = "\u001b[37m";

            var line = $"{file,-40} ";
            var min = all.Min();
            foreach (var value in all)
            {
                if (value == min)
                {
                    line += $"{green}{value,15:G}{white} ";
                }
                else
                {
                    line += $"{value,15:G} ";
                }
            }

            return line;
        }

        static void WithRedirectedConsoleOut(Action action)
        {
            var originalConsoleOut = Console.Out;
            using (var writer = new StringWriter())
            {
                Console.SetOut(writer);
                action();
            }
            Console.SetOut(originalConsoleOut);
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

        private static (List<int> framesWithDifferences, Dictionary<int, FrameDiff> framesComparisons) CompareFrames(IReadOnlyCollection<Particle> particles, int nFrames, Size size, Func<IWorker> sutFactoryA, Func<IWorker> sutFactoryB)
        {
            var wA = sutFactoryA();
            var wB = sutFactoryB();

            var particlesA = particles.Select(x => x.Clone());
            var particlesB = particles.Select(x => x.Clone());

            List<Particle[]> framesA = null;
            List<Particle[]> framesB = null;

            WithRedirectedConsoleOut(() =>
            {
                framesA = wA.Simulate(particlesA, size).Take(nFrames).ToList();
                framesB = wB.Simulate(particlesB, size).Take(nFrames).ToList();
            });

            List<int> framesWithDifferences;
            Dictionary<int, FrameDiff> framesComparisons;
            (framesWithDifferences, framesComparisons) = Tools.Compare(framesA, framesB);
            return (framesWithDifferences, framesComparisons);
        }
    }
}
