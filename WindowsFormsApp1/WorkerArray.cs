using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Numerics;

namespace WindowsFormsApp1
{
    public class WorkerArray
    {
        public List<Particle[]> Simulate(int nFrames, IEnumerable<Particle> particles, Size size)
        {
            var particlesArr = particles.ToArray();
            var frames = new List<Particle[]>();

            var pwCollisions = Array2D.Create<float?>(particlesArr.Length, 4); // 4 - there are walls
            var ppCollisions = Array2D.Create<float?>(particlesArr.Length, particlesArr.Length);

            Collision c;
            float stp; // step seconds
            float t;   // time
            float toc; // time of next collision
            float ttc; // time to next collision
            float ttf; // time to next frame

            stp = (float)0.1;
            t = 0;
            SetAllPwCollisions(particlesArr, pwCollisions, size, t);
            SetAllPpCollisions(particlesArr, ppCollisions, t);
            c = FindClosestCollision(particlesArr, pwCollisions, ppCollisions);
            toc = c?.Dt ?? float.MaxValue;
            // tf - time of next frame
            var timeOfFrames = Enumerable.Range(0, nFrames).Select(x => x * stp).ToArray();
            for (var i = 0; i < timeOfFrames.Length; i++)
            {
                var tof = timeOfFrames[i];
                ttf = tof - t;
                while (toc < tof)
                {
                    ttc = toc - t;
                    Move(particlesArr, ttc);
                    t = toc;
                    ApplyCollision(particlesArr, c, pwCollisions, ppCollisions, size, t);
                    c = FindClosestCollision(particlesArr, pwCollisions, ppCollisions);

                    while (c.Dt <= t)
                    {
                        Console.WriteLine("Computed collision with negative `time to collision`. This is likely a rounding error. Applying collision immediately");
                        Console.WriteLine($"frame={i} t={t} c.Dt={c.Dt} c.Dt-t={c.Dt-t} particles={c.IndexI}/{c.IndexJ}");
                        ApplyCollision(particlesArr, c, pwCollisions, ppCollisions, size, t);
                        c = FindClosestCollision(particlesArr, pwCollisions, ppCollisions);
                    }

                    toc = c?.Dt ?? float.MaxValue;
                    ttf = tof - t;
                }

                Move(particlesArr, ttf);
                AddFrame(frames, particlesArr);
                t = tof;
            }

            Debug.WriteLine($"Computed: {frames.Count} frames");

            return frames;
        }

        private static void SetAllPpCollisions(Particle[] particles, float?[][] ppCollisions, float t)
        {
            for (var i = 0; i < particles.Length; i++)
            {
                for (var j = 0; j < particles.Length; j++)
                {
                    var dt = ComputeCollisionTime(particles[i].Pos, particles[i].Vel, particles[i].Sig, particles[j].Pos, particles[j].Vel, particles[j].Sig);
                    dt = dt.HasValue ? dt + t : null;
                    ppCollisions[i][j] = dt;
                    ppCollisions[j][i] = dt;
                }
            }

            for (int i = 0; i < particles.Length; i++)
            {
                ppCollisions[i][i] = null;
            }
        }

        private static void SetAllPwCollisions(Particle[] particles, float?[][] wallCollisions, Size size, float t)
        {
            for (var j = 0; j < particles.Length; j++)
            {
                var i = particles[j];
                var c = wallCollisions[j];
                SetXWallCollisions(i.Pos, i.Vel, i.Sig, c, size, t);
                SetYWallCollisions(i.Pos, i.Vel, i.Sig, c, size, t);
            }
        }

