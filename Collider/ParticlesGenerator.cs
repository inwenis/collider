using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Numerics;

namespace Collider
{
    public static class ParticlesGenerator
    {
        public static void AddRandomParticles(List<Particle> existingParticles, int count, int sig, float mass, Size size)
        {
            ValidateInputParameters(count, sig, size);
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

        private static void ValidateInputParameters(int count, int sig, Size size)
        {
            // check if particles can fit into given space without overlapping
            var xn = size.Width / (2 * sig); // max particles in x axis
            var yn = size.Height / (2 * sig); // max particles in y axis
            var max = xn * yn; // max particles in space of given size
            var saturation = (double) count / max;
            // threshold below were checked empirically
            if (saturation > 0.7)
            {
                throw new Exception($"You have requested {count} particles in space {size}. Requested density of particles is very high and randomly generated particles might not fit or randomization will take tens of minutes.");
            }
            else if (saturation > 0.6)
            {
                Console.ForegroundColor = ConsoleColor.DarkYellow;
                Console.WriteLine($"WARNING - you have requested {count} particles in space {size} (saturation={saturation}). Requested density of particles is high and randomly generated particles might not fit or randomization will take minutes.");
                Console.ResetColor();
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