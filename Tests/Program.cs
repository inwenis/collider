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
            var nFrames = 2000;

            List<Particle> particles;
            if (File.Exists("input.xml"))
            {
                particles = Tools.ReadFromFile("input.xml");
            }
            else
            {
                particles = ParticlesGenerator.RandomParticles(20);
                var fastParticles = ParticlesGenerator.RandomFastParticles(10);
                particles.AddRange(fastParticles);
                Tools.DumpToFile(particles, $"{DateTime.Now:yyyy-MM-dd--HH-mm-ss}.xml");
            }

            var particlesClone = particles.Select(x => x.Clone()).ToList();

            var w = new Worker();
            var frames = w.Simulate(nFrames, particles);

            var wa = new WorkerArray();
            var framesA = wa.Simulate(nFrames, particlesClone);

            var (framesWithDifferences, framesComparisons) = Tools.Compare(frames, framesA);

            Console.WriteLine($"First diff in frame {framesWithDifferences.First()}");
            foreach (var framesComparison in framesComparisons.OrderBy(x => x.Key).Select(x => x.Value))
            {
                Console.WriteLine($"{framesComparison.TotalDiff}");
            }
        }
    }
}