        private static void SetXWallCollisions(Vector2 r, Vector2 v, int s, float?[] c, Size size, float t)
        {
            float? dt;
            float si = s;

            if (v.X > 0)
            {
                dt = (size.Width - si - r.X) / v.X;
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

        private static void SetYWallCollisions(Vector2 r, Vector2 v, int s, float?[] c, Size size, float t)
        {
            float? dt;
            float si = s;

            if (v.Y > 0)
            {
                dt = (size.Height - si - r.Y) / v.Y;
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

        private static Collision FindClosestCollision(Particle[] particles, float?[][] pwCollisions, float?[][] ppCollisions)
        {
            var ppc = FindClosestPpCollision(particles, ppCollisions);
            var pwc = FindClosestPwCollision(particles, pwCollisions);

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

        private static Collision FindClosestPpCollision(Particle[] particles, float?[][] ppCollisions)
        {
            // PP collision - particle - particle collision
            var minI = 0;
            var minJ = 0;
            var minDt = float.MaxValue;
            var collisionExists = false;
            for (var i = 0; i < particles.Length; i++)
            {
                for (var j = i + 1; j < particles.Length; j++)
                {
                    if (ppCollisions[i][j].HasValue && ppCollisions[i][j] < minDt)
                    {
                        minI = i;
                        minJ = j;
                        minDt = ppCollisions[i][j].Value;
                        collisionExists = true;
                    }
                }
            }

            return collisionExists
                ? new Collision(particles[minI], minI, particles[minJ], minJ, minDt)
                : null;
        }

        private static Collision FindClosestPwCollision(Particle[] particles, float?[][] wallCollisions)
        {
            // PW collision - particle - wall collision
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
                return new Collision(particles[particleIndex], particleIndex, minDt, true, "x");
            }

            return new Collision(particles[particleIndex], particleIndex, minDt, true, "y");
        }

        private static float? ComputeCollisionTime(Vector2 ri, Vector2 vi, int si, Vector2 rj, Vector2 vj, int sj)
        {
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
                return (float?)dt;
            }
        }

        private static void ApplyCollision(Particle[] particles, Collision c, float?[][] pwCollisions,
            float?[][] ppCollisions, Size size, float t)
        {
            if (!c.IsWallCollision)
            {
                var i = c.ParticleI;
                var j = c.ParticleJ;
                var mi = c.ParticleI.Mass;
                var mj = c.ParticleJ.Mass;
                var si = c.ParticleI.Sig;
                var sj = c.ParticleJ.Sig;

                // in the source https://introcs.cs.princeton.edu/java/assignments/collisions.html
                // si + sj is called sigma, but to avoid confusion it's called sij here
                var sij = si + sj;

                Vector2 dr = j.Pos - i.Pos;

                Vector2 dv = j.Vel - i.Vel;

                var dvdr = dv.X * dr.X + dv.Y * dr.Y;

                var J = (2 * mi * mj) * dvdr / (float)(sij * (mi + mj));

                var Jx = (J * dr.X) / sij;
                var Jy = (J * dr.Y) / sij;

                var vxip = (i.Vel.X + Jx) / mi;
                var vyip = (i.Vel.Y + Jy) / mi;

                var vxjp = (j.Vel.X - Jx) / mj;
                var vyjp = (j.Vel.Y - Jy) / mj;

                i.Vel = new Vector2((float)vxip, (float)vyip);
                j.Vel = new Vector2((float)vxjp, (float)vyjp);

                SetXWallCollisions(i.Pos, i.Vel, i.Sig, pwCollisions[c.IndexI], size, t);
                SetYWallCollisions(i.Pos, i.Vel, i.Sig, pwCollisions[c.IndexI], size, t);

                SetXWallCollisions(j.Pos, j.Vel, j.Sig, pwCollisions[c.IndexJ], size, t);
                SetYWallCollisions(j.Pos, j.Vel, j.Sig, pwCollisions[c.IndexJ], size, t);

                for (var k = 0; k < ppCollisions.Length; k++)
                {
                    var dt = ComputeCollisionTime(i.Pos, i.Vel, i.Sig, particles[k].Pos, particles[k].Vel, particles[k].Sig);
                    dt = dt.HasValue ? dt + t : null;
                    ppCollisions[c.IndexI][k] = dt;
                    ppCollisions[k][c.IndexI] = dt;
                }

                for (var k = 0; k < ppCollisions.Length; k++)
                {
                    var dt = ComputeCollisionTime(j.Pos, j.Vel, j.Sig, particles[k].Pos, particles[k].Vel, particles[k].Sig);
                    dt = dt.HasValue ? dt + t : null;
                    ppCollisions[c.IndexJ][k] = dt;
                    ppCollisions[k][c.IndexJ] = dt;
                }

                ppCollisions[c.IndexI][c.IndexI] = null;
                ppCollisions[c.IndexJ][c.IndexJ] = null;
            }
            else if (c.IsWallCollision && c.Wall == "x")
            {
                c.ParticleI.Vel = c.ParticleI.Vel * new Vector2(-1, 1);
                SetXWallCollisions(c.ParticleI.Pos, c.ParticleI.Vel, c.ParticleI.Sig, pwCollisions[c.IndexI], size, t);
                SetYWallCollisions(c.ParticleI.Pos, c.ParticleI.Vel, c.ParticleI.Sig, pwCollisions[c.IndexI], size, t);

                for (var k = 0; k < ppCollisions.Length; k++)
                {
                    var dt = ComputeCollisionTime(c.ParticleI.Pos, c.ParticleI.Vel, c.ParticleI.Sig, particles[k].Pos, particles[k].Vel, particles[k].Sig);
                    dt = dt.HasValue ? dt + t : null;
                    ppCollisions[c.IndexI][k] = dt;
                    ppCollisions[k][c.IndexI] = dt;
                }

                ppCollisions[c.IndexI][c.IndexI] = null;
            }
            else if (c.IsWallCollision && c.Wall == "y")
            {
                c.ParticleI.Vel = c.ParticleI.Vel * new Vector2(1, -1);
                SetXWallCollisions(c.ParticleI.Pos, c.ParticleI.Vel, c.ParticleI.Sig, pwCollisions[c.IndexI], size, t);
                SetYWallCollisions(c.ParticleI.Pos, c.ParticleI.Vel, c.ParticleI.Sig, pwCollisions[c.IndexI], size, t);

                for (var k = 0; k < ppCollisions.Length; k++)
                {
                    var dt = ComputeCollisionTime(c.ParticleI.Pos, c.ParticleI.Vel, c.ParticleI.Sig, particles[k].Pos, particles[k].Vel, particles[k].Sig);
                    dt = dt.HasValue ? dt + t : null;
                    ppCollisions[c.IndexI][k] = dt;
                    ppCollisions[k][c.IndexI] = dt;
                }

                ppCollisions[c.IndexI][c.IndexI] = null;
            }
        }

        private static void Move(Particle[] particles, float t)
        {
            foreach (var particle in particles)
            {
                particle.Pos += Vector2.Multiply(particle.Vel, t);
            }
        }

        private static void AddFrame(List<Particle[]> frames, Particle[] particlesArr)
        {
            frames.Add(particlesArr.Select(x => x.Clone()).ToArray());
        }
    }
}