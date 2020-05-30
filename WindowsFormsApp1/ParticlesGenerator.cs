using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Numerics;

namespace WindowsFormsApp1
{
    public static class ParticlesGenerator
    {
        public static List<Particle> RandomParticles(int count, int s, Size size)
        {
            var random = new Random(DateTimeOffset.UtcNow.Millisecond);
            var list = new List<Particle>();
            for (int i = 0; i < count; i++)
            {
                var particle = new Particle
                {
                    Pos = NextNonCollidingPosition(random, list, s, size),
                    Vel = random.NextVector2(-2.5, 2.5, -2.5, 2.5),
                    Sig = s
                };
                list.Add(particle);
            }

            return list;
        }

        private static Vector2 NextNonCollidingPosition(Random random, List<Particle> existingParticles, int s, Size size)
        {
            Vector2 position;
            int nonCollidingPpDistance = 2 * s + 1; // +1 is added to make sure particles do not overlap
            int nonCollidingPwDistance = s + 1;
            do
            {
                // below numbers make sure particles do not overlap with walls
                position = random.NextVector2(nonCollidingPwDistance, size.Width - nonCollidingPwDistance, nonCollidingPwDistance, size.Height - nonCollidingPwDistance);
            } while (existingParticles.Any(x => (x.Pos - position).Length() < nonCollidingPpDistance));

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