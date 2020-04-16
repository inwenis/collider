using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Numerics;
using Timer = System.Threading.Timer;

namespace WindowsFormsApp1
{
    static class Program
    {
        private static List<Particle> _particles;
        private static (Particle i, Particle j, double Value)? closestCollision;
        private static Form1 _mainForm;
        private static List<Frame> _frames;

        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            var w = new Worker();
            var nFrames = 1000;
            var frames = w.Simulate(nFrames);

            //_particles = RandomParticles(10);
            //closestCollision = CheckCollisionAll();
            _mainForm = new Form1();
            _mainForm.TrackBar1.Minimum = 0;
            _mainForm.TrackBar1.Maximum = nFrames;
            _mainForm.TrackBar1.Scroll += TrackBar1_Scroll;

            // var thread = new Thread(DoJob);
            // thread.Start(mainForm);

            _frames = frames;
            Timer t = new Timer(PrintFrames, (_mainForm, _frames), nFrames, int.MaxValue);
            //var thread = new Thread(PrintFrames);
            //thread.Start((mainForm, frames));

            Application.Run(_mainForm);
        }

        private static void TrackBar1_Scroll(object sender, EventArgs e)
        {
            var trackBar = (TrackBar) sender;
            var frame = _frames[trackBar.Value];
            _mainForm.PictureBox1.Image = Print(frame.Positions);
        }

        private static void PrintFrames(object obj)
        {
            var (form1, frames) = ((Form1, List<Frame>)) obj;

            int frameNumber = 0;

            foreach (var frame in frames)
            {
                form1.PictureBox1.Invoke((MethodInvoker)delegate {
                    // Running on the UI thread
                    form1.PictureBox1.Image = Print(frame.Positions);
                    form1.Label1.Text = frameNumber.ToString();
                    form1.TrackBar1.Value = frameNumber;
                });
                Thread.Sleep(TimeSpan.FromSeconds(0.01));
                frameNumber++;
            }
        }

        private static void DoJob(object obj)
        {
            var f = (Form1) obj;

            while (!f.IsDisposed)
            {
                Thread.Sleep(TimeSpan.FromMilliseconds(100));
                State.Time += 1;

                if (f.IsDisposed)
                {
                    return;
                }

                if (State.Stop == false) UpdateParticles();


                if (closestCollision.HasValue)
                {
                    if (State.Time >= closestCollision.Value.Value)
                    {
                        State.Stop = true;
                    }
                }

                var bitmap = Print(_particles.Select(x => x.Pos));

                string newText = "abc";
                f.Label1.Invoke((MethodInvoker)delegate {
                    // Running on the UI thread
                    f.Label1.Text += "a";
                    f.PictureBox1.Image = bitmap;
                });
            }
        }

        private static (Particle i, Particle j, double Value)? CheckCollisionAll()
        {
            var collisions = new List<(Particle i, Particle j, double Value)>();
            foreach (var i in _particles)
            {
                foreach (var j in _particles)
                {
                    var checkCollision = CheckCollision(i.Pos, i.Vel, j.Pos, j.Vel);
                    if (checkCollision.HasValue)
                    {
                        collisions.Add((i,j,checkCollision.Value));
                    }
                }
            }

            if (collisions.Any())
            {
                var min = collisions.Min(x => x.Value);
                var c = collisions.Find(x => x.Value == min);
                return c;
            }
            else
            {
                return null;
            }
        }

        private static void UpdateParticles()
        {
            foreach (var particle in _particles)
            {
                particle.Pos = particle.Pos + particle.Vel;
            }
        }

        private static Bitmap Print(IEnumerable<Vector2> positions)
        {
            Bitmap bitmap = new Bitmap(700, 400);
            Graphics flagGraphics = Graphics.FromImage(bitmap);

            foreach (var p in positions)
            {
                flagGraphics.FillEllipse(Brushes.Aqua, p.X, p.Y, 10, 10);
            }

            return bitmap;
        }

        private static List<Particle> RandomParticles(int n)
        {
            var r = new Random(DateTimeOffset.UtcNow.Millisecond);
            var list = new List<Particle>();
            for (int i = 0; i < n; i++)
            {
                var particle = new Particle();
                particle.Pos = new Vector2(r.Next(0, 200), r.Next(0, 200));
                particle.Vel = new Vector2((float) NextDouble(r), (float) NextDouble(r));
                list.Add(particle);
            }

            return list;
        }

        private static double NextDouble(Random r)
        {
            return (r.NextDouble() - .5) * 5;
        }

        private static double? CheckCollision(Vector2 ri, Vector2 vi, Vector2 rj, Vector2 vj)
        {
            double t = 1; // current time

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

    }

    public class Particle
    {
        public Vector2 Pos { get; set; }
        public Vector2 Vel { get; set; }
    }
}
