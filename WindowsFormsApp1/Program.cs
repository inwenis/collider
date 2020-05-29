﻿using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using System.Numerics;
using CommandLine;
using Timer = System.Threading.Timer;

namespace WindowsFormsApp1
{
    static class Program
    {
        private static Form1 _mainForm;
        private static List<Frame> _framesA;
        private static Size _size;

        static void Main(string[] args)
        {
            var parserResult = Parser.Default.ParseArguments<Options>(args);
            var options = ((Parsed<Options>) parserResult).Value;

            Console.WriteLine($"To rerun with same arguments use: {options.ToInputArgumentsString()}");

            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            var nFrames = options.NumberOfFrames;
            var array = options.Dimensions.ToArray();
            _size = new Size(array[0], array[1]);

            List<Particle> particles;
            if (options.ParticlesFile != null)
            {
                particles = Tools.ReadFromFile(options.ParticlesFile);
            }
            else
            {
                particles = ParticlesGenerator.RandomParticles(options.NumberOfParticles, _size);
                Tools.DumpToFile(particles, $"{DateTime.Now:yyyy-MM-dd--HH-mm-ss}.xml");
            }

            var w = new WorkerArray();

            var particlesB = particles.Select(x => x.Clone());

            _framesA = w.Simulate(nFrames, particlesB, _size);

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
            _mainForm.PictureBox1.Image = PrintFrame(frameA.Positions, _size);
            _mainForm.Label1.Text = trackBar.Value.ToString();
        }

        private static void PrintFrames(object obj)
        {
            int frameNumber = 0;

            foreach (var frame in _framesA)
            {
                _mainForm.PictureBox1.Invoke((MethodInvoker)delegate {
                    // Running on the UI thread
                    _mainForm.PictureBox1.Image = PrintFrame(frame.Positions, _size);
                    _mainForm.Label1.Text = frameNumber.ToString();
                    _mainForm.TrackBar1.Value = frameNumber;
                });
                //Thread.Sleep(TimeSpan.FromMilliseconds(1));
                frameNumber++;
            }
        }

        private static Bitmap PrintFrame(IEnumerable<Vector2> positionsA, Size size)
        {
            var bitmap = new Bitmap(size.Width+1, size.Height+1); // add 1 so there is space to print the border
            var g = Graphics.FromImage(bitmap);

            g.DrawLine(Pens.Black, 0,          0,           size.Width, 0);
            g.DrawLine(Pens.Black, size.Width, 0,           size.Width, size.Height);
            g.DrawLine(Pens.Black, size.Width, size.Height, 0,          size.Height);
            g.DrawLine(Pens.Black, 0,          size.Height, 0,          0);

            foreach (var p in positionsA)
            {
                g.FillEllipse(Brushes.Black, p.X - 5, p.Y - 5, 10, 10);
            }

            return bitmap;
        }
    }
}
