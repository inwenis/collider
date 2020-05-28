using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Numerics;

namespace WindowsFormsApp1
{
    public class Worker
    {
        public List<Frame> Simulate(int nFrames, IEnumerable<Particle> particles, Size size)
        {
            var particlesArr = particles.ToArray();
            var frames = new List<Frame>();

            Collision c;
            float step; // seconds
            float t;   // time
            float tc;  // time of next collision
            float ttc; // time to next collision
            float ttf; // time to next frame

            step = (float)0.1;
            t = 0;
            c = ComputeClosestCollision(particlesArr, size);
            tc = t + c?.Dt ?? float.MaxValue;
            // tf - time of next frame
            foreach (var tf in Enumerable.Range(0, nFrames).Select(x => x * step))
            {
                ttf = tf - t;
                while (tc < tf)
                {
                    ttc = tc - t;
                    Move(particlesArr, ttc);
                    ApplyCollision(c);
                    c = ComputeClosestCollision(particlesArr, size);
                    t = tc;
                    tc = tc + c?.Dt ?? float.MaxValue;
                    ttf = tf - t;
                }
                Move(particlesArr, ttf);
                AddFrame(frames, particlesArr);
                t = tf;
            }

            Debug.WriteLine($"Computed: {frames.Count} frames");

            return frames;
        }

        private static void AddFrame(List<Frame> frames, Particle[] particlesArr)
        {
            frames.Add(new Frame {Positions = particlesArr.Select(x => x.Pos).ToList()});
        }

        private Collision ComputeClosestCollision(Particle[] particles, Size size)
        {
            var ppc = ComputeClosestPpCollision(particles);
            var pwc = ComputeClosestPwCollision(particles, size);
            if (pwc != null && ppc != null)
            {
                return pwc.Dt < ppc.Dt
                    ? pwc
                    : ppc;
            }
            if (pwc != null)
            {
                return pwc;
            }
            if (ppc != null)
            {
                return ppc;
            }

            return null;
        }

        // PP collision - particle - particle collision
        private static Collision ComputeClosestPpCollision(Particle[] particles)
        {
            var collisions = new List<Collision>();
            for (var i = 0; i < particles.Length; i++)
            {
                for (var j = 0; j < particles.Length; j++)
                {
                    var dt = ComputeCollisionTime(particles[i].Pos, particles[i].Vel, particles[j].Pos, particles[j].Vel);
                    if (dt.HasValue)
                    {
                        collisions.Add(new Collision(particles[i], i, particles[j], j, dt.Value));
                    }
                }
            }

            return collisions.OrderBy(x => x.Dt).FirstOrDefault();
        }

        private static float? ComputeCollisionTime(Vector2 ri, Vector2 vi, Vector2 rj, Vector2 vj)
        {
            float si = 5; // sigma, radius

            float sj = 5;

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
                double dt = -(dvdr + Math.Sqrt(d)) / dvdv;
                //Console.WriteLine($"collision at {dt}");
                return (float?) dt;
            }
        }

        // PW collision - particle - wall collision
        private Collision ComputeClosestPwCollision(Particle[] particles, Size size)
        {
            var xs = new List<Collision>();
            var ys = new List<Collision>();

            for (var i = 0; i < particles.Length; i++)
            {
                var x = CheckWallCollisionX(particles[i].Pos, particles[i].Vel, size);
                var y = CheckWallCollisionY(particles[i].Pos, particles[i].Vel, size);
                if (x.HasValue)
                {
                    xs.Add(new Collision(particles[i], i, x.Value, true, "x"));
                }

                if (y.HasValue)
                {
                    ys.Add(new Collision(particles[i], i, y.Value, true, "y"));
                }
            }

            var closestWallCollision = new Collision[] { }
                .Concat(xs.Where(x => x.Dt > 0))
                .Concat(ys.Where(x => x.Dt > 0))
                .OrderBy(x => x.Dt)
                .FirstOrDefault();

            return closestWallCollision;
        }

        private float? CheckWallCollisionY(Vector2 r, Vector2 v, Size size)
        {
            float? dt;
            float si = 5; // sigma, radius

            if (v.Y > 0)
            {
                dt = (size.Height - si - r.Y) / v.Y;
            }
            else if (v.Y < 0)
            {
                dt = (si - r.Y) / v.Y;
            }
            else //v.Y == 0
            {
                dt = null;
            }

            return dt;
        }

        private float? CheckWallCollisionX(Vector2 r, Vector2 v, Size size)
        {
            float? dt;
            float si = 5; // sigma, radius

            if (v.X > 0)
            {
                dt = (size.Width - si - r.X) / v.X;
            }
            else if (v.X < 0)
            {
                dt = (si - r.X) / v.X;
            }
            else //v.Y == 0
            {
                dt = null;
            }

            return dt;
        }

        private void ApplyCollision(Collision c)
        {
            if (!c.IsWallCollision)
            {
                var i = c.ParticleI;
                var j = c.ParticleJ;
                var mi = 1;
                var mj = 1;

                double si = 5; // sigma, radius
                double sj = 5;

                var sigma = si + sj;

                Vector2 dr = j.Pos - i.Pos;

                Vector2 dv = j.Vel - i.Vel;

                var dvdr = dv.X * dr.X + dv.Y * dr.Y;

                var J = (2 * mi * mj) * dvdr / (float)(sigma * (mi + mj));

                var Jx = (J * dr.X) / sigma;
                var Jy = (J * dr.Y) / sigma;

                var vxip = (i.Vel.X + Jx) / mi;
                var vyip = (i.Vel.Y + Jy) / mi;

                var vxjp = (j.Vel.X - Jx) / mj;
                var vyjp = (j.Vel.Y - Jy) / mj;

                i.Vel = new Vector2((float)vxip, (float)vyip);
                j.Vel = new Vector2((float)vxjp, (float)vyjp);
            }
            else if (c.IsWallCollision && c.Wall == "x")
            {
                c.ParticleI.Vel = c.ParticleI.Vel * new Vector2(-1, 1);
            }
            else if (c.IsWallCollision && c.Wall == "y")
            {
                c.ParticleI.Vel = c.ParticleI.Vel * new Vector2(1, -1);
            }
        }

        private void Move(Particle[] particles, float t)
        {
            foreach (var particle in particles)
            {
                particle.Pos += Vector2.Multiply(particle.Vel, t);
            }
        }
    }
}