using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;

namespace WindowsFormsApp1
{
    public static class ParticlesGenerator
    {
        public static List<Particle> RandomParticles(int count)
        {
            var random = new Random(DateTimeOffset.UtcNow.Millisecond);
            var list = new List<Particle>();
            for (int i = 0; i < count; i++)
            {
                var particle = new Particle
                {
                    Pos = NextNonCollidingPosition(random, list),
                    Vel = random.NextVector2(-2.5, 2.5, -2.5, 2.5)
                };
                list.Add(particle);
            }

            return list;
        }

        private static Vector2 NextNonCollidingPosition(Random random, List<Particle> existingParticles)
        {
            Vector2 position;
            const int sigma = 5;
            const int nonCollidingDistance = 2 * sigma + 1; // +1 is added to make sure particles do not overlap
            do
            {
                position = random.NextVector2(6, 200, 6, 200);
            } while (existingParticles.Any(x => (x.Pos - position).Length() < nonCollidingDistance));

            return position;
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