using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Numerics;

namespace WindowsFormsApp1
{
    public class WorkerArray2
    {
        public List<Frame> Simulate(int nFrames, IEnumerable<Particle> particles)
        {
            var particlesArr = particles.ToArray();
            var frames = new List<Frame>();

            var pwCollisions = Array2D.Create<float?>(particlesArr.Length, 4);
            var ppCollisions = Array2D.Create<float?>(particlesArr.Length, particlesArr.Length);

            Collision c;
            float step; // seconds
            float t;   // time
            float tc;  // time of next collision
            float ttc; // time to next collision
            float ttf; // time to next frame

            step = (float)0.1;
            t = 0;
            SetAllPwCollisions(particlesArr, pwCollisions, t);
            SetAllPpCollisions(particlesArr, ppCollisions);
            c = ComputeClosestCollision(particlesArr, pwCollisions, ppCollisions, t);
            tc = t + c?.Dt ?? float.MaxValue;
            // tf - time of next frame
            foreach (var tf in Enumerable.Range(0, nFrames).Select(x => x * step))
            {
                ttf = tf - t;
                while (tc < tf)
                {
                    ttc = tc - t;
                    Move(particlesArr, ttc);
                    t = tc;
                    ApplyCollision(particlesArr, c, pwCollisions, ppCollisions, t);
                    c = ComputeClosestCollision(particlesArr, pwCollisions, ppCollisions, t);
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

        private Collision ComputeClosestCollision(Particle[] particles, float?[][] pwCollisions, float?[][] ppCollisionsFromOutside,
            float t)
        {
            var ppCollisions = Array2D.Create<float?>(particles.Length, particles.Length);
            SetAllPpCollisions(particles, ppCollisions);
            var ppc = FindClosestPpCollision(particles, ppCollisions); // TODO here
            // one array keeps relative collision, one keeps absolute
            var ppcfo = FindClosestPpCollision(particles, ppCollisionsFromOutside);

            for (int i = 0; i < ppCollisionsFromOutside.Length; i++)
            {
                for (int j = 0; j < ppCollisionsFromOutside.Length; j++)
                {
                    if (ppCollisionsFromOutside[i][j].HasValue && ppCollisionsFromOutside[i][j] < 0)
                    {
                        Console.WriteLine("alert");
                    }
                }
            }

            if (ppc.IndexI != ppcfo.IndexI || ppc.IndexJ != ppcfo.IndexJ)
            {
                Console.WriteLine("diff");
            }

            var pwc = FindClosestPwCollision(particles, pwCollisions, t);
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

        private static Collision FindClosestPpCollision(Particle[] particles, float?[][] ppCollisions)
        {
            var minI = 0;
            var minJ = 0;
            var minDt = float.MaxValue;
            for (var i = 0; i < particles.Length; i++)
            {
                for (var j = i + 1; j < particles.Length; j++)
                {
                    if (ppCollisions[i][j].HasValue && ppCollisions[i][j] < minDt)
                    {
                        minI = i;
                        minJ = j;
                        minDt = ppCollisions[i][j].Value;
                    }
                }
            }

            return new Collision(particles[minI], minI, particles[minJ], minJ, minDt);
        }

        private static void SetAllPpCollisions(Particle[] particles, float?[][] ppCollisions)
        {
            for (var i = 0; i < particles.Length; i++)
            {
                for (var j = 0; j < particles.Length; j++)
                {
                    var dt = ComputeCollisionTime(particles[i].Pos, particles[i].Vel, particles[j].Pos, particles[j].Vel);
                    ppCollisions[i][j] = dt;
                    ppCollisions[j][i] = dt;
                }
            }
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
                if (dt < 0)
                {
                    Console.WriteLine("alert 100");
                }
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

        private void ApplyCollision(Particle[] particles, Collision c, float?[][] pwCollisions, float?[][] ppCollisions, float t)
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

                SetXWallCollisions(i.Pos, i.Vel, pwCollisions[c.IndexI], t);
                SetYWallCollisions(i.Pos, i.Vel, pwCollisions[c.IndexI], t);

                SetXWallCollisions(j.Pos, j.Vel, pwCollisions[c.IndexJ], t);
                SetYWallCollisions(j.Pos, j.Vel, pwCollisions[c.IndexJ], t);

                for (var k = 0; k < ppCollisions.Length; k++)
                {
                    var dt = ComputeCollisionTime(i.Pos, i.Vel, particles[k].Pos, particles[k].Vel);
                    dt = dt.HasValue ? dt + t : t;
                    ppCollisions[c.IndexI][k] = dt;
                    ppCollisions[k][c.IndexI] = dt;
                }

                for (var k = 0; k < ppCollisions.Length; k++)
                {
                    var dt = ComputeCollisionTime(j.Pos, j.Vel, particles[k].Pos, particles[k].Vel);
                    dt = dt.HasValue ? dt + t : t;
                    ppCollisions[c.IndexJ][k] = dt;
                    ppCollisions[k][c.IndexJ] = dt;
                }
            }
            else if (c.IsWallCollision && c.Wall == "x")
            {
                c.ParticleI.Vel = c.ParticleI.Vel * new Vector2(-1, 1);
                SetXWallCollisions(c.ParticleI.Pos, c.ParticleI.Vel, pwCollisions[c.IndexI], t);
                SetYWallCollisions(c.ParticleI.Pos, c.ParticleI.Vel, pwCollisions[c.IndexI], t);

                for (var k = 0; k < ppCollisions.Length; k++)
                {
                    var dt = ComputeCollisionTime(c.ParticleI.Pos, c.ParticleI.Vel, particles[k].Pos, particles[k].Vel);
                    dt = dt.HasValue ? dt + t : t;
                    ppCollisions[c.IndexI][k] = dt;
                    ppCollisions[k][c.IndexI] = dt;
                }
            }
            else if (c.IsWallCollision && c.Wall == "y")
            {
                c.ParticleI.Vel = c.ParticleI.Vel * new Vector2(1, -1);
                SetXWallCollisions(c.ParticleI.Pos, c.ParticleI.Vel, pwCollisions[c.IndexI], t);
                SetYWallCollisions(c.ParticleI.Pos, c.ParticleI.Vel, pwCollisions[c.IndexI], t);

                for (var k = 0; k < ppCollisions.Length; k++)
                {
                    var dt = ComputeCollisionTime(c.ParticleI.Pos, c.ParticleI.Vel, particles[k].Pos, particles[k].Vel);
                    dt = dt.HasValue ? dt + t : t;
                    ppCollisions[c.IndexI][k] = dt;
                    ppCollisions[k][c.IndexI] = dt;
                }
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