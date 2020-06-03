using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using WindowsFormsApp1;

namespace Tests
{
    class Program
    {
        static void Main(string[] args)
        {
            var nFrames = 6000;
            var size = new Size(700, 400);

            List<Particle> particles;
            if (File.Exists("input.xml"))
            {
                particles = Tools.ReadFromFile("input.xml");
            }
            else
            {
                particles = new List<Particle>();
                ParticlesGenerator.AddRandomParticles(particles, 20, 5, size);
                Tools.DumpToFile(particles, $"{DateTime.Now:yyyy-MM-dd--HH-mm-ss}.xml");
            }

            var wA = new Worker();
            var wB = new WorkerArray();

            var particlesA = particles.Select(x => x.Clone());
            var particlesB = particles.Select(x => x.Clone());

            var framesA = wA.Simulate(nFrames, particlesA, size);
            var framesB = wB.Simulate(nFrames, particlesB, size);

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
