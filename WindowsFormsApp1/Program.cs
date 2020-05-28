using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using System.Numerics;
using Timer = System.Threading.Timer;

namespace WindowsFormsApp1
{
    static class Program
    {
        private static Form1 _mainForm;
        private static List<Frame> _framesA;
        private static List<Frame> _framesB;
        private static Size _size;

        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            var nFrames = 1000;
            _size = new Size(1000, 800);

            List<Particle> particles;
            if (File.Exists("input.xml"))
            {
                particles = Tools.ReadFromFile("input.xml");
            }
            else
            {
                
                particles = ParticlesGenerator.RandomParticles(250, _size);
                Tools.DumpToFile(particles, $"{DateTime.Now:yyyy-MM-dd--HH-mm-ss}.xml");
            }

            var wA = new Worker();
            var wB = new WorkerArray();

            var particlesA = particles.Select(x => x.Clone());
            var particlesB = particles.Select(x => x.Clone());

            _framesA = wA.Simulate(nFrames, particlesA, _size);
            _framesB = wB.Simulate(nFrames, particlesB, _size);

            _mainForm = new Form1();
            _mainForm.TrackBar1.Minimum = 0;
            _mainForm.TrackBar1.Maximum = nFrames - 1;
            _mainForm.TrackBar1.Scroll += TrackBar1_Scroll;

            Timer t = new Timer(PrintFrames, null, 0, int.MaxValue);

            Application.Run(_mainForm);
        }

        private static void TrackBar1_Scroll(object sender, EventArgs e)
        {
            var trackBar = (TrackBar) sender;
            var frameA = _framesA[trackBar.Value];
            var frameB = _framesB[trackBar.Value];
            _mainForm.PictureBox1.Image = PrintFrame(frameA.Positions, frameB.Positions);
            _mainForm.Label1.Text = trackBar.Value.ToString();
        }

        private static void PrintFrames(object obj)
        {
            int frameNumber = 0;

            foreach (var (frameA, frameB) in _framesA.Zip(_framesB, (a, b) => (a, b)))
            {
                _mainForm.PictureBox1.Invoke((MethodInvoker)delegate {
                    // Running on the UI thread
                    _mainForm.PictureBox1.Image = PrintFrame(frameA.Positions, frameB.Positions);
                    _mainForm.Label1.Text = frameNumber.ToString();
                    _mainForm.TrackBar1.Value = frameNumber;
                });
                //Thread.Sleep(TimeSpan.FromMilliseconds(1));
                frameNumber++;
            }
        }

        private static Bitmap PrintFrame(IEnumerable<Vector2> positionsA, List<Vector2> positionsB)
        {
            var bitmap = new Bitmap(_size.Width, _size.Height);
            var flagGraphics = Graphics.FromImage(bitmap);

            foreach (var p in positionsA)
            {
                flagGraphics.FillEllipse(Brushes.Aqua, p.X - 5, p.Y - 5, 10, 10);
            }

            foreach (var p in positionsB)
            {
                flagGraphics.FillEllipse(Brushes.BlueViolet, p.X - 5, p.Y - 5, 10, 10);
            }

            return bitmap;
        }
    }
}
