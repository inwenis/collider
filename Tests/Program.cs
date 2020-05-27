using System;
using System.Collections.Generic;
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

            List<Particle> particles;
            if (File.Exists("input.xml"))
            {
                particles = Tools.ReadFromFile("input.xml");
            }
            else
            {
                particles = ParticlesGenerator.RandomParticles(20);
                Tools.DumpToFile(particles, $"{DateTime.Now:yyyy-MM-dd--HH-mm-ss}.xml");
            }

            var wA = new Worker();
            var wB = new WorkerArray();

            var particlesA = particles.Select(x => x.Clone());
            var particlesB = particles.Select(x => x.Clone());

            var framesA = wA.Simulate(nFrames, particlesA);
            var framesB = wB.Simulate(nFrames, particlesB);

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
