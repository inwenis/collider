﻿using System;
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

            var closestCol = ClosestCol(particles);

            var nextCollisionTime = closestCol?.Dt + t ?? double.MaxValue;
            var nextFrameTime = t + step;

            while (frames.Count < nFrames)
            {
                while (nextFrameTime < nextCollisionTime && frames.Count < nFrames)
                {
                    Move(particles, (float) step);
                    frames.Add(new Frame {Positions = particles.Select(x => x.Pos).ToList()});
                    t = nextFrameTime;
                    nextFrameTime += step;
                }

                if (frames.Count >= nFrames)
                {
                    break;
                }

                while (nextFrameTime > nextCollisionTime)
                {
                    float beforeCollision = (float)(nextCollisionTime - t);
                    Move(particles, beforeCollision);
                    t += beforeCollision;
                    ApplyCollision(closestCol);
                    closestCol = ClosestCol(particles);
                    nextCollisionTime = closestCol?.Dt + t ?? double.MaxValue;
                }

                Move(particles, (float) (nextFrameTime - t));
                frames.Add(new Frame { Positions = particles.Select(x => x.Pos).ToList() });
                t = nextFrameTime;
                nextFrameTime += step;
            }

            Debug.WriteLine($"Computed: {frames.Count} frames");

            return frames;
        }

        private Collision ClosestCol(List<Particle> particles)
        {
            var closestPartCollision = CheckPartCollisions(particles);
            var closestWallCollision = CheckWallCollisions(particles);
            Collision closestCol;
            if (closestPartCollision == null)
            {
                closestCol = closestWallCollision;
            }
            else if (closestWallCollision == null)
            {
                closestCol = closestPartCollision;
            }
            else if (closestWallCollision == null && closestPartCollision == null)
            {
                closestCol = null;
            }
            else
            {
                closestCol = closestWallCollision.Dt < closestPartCollision.Dt
                    ? closestWallCollision
                    : closestPartCollision;
            }

            return closestCol;
        }

        private Collision CheckWallCollisions(List<Particle> particles)
        {
            var xs = new List<(Particle i, Particle j, double Value)>();
            var ys = new List<(Particle i, Particle j, double Value)>();

            foreach (var i in particles)
            {
                var x = CheckWallCollisionX(i.Pos, i.Vel);
                var y = CheckWallCollisionY(i.Pos, i.Vel);
                if (x.HasValue)
                {
                    xs.Add((i, null, x.Value));
                }
                if (y.HasValue)
                {
                    ys.Add((i, null, y.Value));
                }
            }

            var xss = xs
                .Where(x => x.Value > 0)
                .Select(x => new Collision(x.i, x.Value, isWallCollision: true, wall: "x"));
            var yss = ys
                .Where(x => x.Value > 0)
                .Select(x => new Collision(x.i, x.Value, isWallCollision: true, wall: "y"));
            var closestWallCollision = xss.Concat(yss).OrderBy(x => x.Dt).FirstOrDefault();

            return closestWallCollision;
        }

        private double? CheckWallCollisionY(Vector2 r, Vector2 v)
        {
            double? dt = 0;
            double si = 5; // sigma, radius

            if (v.Y > 0)
            {
                dt = (400 - si - r.Y) / v.Y;
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

        private double? CheckWallCollisionX(Vector2 r, Vector2 v)
        {
            double? dt = 0;
            double si = 5; // sigma, radius

            if (v.X > 0)
            {
                dt = (700 - si - r.X) / v.X;
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

        private void Move(List<Particle> particles, float t)
        {
            foreach (var particle in particles)
            {
                particle.Pos = particle.Pos + Vector2.Multiply(particle.Vel, t);
            }
        }

        private static Collision CheckPartCollisions(IEnumerable<Particle> particles)
        {
            var collisions = new List<Collision>();
            foreach (var i in particles)
            {
                foreach (var j in particles)
                {
                    var c = CheckCollision(i.Pos, i.Vel, j.Pos, j.Vel);
                    if (c.HasValue)
                    {
                        collisions.Add(new Collision(i, j, c.Value));
                    }
                }
            }

            return collisions.OrderBy(x => x.Dt).FirstOrDefault();
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

    internal class Collision
    {
        public Particle ParticleI { get; }
        public Particle ParticleJ { get; }
        public double Dt { get; }
        public bool IsWallCollision { get; }
        public string Wall { get; }

        public Collision(Particle particleI, double dt, bool isWallCollision, string wall)
        {
            ParticleI = particleI;
            Dt = dt;
            IsWallCollision = isWallCollision;
            Wall = wall;
        }

        public Collision(Particle particleI, Particle particleJ, double dt)
        {
            ParticleI = particleI;
            ParticleJ = particleJ;
            Dt = dt;
            IsWallCollision = false;
        }
    }
}