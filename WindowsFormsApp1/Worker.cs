using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Numerics;

namespace WindowsFormsApp1
{
    public class Worker
    {
        public List<Frame> Simulate(int nFrames)
        {
            int nParticles = 20;
            double step = 0.1; // seconds
            double t = 0; // current time

            var frames = new List<Frame>();

            var particles = GenerateRandomParticles(nParticles);
            var fastParticles = GenerateFastParticles(10);
            particles.AddRange(fastParticles);

            // get frame 0
            frames.Add(new Frame {Positions = particles.Select(x => x.Pos).ToList()});

            while (frames.Count < nFrames)
            {
                // get next collision
                var closestCollision = CheckCollisionAll(particles);
                var nextCollisionTime = closestCollision?.Value + t ?? double.MaxValue;
                while (t + step < nextCollisionTime && frames.Count < nFrames)
                {
                    Move(particles, (float) step);
                    frames.Add(new Frame {Positions = particles.Select(x => x.Pos).ToList()});
                    t += step;
                }

                if (frames.Count >= nFrames)
                {
                    break;
                }

                // simulate frames till next collision
                float beforeCollision = (float) (nextCollisionTime - t);
                float afterCollision = (float) (step - beforeCollision);

                Move(particles, beforeCollision);
                t += beforeCollision;

                ApplyCollision(closestCollision);

                Move(particles, afterCollision);
                t += afterCollision;
            }

            Debug.WriteLine($"Computed: {frames.Count} frames");

            return frames;
        }

        private List<Particle> GenerateRandomParticles(int nParticles)
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
                    Vel = new Vector2((float)NextDouble(random), (float)NextDouble(random))
                };
                list.Add(particle);
            }

            return list;
        }

        private List<Particle> GenerateFastParticles(int count)
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

        private void ApplyCollision((Particle i, Particle j, double Value)? closestCollision)
        {
            (Particle i, Particle j, _) = closestCollision.Value;
            var mi = 1;
            var mj = 1;

            double si = 5; // sigma, radius
            double sj = 5;

            var sigma = si + sj;

            Vector2 dr = j.Pos - i.Pos;

            Vector2 dv = j.Vel - i.Vel;

            var dvdr = dv.X * dr.X + dv.Y * dr.Y;

            var J = (2 * mi * mj) * dvdr/ (float) (sigma * (mi + mj));

            var Jx = (J * dr.X) / sigma;
            var Jy = (J * dr.Y) / sigma;

            var vxip = (i.Vel.X + Jx) / mi;
            var vyip = (i.Vel.Y + Jy) / mi;

            var vxjp = (j.Vel.X - Jx) / mj;
            var vyjp = (j.Vel.Y - Jy) / mj;

            i.Vel = new Vector2((float)vxip, (float)vyip);
            j.Vel = new Vector2((float)vxjp, (float)vyjp);
        }

        private void Move(List<Particle> particles, float t)
        {
            foreach (var particle in particles)
            {
                particle.Pos = particle.Pos + Vector2.Multiply(particle.Vel, t);
            }
        }

        private static (Particle i, Particle j, double Value)? CheckCollisionAll(IEnumerable<Particle> particles)
        {
            var collisions = new List<(Particle i, Particle j, double Value)>();
            foreach (var i in particles)
            {
                foreach (var j in particles)
                {
                    var checkCollision = CheckCollision(i.Pos, i.Vel, j.Pos, j.Vel);
                    if (checkCollision.HasValue)
                    {
                        collisions.Add((i, j, checkCollision.Value));
                    }
                }
            }

            if (collisions.Any(x => x.Value > 0))
            {
                var min = collisions.Where(x => x.Value > 0).Min(x => x.Value);
                var c = collisions.Find(x => x.Value == min);
                return c;
            }
            else
            {
                return null;
            }
        }

        private static double? CheckCollision(Vector2 ri, Vector2 vi, Vector2 rj, Vector2 vj)
        {
            double si = 5; // sigma, radius

            double sj = 5;

            Vector2 dr = rj - ri;
            Vector2 dv = vj - vi;

            var s_pow_2 = Math.Pow(si + sj, 2);

            var dvdr = dv.X * dr.X + dv.Y * dr.Y;
            var dvdv = Math.Pow(dv.X, 2) + Math.Pow(dv.Y, 2);
            var drdr = Math.Pow(dr.X, 2) + Math.Pow(dr.Y, 2);
            var d = Math.Pow(dvdr, 2) - dvdv * (drdr - s_pow_2);
            if (dvdr >= 0)
            {
                //Console.WriteLine("no collision");
                return null;
            }
            else if (d < 0)
            {
                //Console.WriteLine("no collision");
                return null;
            }
            else
            {
                var dt = -(dvdr + Math.Sqrt(d)) / dvdv;
                //Console.WriteLine($"collision at {dt}");
                return dt;
            }
        }

        private static double NextDouble(Random r)
        {
            // TODO support min/max here
            return (r.NextDouble() - .5) * 5;
        }
    }
}