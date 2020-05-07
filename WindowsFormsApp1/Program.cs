using System;
using System.Collections.Generic;
using System.Drawing;
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

            var w = new Worker();
            var nFrames = 100;

            var particles = ParticlesGenerator.RandomParticles(20);
            var fastParticles = ParticlesGenerator.RandomFastParticles(10);
            particles.AddRange(fastParticles);

            var frames = w.Simulate(nFrames, particles);

            _mainForm = new Form1();
            _mainForm.TrackBar1.Minimum = 0;
            _mainForm.TrackBar1.Maximum = nFrames - 1;
            _mainForm.TrackBar1.Scroll += TrackBar1_Scroll;

            _frames = frames;
            Timer t = new Timer(PrintFrames, (_mainForm, _frames), nFrames, int.MaxValue);

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
    }
}
