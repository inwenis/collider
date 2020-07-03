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
            MeasureTime(particles, options.NumberOfFrames, size);
        }

        private static void MeasureTime(List<Particle> particles, int nFrames, Size size)
        {
            var w = new WorkerArray();
            List<Particle[]> frames;
            var originalConsoleOut = Console.Out;

            using (var writer = new StringWriter())
            {
                Console.SetOut(writer);

                // warmup x5
                for (int i = 0; i < 5; i++)
                {
                    var particlesClone = particles.Select(x => x.Clone()).ToArray();
                    Console.WriteLine("----------------");
                    frames = w.Simulate(particlesClone, size).Take(nFrames).ToList();
                }

            }
            Console.SetOut(originalConsoleOut);

            var results = new List<TimeSpan>();
            using (var writer = new StringWriter())
            {
                Console.SetOut(writer);
                // test x10
                for (int i = 0; i < 10; i++)
                {
                    var particlesClone = particles.Select(x => x.Clone()).ToArray();
                    Console.WriteLine("----------------");
                    var sw = Stopwatch.StartNew();
                    frames = w.Simulate(particlesClone, size).Take(nFrames).ToList();
                    results.Add(sw.Elapsed);
                }
            }
            Console.SetOut(originalConsoleOut);

            var average = results.Average(x => x.TotalMilliseconds);
            Console.WriteLine(TimeSpan.FromMilliseconds(average));
        }

        private static void CompareWorkers(List<Particle> particles, int nFrames, Size size)
        {
            var wA = new Worker();
            var wB = new WorkerArray();

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
