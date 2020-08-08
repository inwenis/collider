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
                //"input_f1000_s1000x1000_n2560.csv",
                //"input_f1000_s1000x1000_n5120.csv",
            };

            var workers = new (string, Func<Particle[], float?[][], Collision>, Func<IWorker>)[]
            {
                ("Sequential"        , Worker_Sequential.FindClosestPpCollision                        , () => new Worker_Sequential()),
                ("par_justAgg"       , Worker_Parallel_justAggregate.FindClosestPpCollision            , () => new Worker_Parallel_justAggregate()),
                ("par_AsParallelAgg" , Worker_Parallel_AsParallelAggregation.FindClosestPpCollision    , () => new Worker_Parallel_AsParallelAggregation()),
                ("par_WhereAgg"      , Worker_Parallel_WhereAggregate.FindClosestPpCollision           , () => new Worker_Parallel_WhereAggregate()),
                ("par_AsParWhereAgg" , Worker_Parallel_AsParallelWhereAggregate.FindClosestPpCollision , () => new Worker_Parallel_AsParallelWhereAggregate()),
                ("par_AggUsingFor"   , Worker_Parallel_AggreagteUsing_For.FindClosestPpCollision       , () => new Worker_Parallel_AggreagteUsing_For()),
            };

            var names = string.Join(" ", workers.Select(x => $"{x.Item1,23}"));
            Console.WriteLine($"{"file",-40} {names}");

            Console.WriteLine("FindClosestPpCollision() measurement (sum of x runs)");
            foreach (var file in files)
            {
                var lines = File.ReadAllLines(file);
                CsvSerializer.ParseCsv(lines, out _, out var outParticles);
                var particlesArr = outParticles.ToArray();
                var ppCollisions = Array2D.Create<float?>(particlesArr.Length, particlesArr.Length);
                Worker_Sequential.SetAllPpCollisions(particlesArr, ppCollisions, 0);

                // use Sum() instead of average because measurements are so small
                var sums = workers
                    .Select(x => x.Item2)
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

                var averages = workers
                    .Select(x => x.Item3)
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

                List<Particle[]> templateFrames = null;
                var templateWorker = new Worker_Parallel_justAggregate();
                var particlesClone = particlesArr.Select(x => x.Clone());
                WithRedirectedConsoleOut(() =>
                {
                    templateFrames = templateWorker.Simulate(particlesClone, options.Size).Take(options.NumberOfFrames).ToList();
                });

                var messages = workers
                    .Select(x => x.Item3)
                    .Select(x => CompareFrames(templateFrames, particlesArr, options.NumberOfFrames, options.Size, x))
                    .Select(x => x.framesWithDifferences.Any() ? $"fst df frame {x.framesWithDifferences.First(),5}" : "OK (no difference)")
                    .ToArray();

                Console.WriteLine($"{file,-40} {string.Join(" ", messages.Select(x => $"{x,23}"))}");
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
                    line += $"{green}{value,23:G}{white} ";
                }
                else
                {
                    line += $"{value,23:G} ";
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

        private static (List<int> framesWithDifferences, Dictionary<int, FrameDiff> framesComparisons) CompareFrames(
            List<Particle[]> template,
            IReadOnlyCollection<Particle> particles,
            int nFrames,
            Size size,
            Func<IWorker> sutFactory)
        {
            var sut = sutFactory();
            var particlesClone = particles.Select(x => x.Clone());

            List<Particle[]> frames = null;

            WithRedirectedConsoleOut(() =>
            {
                frames = sut.Simulate(particlesClone, size).Take(nFrames).ToList();
            });

            List<int> framesWithDifferences;
            Dictionary<int, FrameDiff> framesComparisons;
            (framesWithDifferences, framesComparisons) = Tools.Compare(template, frames);
            return (framesWithDifferences, framesComparisons);
        }
    }
}
