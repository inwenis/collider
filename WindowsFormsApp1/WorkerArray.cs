using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
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

            Collision c;
            float step; // seconds
            float t;   // time
            float tc;  // time of next collision
            float ttc; // time to next collision
            float ttf; // time to next frame

            step = (float)0.1;
            t = 0;
            SetAllPwCollisions(particlesArr, wallCollisions, t);
            c = ComputeClosestCollision(particlesArr, wallCollisions, t);
            tc = t + c?.Dt ?? float.MaxValue;
            int frameNumber = 0;
            // tf - time of next frame
            foreach (var tf in Enumerable.Range(0, nFrames).Select(x => x * step))
            {
                var line = Worker.Line(frameNumber, c);
                File.AppendAllText("array.txt", line);
                ttf = tf - t;
                while (tc < tf)
                {
                    ttc = tc - t;
                    Move(particlesArr, ttc);
                    t = tc;
                    ApplyCollision(c, wallCollisions, t);
                    c = ComputeClosestCollision(particlesArr, wallCollisions, t);
                    tc = tc + c?.Dt ?? float.MaxValue;
                    ttf = tf - t;
                }
                Move(particlesArr, ttf);
                AddFrame(frames, particlesArr);
                t = tf;
                frameNumber++;
            }

            Debug.WriteLine($"Computed: {frames.Count} frames");

            return frames;
        }

        private static void AddFrame(List<Frame> frames, Particle[] particlesArr)
        {
            frames.Add(new Frame {Positions = particlesArr.Select(x => x.Pos).ToList()});
        }

        private Collision ComputeClosestCollision(Particle[] particles, float?[][] wallCollisions, float t)
        {
            var ppc = ComputeClosestPpCollision(particles);
            var pwc = FindClosestPwCollision(particles, wallCollisions, t);
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
            for (var k = 0; k < particles.Length; k++)
            {
                var i = particles[k];
                for (var l = 0; l < particles.Length; l++)
                {
                    var j = particles[l];
                    var dt = ComputeCollisionTime(i.Pos, i.Vel, j.Pos, j.Vel);
                    if (dt.HasValue)
                    {
                        collisions.Add(new Collision(i, k, j, l, dt.Value));
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
        private static Collision FindClosestPwCollision(Particle[] particles, float?[][] wallCollisions, float t)
        {
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
                return new Collision(particles[particleIndex], particleIndex, minDt - t, true, "x");
            }

            return new Collision(particles[particleIndex], particleIndex, minDt - t, true, "y");
        }

        private void SetAllPwCollisions(Particle[] particles, float?[][] wallCollisions, float t)
        {
            for (var j = 0; j < particles.Length; j++)
            {
                var i = particles[j];
                var c = wallCollisions[j];
                SetXWallCollisions(i.Pos, i.Vel, c, t);
                SetYWallCollisions(i.Pos, i.Vel, c, t);
            }
        }

        private void SetYWallCollisions(Vector2 r, Vector2 v, float?[] c, float t)
        {
            float? dt;
            float si = 5; // sigma, radius

            if (v.Y > 0)
            {
                dt = (400 - si - r.Y) / v.Y;
                c[2] = null;
                c[3] = t + dt;
            }
            else if (v.Y < 0)
            {
                dt = (si - r.Y) / v.Y;
                c[2] = t + dt;
                c[3] = null;
            }
            else //v.Y == 0
            {
                c[2] = null;
                c[3] = null;
            }
        }

        private void SetXWallCollisions(Vector2 r, Vector2 v, float?[] c, float t)
        {
            float? dt;
            float si = 5; // sigma, radius

            if (v.X > 0)
            {
                dt = (700 - si - r.X) / v.X;
                c[0] = null;
                c[1] = t + dt;
            }
            else if (v.X < 0)
            {
                dt = (si - r.X) / v.X;
                c[0] = t + dt;
                c[1] = null;
            }
            else //v.X == 0
            {
                c[0] = null;
                c[1] = null;
            }
        }

        private void ApplyCollision(Collision c, float?[][] wallCollisions, float t)
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

                SetXWallCollisions(c.ParticleI.Pos, c.ParticleI.Vel, wallCollisions[c.IndexI], t);
                SetYWallCollisions(c.ParticleI.Pos, c.ParticleI.Vel, wallCollisions[c.IndexI], t);

                SetXWallCollisions(c.ParticleJ.Pos, c.ParticleJ.Vel, wallCollisions[c.IndexJ], t);
                SetYWallCollisions(c.ParticleJ.Pos, c.ParticleJ.Vel, wallCollisions[c.IndexJ], t);

            }
            else if (c.IsWallCollision && c.Wall == "x")
            {
                c.ParticleI.Vel = c.ParticleI.Vel * new Vector2(-1, 1);
                SetXWallCollisions(c.ParticleI.Pos, c.ParticleI.Vel, wallCollisions[c.IndexI], t);
                SetYWallCollisions(c.ParticleI.Pos, c.ParticleI.Vel, wallCollisions[c.IndexI], t);
            }
            else if (c.IsWallCollision && c.Wall == "y")
            {
                c.ParticleI.Vel = c.ParticleI.Vel * new Vector2(1, -1);
                SetXWallCollisions(c.ParticleI.Pos, c.ParticleI.Vel, wallCollisions[c.IndexI], t);
                SetYWallCollisions(c.ParticleI.Pos, c.ParticleI.Vel, wallCollisions[c.IndexI], t);
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