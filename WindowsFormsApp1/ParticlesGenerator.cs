using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Numerics;

namespace WindowsFormsApp1
{
    public static class ParticlesGenerator
    {
        public static List<Particle> RandomParticles(int count, Size size)
        {
            var random = new Random(DateTimeOffset.UtcNow.Millisecond);
            var list = new List<Particle>();
            for (int i = 0; i < count; i++)
            {
                var particle = new Particle
                {
                    Pos = NextNonCollidingPosition(random, list, size),
                    Vel = random.NextVector2(-2.5, 2.5, -2.5, 2.5)
                };
                list.Add(particle);
            }

            return list;
        }

        private static Vector2 NextNonCollidingPosition(Random random, List<Particle> existingParticles, Size size)
        {
            Vector2 position;
            const int sigma = 5;
            const int nonCollidingDistance = 2 * sigma + 1; // +1 is added to make sure particles do not overlap
            const int nonCollidingDistanceFromWall = sigma + 1;
            do
            {
                // below numbers make sure particles do not overlap with walls
                position = random.NextVector2(nonCollidingDistanceFromWall, size.Width - nonCollidingDistanceFromWall, nonCollidingDistanceFromWall, size.Height - nonCollidingDistanceFromWall);
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