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
        static void Main(string[] args)
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
                "input_f1000_s1000x1000_n5120.csv",
            };

            Console.WriteLine($"{"file",40} {"seq",15} {"par",15} {"par2",15}");

            Console.WriteLine("FindClosestPpCollision() measurement (sum of x runs)");
            foreach (var file in files)
            {
                var lines = File.ReadAllLines(file);
                CsvSerializer.ParseCsv(lines, out var options, out var outParticles);
                var particlesArr = outParticles.ToArray();
                var ppCollisions = Array2D.Create<float?>(particlesArr.Length, particlesArr.Length);
                WorkerArray_FindClosestPpCollisionSequential.SetAllPpCollisions(particlesArr, ppCollisions, 0);

                var timesSeq  = MeasureFunction(particlesArr, ppCollisions, WorkerArray_FindClosestPpCollisionSequential.FindClosestPpCollision, 10, 10000);
                var timesPar  = MeasureFunction(particlesArr, ppCollisions, WorkerArray_FindClosestPpCollisionParallel.FindClosestPpCollision, 10, 10000);
                var timesPar2 = MeasureFunction(particlesArr, ppCollisions, WorkerArray_FindClosestPpCollisionParallel2.FindClosestPpCollision, 10, 10000);

                // use Sum() instead of average because measurements are so small
                var sumts  = TimeSpan.FromMilliseconds(timesSeq.Sum(x => x.TotalMilliseconds));
                var sumtp  = TimeSpan.FromMilliseconds(timesPar.Sum(x => x.TotalMilliseconds));
                var sumtp2 = TimeSpan.FromMilliseconds(timesPar2.Sum(x => x.TotalMilliseconds));

                Console.WriteLine($"{file,-40} {sumts,15:G} {sumtp,15:G} {sumtp2,15:G}");
            }

            Console.WriteLine("Complete simulation measurement (average)");
            foreach (var file in files)
            {
                var lines = File.ReadAllLines(file);
                CsvSerializer.ParseCsv(lines, out var options, out var outParticles);
                var particlesArr = outParticles.ToArray();

                var timesSeq  = MeasureApp(particlesArr, options.NumberOfFrames, options.Size, () => new WorkerArray_FindClosestPpCollisionSequential(), 1, 4);
                var timesPar  = MeasureApp(particlesArr, options.NumberOfFrames, options.Size, () => new WorkerArray_FindClosestPpCollisionParallel(), 1, 4);
                var timesPar2 = MeasureApp(particlesArr, options.NumberOfFrames, options.Size, () => new WorkerArray_FindClosestPpCollisionParallel2(), 1, 4);

                var avgs  = TimeSpan.FromMilliseconds(timesSeq.Average(x => x.TotalMilliseconds));
                var avgp  = TimeSpan.FromMilliseconds(timesPar.Average(x => x.TotalMilliseconds));
                var avgp2 = TimeSpan.FromMilliseconds(timesPar2.Average(x => x.TotalMilliseconds));

                Console.WriteLine($"{file,-40} {avgs,15:G} {avgp,15:G} {avgp2,15:G}");
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

                if (framesWithDifferences.Any())
                {
                    Console.WriteLine($"First diff in frame {framesWithDifferences.First()}");
                }
                else
                {
                    Console.WriteLine("No diff");
                }

                foreach (var framesComparison in framesComparisons.OrderBy(x => x.Key).Select(x => x.Value))
                {
                    Console.WriteLine($"{framesComparison.TotalDiff}");
                }
            }
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
