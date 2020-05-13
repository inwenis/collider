using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Threading;
using System.Windows.Forms;
using System.Numerics;
using Timer = System.Threading.Timer;

namespace WindowsFormsApp1
{
    static class Program
    {
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

            var nFrames = 2000;

            List<Particle> particles;
            if (File.Exists("input.xml"))
            {
                particles = Tools.ReadFromFile("input.xml");
            }
            else
            {
                particles = ParticlesGenerator.RandomParticles(20);
                var fastParticles = ParticlesGenerator.RandomFastParticles(10);
                particles.AddRange(fastParticles);
                Tools.DumpToFile(particles, $"{DateTime.Now:yyyy-MM-dd--HH-mm-ss}.xml");
            }

            var particlesClone = particles.Select(x => x.Clone()).ToList();

            var w = new Worker();
            var frames = w.Simulate(nFrames, particles);

            var wa = new WorkerArray();
            var framesA = wa.Simulate(nFrames, particlesClone);

            Tools.Compare(frames, framesA);

            _mainForm = new Form1();
            _mainForm.TrackBar1.Minimum = 0;
            _mainForm.TrackBar1.Maximum = nFrames - 1;
            _mainForm.TrackBar1.Scroll += TrackBar1_Scroll;

            _frames = frames;
            Timer t = new Timer(PrintFrames, null, nFrames, int.MaxValue);

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
            int frameNumber = 0;

            foreach (var frame in _frames)
            {
                _mainForm.PictureBox1.Invoke((MethodInvoker)delegate {
                    // Running on the UI thread
                    _mainForm.PictureBox1.Image = Print(frame.Positions);
                    _mainForm.Label1.Text = frameNumber.ToString();
                    _mainForm.TrackBar1.Value = frameNumber;
                });
                Thread.Sleep(TimeSpan.FromSeconds(0.01));
                frameNumber++;
            }
        }

        private static Bitmap Print(IEnumerable<Vector2> positions)
        {
            Bitmap bitmap = new Bitmap(750, 450);
            Graphics flagGraphics = Graphics.FromImage(bitmap);

            foreach (var p in positions)
            {
                flagGraphics.FillEllipse(Brushes.Aqua, p.X - 5, p.Y - 5, 10, 10);
            }

            return bitmap;
        }
    }
}
