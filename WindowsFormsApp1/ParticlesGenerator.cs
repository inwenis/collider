using System;
using System.Collections.Generic;
using System.Numerics;

namespace WindowsFormsApp1
{
    internal static class ParticlesGenerator
    {
        public static List<Particle> Particles(int nParticles)
        {
            var particles = GenerateRandomParticles(nParticles);
            var fastParticles = GenerateFastParticles(10);
            particles.AddRange(fastParticles);
            return particles;
        }

        public static List<Particle> GenerateRandomParticles(int nParticles)
        {
            var xPosMin = 0;
            var xPosMax = 200;

            var yPosMin = 0;
            var yPosMax = 200;

            var random = new Random(DateTimeOffset.UtcNow.Millisecond);
            var list = new List<Particle>();
            for (int i = 0; i < nParticles; i++)
            {
                var particle = new Particle
                {
                    Pos = new Vector2(random.Next(xPosMin, xPosMax), random.Next(yPosMin, yPosMax)),
                    Vel = new Vector2((float) random.NextDouble(-2.5, 2.5), (float)random.NextDouble(-2.5, 2.5))
                };
                list.Add(particle);
            }

            return list;
        }

        public static List<Particle> GenerateFastParticles(int count)
        {
            var particles2 = new List<Particle>();
            for (int i = 0; i < count; i++)
            {
                particles2.Add(new Particle
                {
                    Pos = new Vector2(800, i * 20),
                    Vel = new Vector2(-20, 0)
                });
            }

            return particles2;
        }
    }
}