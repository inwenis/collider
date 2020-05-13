using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Threading;
using System.Windows.Forms;
using System.Numerics;
using System.Xml.Serialization;
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
                particles = ReadFromFile("input.xml");
            }
            else
            {
                particles = ParticlesGenerator.RandomParticles(20);
                var fastParticles = ParticlesGenerator.RandomFastParticles(10);
                particles.AddRange(fastParticles);
                DumpToFile(particles, $"{DateTime.Now:yyyy-MM-dd--HH-mm-ss}.xml");
            }

            var particlesClone = particles.Select(x => x.Clone()).ToList();

            Compare(particles, particlesClone);

            var w = new Worker();
            var frames = w.Simulate(nFrames, particles);

            var wa = new WorkerArray();
            var framesA = wa.Simulate(nFrames, particlesClone);

            Compare(frames, framesA);

            _mainForm = new Form1();
            _mainForm.TrackBar1.Minimum = 0;
            _mainForm.TrackBar1.Maximum = nFrames - 1;
            _mainForm.TrackBar1.Scroll += TrackBar1_Scroll;

            _frames = frames;
            Timer t = new Timer(PrintFrames, (_mainForm, _frames), nFrames, int.MaxValue);

            Application.Run(_mainForm);
        }

        private static List<Particle> ReadFromFile(string fileName)
        {
            XmlSerializer ser = new XmlSerializer(typeof(List<Particle>));
            using (var reader = new StreamReader(fileName))
            {
                var deserialize = ser.Deserialize(reader);
                reader.Close();
                return (List<Particle>) deserialize;
            }
        }

        public static void DumpToFile(List<Particle> particles, string fileName)
        {
            XmlSerializer ser = new XmlSerializer(typeof(List<Particle>));
            TextWriter writer = new StreamWriter(fileName);
            ser.Serialize(writer, particles);
            writer.Close();
        }

        private static void Compare(List<Particle> particles, List<Particle> particlesClone)
        {
            for (int i = 0; i < particles.Count; i++)
            {
                Particle a = particles[i];
                Particle b = particlesClone[i];
                if (a.Pos != b.Pos || a.Vel != b.Vel)
                {
                    Console.WriteLine("diff particles");
                }
            }
        }

        private static void Compare(List<Frame> frames, List<Frame> framesA)
        {
            for (int i = 0; i < frames.Count; i++)
            {
                for (int j = 0; j < frames[i].Positions.Count; j++)
                {
                    var a = frames[i].Positions[j];
                    var b = framesA[i].Positions[j];
                    if ((a - b).Length() > 0.001)
                    {
                        Console.WriteLine("diff in frame!");
                    }
                }
            }
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
