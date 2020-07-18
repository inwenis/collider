using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using WindowsFormsApp1;
using WindowsFormsApp1.Csv;
using CommandLine;

namespace Tests
{
    class Program
    {
        static void Main(string[] args)
        {
            var parserResult = Parser.Default.ParseArguments<Options>(args);
            parserResult.WithParsed(RunApp);
        }

        static void RunApp(Options options)
        {
            Size size;

            List<Particle> particles;
            if (options.ParticlesFile != null)
            {
                var lines = File.ReadAllLines(options.ParticlesFile);
                // not super happy about this code cuz we overwrite options here
                CsvSerializer.ParseCsv(lines, out options, out var outParticles);
                particles = outParticles.ToList();

                var dimensions = options.Dimensions.ToArray();
                size = new Size(dimensions[0], dimensions[1]);
            }
            else
            {
                var dimensions = options.Dimensions.ToArray();
                size = new Size(dimensions[0], dimensions[1]);

                particles = new List<Particle>();
                ParticlesGenerator.AddRandomParticles(particles, options.NumberOfParticles, options.Radius, 1, size);

                var serializedToCsv = CsvSerializer.ToCsvFixedWidth(options, particles);
                var fileName = $"{DateTime.Now:yyyy-MM-dd--HH-mm-ss}.csv";
                File.WriteAllText(fileName, serializedToCsv);
                Console.WriteLine($"Particles saved to {fileName}. To rerun use: --file={fileName}");
            }

            //CompareWorkers(particles, nFrames, size);
            MeasureTime(particles, options.NumberOfFrames, size, 0, 1);
        }

        private static List<TimeSpan> MeasureTime(List<Particle> particles, int nFrames, Size size, int nWarmups, int nTests)
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

            var w = new WorkerArray_FindClosestPpCollisionSequential();
            List<Particle[]> dump;

            WithRedirectedConsoleOut(() =>
            {
                // warmups
                for (int i = 0; i < nWarmups; i++)
                {
                    var particlesClone = particles.Select(x => x.Clone()).ToArray();
                    dump = w.Simulate(particlesClone, size).Take(nFrames).ToList();
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
                    dump = w.Simulate(particlesClone, size).Take(nFrames).ToList();
                    sw.Stop();
                    results.Add(sw.Elapsed);
                }
            });

            return results;
        }

        private static void CompareWorkers(List<Particle> particles, int nFrames, Size size)
        {
            var wA = new Worker();
            var wB = new WorkerArray_FindClosestPpCollisionSequential();

            var particlesA = particles.Select(x => x.Clone());
            var particlesB = particles.Select(x => x.Clone());

            var framesA = wA.Simulate(nFrames, particlesA, size);
            var framesB = wB.Simulate(particlesB, size).Take(nFrames).ToList();

            var (framesWithDifferences, framesComparisons) = Tools.Compare(framesA, framesB);

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
}
