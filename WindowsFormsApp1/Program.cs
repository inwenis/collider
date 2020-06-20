using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using System.Numerics;
using System.Threading.Tasks;
using WindowsFormsApp1.Csv;
using CommandLine;
using Timer = System.Threading.Timer;

namespace WindowsFormsApp1
{
    static class Program
    {
        private static Form1 _mainForm;
        private static Size _size;
        private static List<byte[]> _framesAsGifs;

        static async Task Main(string[] args)
        {
            var parserResult = Parser.Default.ParseArguments<Options>(args);
            var options = ((Parsed<Options>) parserResult).Value;

            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            List<Particle> particles;
            if (options.ParticlesFile != null)
            {
                var lines = File.ReadAllLines(options.ParticlesFile);
                // not super happy about this code cuz we overwrite options here
                CsvSerializer.ParseCsv(lines, out options, out var outParticles);
                particles = outParticles.ToList();

                var size = options.Dimensions.ToArray();
                _size = new Size(size[0], size[1]);
            }
            else
            {
                var size = options.Dimensions.ToArray();
                _size = new Size(size[0], size[1]);

                particles = new List<Particle> {new Particle {Pos = new Vector2(200, 200), Vel = Vector2.Zero, Sig = 20, Mass = 20}};
                ParticlesGenerator.AddRandomParticles(particles, options.NumberOfParticles, options.Radius, 1, _size);

                var serializedToCsv = CsvSerializer.ToCsvFixedWidth(options, particles);
                var fileName = $"{DateTime.Now:yyyy-MM-dd--HH-mm-ss}.csv";
                File.WriteAllText(fileName, serializedToCsv);
                Console.WriteLine($"Particles saved to {fileName}. To rerun use: --file={fileName}");
            }

            var w = new WorkerArray();

            var frames = await Task.Run(() =>
            {
                var list = new List<Particle[]>();
                var sw = Stopwatch.StartNew();
                foreach (var (frame, i) in w
                    .Simulate(particles, _size)
                    .Take(options.NumberOfFrames)
                    .Select((frame, i) => (frame, i)))
                {
                    HandleProgress(i, options.NumberOfFrames, sw.Elapsed);
                    list.Add(frame);
                }
                sw.Stop();
                return list;
            });

            Console.WriteLine("Printing frames");
            _framesAsGifs = frames
                .AsParallel()
                .Select(x => FrameToGifBytes(x, _size))
                .ToList();

            _mainForm = new Form1();
            _mainForm.TrackBar1.Minimum = 0;
            _mainForm.TrackBar1.Maximum = options.NumberOfFrames - 1;
            _mainForm.TrackBar1.Scroll += TrackBar1_Scroll;

            Timer t = new Timer(obj => PrintFrames(), null, 500, -1); // wait 500ms before starting timer to let window be created

            Application.Run(_mainForm);
        }

        private static void HandleProgress(int currentItem, int totalItems, TimeSpan elapsed)
        {
            if (currentItem % (totalItems / 100) == 0) // print 100 progress updates
            {
                var progress = (double)currentItem / totalItems;
                var tte = currentItem != 0 // total time estimated
                    ? TimeSpan.FromMilliseconds(elapsed.TotalMilliseconds / progress)
                    : TimeSpan.MaxValue;
                var rem = tte - elapsed; // remaining
                Console.WriteLine($"{progress*100,3}% passed={elapsed} total estimated={tte} remaining={rem}");
            }
        }

        private static void TrackBar1_Scroll(object sender, EventArgs e)
        {
            var trackBar = (TrackBar) sender;
            var frame = _framesAsGifs[trackBar.Value];
            _mainForm.PictureBox1.Image = Image.FromStream(new MemoryStream(frame));
            _mainForm.Label1.Text = trackBar.Value.ToString();
        }

        private static void PrintFrames()
        {
            foreach (var (frame, i) in _framesAsGifs.Select((x, i) => (x, i)))
            {
                _mainForm.PictureBox1.Invoke((MethodInvoker) delegate
                {
                    // Running on the UI thread
                    _mainForm.PictureBox1.Image = Image.FromStream(new MemoryStream(frame));
                    _mainForm.Label1.Text = i.ToString();
                    _mainForm.TrackBar1.Value = i;
                });
                //Thread.Sleep(TimeSpan.FromMilliseconds(1));
            }
        }

        private static byte[] FrameToGifBytes(Particle[] particles, Size size)
        {
            // add 1 so there is space to print the border
            using (var bitmap = new Bitmap(size.Width + 1, size.Height + 1))
            using (var g = Graphics.FromImage(bitmap))
            using (var memStream = new MemoryStream())
            {
                g.Clear(Color.White);
                g.DrawLine(Pens.Black, 0,          0,           size.Width, 0);
                g.DrawLine(Pens.Black, size.Width, 0,           size.Width, size.Height);
                g.DrawLine(Pens.Black, size.Width, size.Height, 0,          size.Height);
                g.DrawLine(Pens.Black, 0,          size.Height, 0,          0);

                foreach (var p in particles)
                {
                    g.FillEllipse(Brushes.Black, p.Pos.X - p.Sig, p.Pos.Y - p.Sig, 2 * p.Sig, 2 * p.Sig);
                }
                bitmap.Save(memStream, ImageFormat.Gif);
                return memStream.ToArray();
            }
        }
    }
}
