using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using WindowsFormsApp1;
using WindowsFormsApp1.Csv;

namespace Tests
{
    class Program
    {
        static void Main()
        {
            var inputCsv = "input.csv";
            var nFrames = 6000;
            int nPartic = 200;
            var size = new Size(700, 400);

            List<Particle> particles = null;
            if (File.Exists(inputCsv))
            {
                CsvSerializer.ParseCsv(File.ReadAllLines(inputCsv), out var options, out var outParticles);
                particles = outParticles.ToList();
            }
            else
            {
                particles = new List<Particle>();
                ParticlesGenerator.AddRandomParticles(particles, nPartic, 5, 1, size);
                var options = new Options(){NumberOfFrames = nFrames, Dimensions = new []{ size.Width, size.Height}};
                var serializedToCsv = CsvSerializer.ToCsvFixedWidth(options, particles);
                File.WriteAllText($"{DateTime.Now:yyyy-MM-dd--HH-mm-ss}.csv", serializedToCsv);
            }

            //CompareWorkers(particles, nFrames, size);
            MeasureTime(particles, nFrames, size);
        }

        private static void MeasureTime(List<Particle> particles, int nFrames, Size size)
        {
            var w = new WorkerArray();
            List<Particle[]> frames;

            // warmup x5
            for (int i = 0; i < 5; i++)
            {
                frames = w.Simulate(particles, size).Take(nFrames).ToList();
            }

            // test x10
            var results = new List<TimeSpan>();
            for (int i = 0; i < 10; i++)
            {
                var sw = Stopwatch.StartNew();
                frames = w.Simulate(particles, size).Take(nFrames).ToList();
                results.Add(sw.Elapsed);
            }

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
