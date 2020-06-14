using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using WindowsFormsApp1;
using WindowsFormsApp1.Csv;

namespace Tests
{
    class Program
    {
        static void Main(string[] args)
        {
            var nFrames = 6000;
            var size = new Size(700, 400);

            List<Particle> particles = null;
            if (File.Exists("input.csv"))
            {
                CsvSerializer.ParseCsv(File.ReadAllLines("input.xml"), out var options, out var outParticles);
                particles = outParticles.ToList();
            }
            else
            {
                particles = new List<Particle>();
                ParticlesGenerator.AddRandomParticles(particles, 20, 5, 1, size);
                var options = new Options(){NumberOfFrames = 20, Dimensions = new []{ 700, 400}};
                var serializedToCsv = CsvSerializer.ToCsvFixedWidth(options, particles);
                File.WriteAllText($"{DateTime.Now:yyyy-MM-dd--HH-mm-ss}.xml", serializedToCsv);
            }

            var wA = new Worker();
            var wB = new WorkerArray();

            var particlesA = particles.Select(x => x.Clone());
            var particlesB = particles.Select(x => x.Clone());

            var framesA = wA.Simulate(nFrames, particlesA, size);
            var framesB = wB.Simulate(nFrames, particlesB, size).ToList();

            var (framesWithDifferences, framesComparisons) = Tools.Compare(framesA, framesB);

            if (framesWithDifferences.Any())
            {
                Console.WriteLine($"First diff in frame {framesWithDifferences.First()}");
            }
            else
            {
                Console.WriteLine("No diff");
            }
            
            //foreach (var framesComparison in framesComparisons.OrderBy(x => x.Key).Select(x => x.Value))
            //{
            //    Console.WriteLine($"{framesComparison.TotalDiff}");
            //}
        }
    }
}
