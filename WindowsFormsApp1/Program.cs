using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net;
using System.Windows.Forms;
using System.Numerics;
using WindowsFormsApp1.Csv;
using CommandLine;
using Timer = System.Threading.Timer;

namespace WindowsFormsApp1
{
    static class Program
    {
        private static Form1 _mainForm;
        private static List<Particle[]> _frames;
        private static Size _size;

        static void Main(string[] args)
        {
            var parserResult = Parser.Default.ParseArguments<Options>(args);
            var options = ((Parsed<Options>) parserResult).Value;

            Console.WriteLine($"To rerun with same arguments use: {options.ToInputArgumentsString()}");

            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            List<Particle> particles;
            if (options.ParticlesFile != null)
            {
                var lines = File.ReadAllLines(options.ParticlesFile);
                CsvSerializer.ParseCsv(lines, out options, out var outParticles);
                particles = outParticles.ToList();

                var size = options.Dimensions.ToArray();
                _size = new Size(size[0], size[1]);
            }
            else
            {
                var size = options.Dimensions.ToArray();
                _size = new Size(size[0], size[1]);

                particles = new List<Particle> {new Particle {Pos = new Vector2(100, 100), Vel = Vector2.Zero, Sig = 20}};
                ParticlesGenerator.AddRandomParticles(particles, options.NumberOfParticles, options.Radius, _size);
                Tools.DumpToFile(particles, $"{DateTime.Now:yyyy-MM-dd--HH-mm-ss}.xml");
            }

            var w = new WorkerArray();

            _frames = w.Simulate(options.NumberOfFrames, particles, _size);

            _mainForm = new Form1();
            _mainForm.TrackBar1.Minimum = 0;
            _mainForm.TrackBar1.Maximum = options.NumberOfFrames - 1;
            _mainForm.TrackBar1.Scroll += TrackBar1_Scroll;

            Timer t = new Timer(obj => PrintFrames(), null, 500, -1); // wait 500ms before starting timer to let window be created

            Application.Run(_mainForm);
        }

        private static void TrackBar1_Scroll(object sender, EventArgs e)
        {
            var trackBar = (TrackBar) sender;
            var frame = _frames[trackBar.Value];
            _mainForm.PictureBox1.Image = PrintFrame(frame, _size);
            _mainForm.Label1.Text = trackBar.Value.ToString();
        }

        private static void PrintFrames()
        {
            int frameNumber = 0;

            foreach (var frame in _frames)
            {
                _mainForm.PictureBox1.Invoke((MethodInvoker)delegate {
                    // Running on the UI thread
                    _mainForm.PictureBox1.Image = PrintFrame(frame, _size);
                    _mainForm.Label1.Text = frameNumber.ToString();
                    _mainForm.TrackBar1.Value = frameNumber;
                });
                //Thread.Sleep(TimeSpan.FromMilliseconds(1));
                frameNumber++;
            }
        }

        private static Bitmap PrintFrame(Particle[] frame, Size size)
        {
            var bitmap = new Bitmap(size.Width+1, size.Height+1); // add 1 so there is space to print the border
            var g = Graphics.FromImage(bitmap);

            g.DrawLine(Pens.Black, 0,          0,           size.Width, 0);
            g.DrawLine(Pens.Black, size.Width, 0,           size.Width, size.Height);
            g.DrawLine(Pens.Black, size.Width, size.Height, 0,          size.Height);
            g.DrawLine(Pens.Black, 0,          size.Height, 0,          0);

            foreach (var p in frame)
            {
                g.FillEllipse(Brushes.Black, p.Pos.X - p.Sig, p.Pos.Y - p.Sig, 2 * p.Sig, 2 * p.Sig);
            }

            return bitmap;
        }
    }
}
