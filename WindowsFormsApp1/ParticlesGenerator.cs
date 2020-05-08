using System;
using System.Collections.Generic;
using System.Numerics;

namespace WindowsFormsApp1
{
    internal static class ParticlesGenerator
    {
        public static List<Particle> RandomParticles(int count)
        {
            var xPosMin = 6;
            var xPosMax = 200;

            var yPosMin = 6;
            var yPosMax = 200;

            var random = new Random(DateTimeOffset.UtcNow.Millisecond);
            var list = new List<Particle>();
            for (int i = 0; i < count; i++)
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

        public static List<Particle> RandomFastParticles(int count)
        {
            var particles = new List<Particle>();
            for (int i = 0; i < count; i++)
            {
                particles.Add(new Particle
                {
                    Pos = new Vector2(800, (i + 1) * 10),
                    Vel = new Vector2(-20, 0)
                });
            }

            return particles;
        }
    }
}