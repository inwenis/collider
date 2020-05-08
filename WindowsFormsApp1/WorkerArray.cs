using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Numerics;

namespace WindowsFormsApp1
{
    public class WorkerArray
    {
        public List<Frame> Simulate(int nFrames, IEnumerable<Particle> particles)
        {
            var particlesArr = particles.ToArray();
            var frames = new List<Frame>();

            var wallCollisions = new float?[particlesArr.Length][];
            for (int i = 0; i < wallCollisions.Length; i++)
            {
                wallCollisions[i] = new float?[4];
            }

            CollisionArray c;
            float step; // seconds
            float t;   // time
            float tc;  // time of next collision
            float ttc; // time to next collision
            float ttf; // time to next frame

            step = (float)0.1;
            t = 0;
            c = ComputeClosestCollision(particlesArr, wallCollisions);
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
                    c = ComputeClosestCollision(particlesArr, wallCollisions);
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

        private CollisionArray ComputeClosestCollision(Particle[] particles, float?[][] wallCollisions)
        {
            var ppc = ComputeClosestPpCollision(particles);
            var pwc = ComputeClosestPwCollision(particles, wallCollisions);
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
        private static CollisionArray ComputeClosestPpCollision(Particle[] particles)
        {
            var collisions = new List<CollisionArray>();
            foreach (var i in particles)
            {
                foreach (var j in particles)
                {
                    var dt = ComputeCollisionTime(i.Pos, i.Vel, j.Pos, j.Vel);
                    if (dt.HasValue)
                    {
                        collisions.Add(new CollisionArray(i, j, dt.Value));
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
        private CollisionArray ComputeClosestPwCollision(Particle[] particles, float?[][] wallCollisions)
        {
            var xs = new List<(Particle i, Particle j, float Value)>();
            var ys = new List<(Particle i, Particle j, float Value)>();

            for (var j = 0; j < particles.Length; j++)
            {
                var i = particles[j];
                var c = wallCollisions[j];
                SetXWallCollisions(i.Pos, i.Vel, c);
                SetYWallCollisions(i.Pos, i.Vel, c);
            }

            int particleIndex = 0;
            int wallIndex = 0;
            float minDt = float.MaxValue;

            for (var j = 0; j < particles.Length; j++)
            {
                for (int x = 0; x < 4; x++)
                {
                    if (wallCollisions[j][x] != null && wallCollisions[j][x] < minDt)
                    {
                        particleIndex = j;
                        wallIndex = x;
                        minDt = wallCollisions[j][x].Value;
                    }
                }
            }

            if (wallIndex == 0 || wallIndex == 1)
            {
                return new CollisionArray(particles[particleIndex], minDt, true, "x");
            }
            return new CollisionArray(particles[particleIndex], minDt, true, "y");
        }

        private void SetYWallCollisions(Vector2 r, Vector2 v, float?[] c)
        {
            float? dt;
            float si = 5; // sigma, radius

            if (v.Y > 0)
            {
                dt = (400 - si - r.Y) / v.Y;
                c[2] = null;
                c[3] = (float)dt;
            }
            else if (v.Y < 0)
            {
                dt = (si - r.Y) / v.Y;
                c[2] = (float)dt;
                c[3] = null;
            }
            else //v.Y == 0
            {
                c[2] = null;
                c[3] = null;
            }
        }

        private void SetXWallCollisions(Vector2 r, Vector2 v, float?[] c)
        {
            float? dt;
            float si = 5; // sigma, radius

            if (v.X > 0)
            {
                dt = (700 - si - r.X) / v.X;
                c[0] = null;
                c[1] = (float)dt;
            }
            else if (v.X < 0)
            {
                dt = (si - r.X) / v.X;
                c[0] = (float)dt;
                c[1] = null;
            }
            else //v.X == 0
            {
                c[0] = null;
                c[1] = null;
            }
        }

        private void ApplyCollision(CollisionArray c)
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

    internal class CollisionArray
    {
        public Particle ParticleI { get; }
        public Particle ParticleJ { get; }
        public float Dt { get; }
        public bool IsWallCollision { get; }
        public string Wall { get; }

        public CollisionArray(Particle particleI, float dt, bool isWallCollision, string wall)
        {
            ParticleI = particleI;
            Dt = dt;
            IsWallCollision = isWallCollision;
            Wall = wall;
        }

        public CollisionArray(Particle particleI, Particle particleJ, float dt)
        {
            ParticleI = particleI;
            ParticleJ = particleJ;
            Dt = dt;
            IsWallCollision = false;
        }
    }
}