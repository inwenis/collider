using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Numerics;

namespace WindowsFormsApp1
{
    public static class ParticlesGenerator
    {
        public static void AddRandomParticles(List<Particle> existingParticles, int count, int sig, float mass, Size size)
        {
            var random = new Random(DateTimeOffset.UtcNow.Millisecond);
            for (int i = 0; i < count; i++)
            {
                var particle = new Particle
                {
                    Pos = NextNonCollidingPosition(random, existingParticles, sig, size),
                    Vel = random.NextVector2(-2.5, 2.5, -2.5, 2.5),
                    Sig = sig,
                    Mass = mass
                };
                existingParticles.Add(particle);
            }
        }

        private static Vector2 NextNonCollidingPosition(Random random, List<Particle> existingParticles, int s, Size size)
        {
            Vector2 position;
            int nonCollidingPwDistance = s + 1;
            do
            {
                // below numbers make sure particles do not overlap with walls
                position = random.NextVector2(nonCollidingPwDistance, size.Width - nonCollidingPwDistance, nonCollidingPwDistance, size.Height - nonCollidingPwDistance);
            } while (existingParticles.Any(x => (x.Pos - position).Length() < (s + x.Sig + 0.5)));

            return position;
        }
    }
}