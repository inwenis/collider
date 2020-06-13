using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Text;
using WindowsFormsApp1;

namespace ConsoleApp1
{
    class Program
    {
        static void Main(string[] args)
        {
            var particles = Tools.ReadFromFile(@"C:\git\collider\WindowsFormsApp1\bin\Debug\2020-06-12--23-20-03.xml");

            var csv = ToCsvFixedWidth(particles);
            var options = new Options
            {
                Dimensions = new[] {800, 800},
                NumberOfFrames = 1000,
            };
            var top = ToCsvHeaders(options);
            File.WriteAllText("out2.csv", top + csv);

            var allLines = File.ReadAllLines("out2.csv");
            Read(allLines, out List<Particle> particles2, out Options options2);

            csv = ToCsvFixedWidth(particles2.ToList());
            File.WriteAllText("out3.csv", csv);

            Console.WriteLine(particles2);
            Console.ReadKey();
        }

        private static string ToCsvHeaders(Options op)
        {
            var s = $"{nameof(op.NumberOfFrames)}={op.NumberOfFrames}\n" +
                    $"size={string.Join(",", op.Dimensions)}\n";
            return s;
        }

        private static void Read(string[] allLines, out List<Particle> particles, out Options options)
        {
            var header = allLines[2];
            var split = header.Split('|');
            var headers = split.Select(x => x.Replace(" ", ""));
            var rows = new List<Dictionary<string, string>>();
            foreach (var row in allLines.Skip(3))
            {
                split = row.Split('|');
                var values = split.Select(x => x.Replace(" ", ""));
                var rowAsDict = headers.Zip(values, (s, s1) => (s, s1)).ToDictionary(x => x.s, x => x.s1);
                rows.Add(rowAsDict);
            }

            particles = rows.Select(x => ToParticle(x)).ToList();

            var n = allLines[0].Split('=')[1];
            var dims = allLines[1].Split('=')[1].Split(',');
            options = new Options
            {
                NumberOfFrames = int.Parse(n),
                Dimensions = dims.Select(x => int.Parse(x))
            };
        }

        private static Particle ToParticle(Dictionary<string, string> dictionary)
        {
            var posX = float.Parse(dictionary["positionX"]);
            var posY = float.Parse(dictionary["positionY"]);
            var velX = float.Parse(dictionary["velocityX"]);
            var velY = float.Parse(dictionary["velocityY"]);
            var sig = int.Parse(dictionary["radius"]);
            return new Particle
            {
                Pos = new Vector2(posX, posY),
                Vel = new Vector2(velX, velY),
                Sig = sig
            };
        }

        private static string MagicFormat(float f)
        {
            var exactString = DoubleConverter.ToExactString(f);
            var indexOf = exactString.IndexOf(".");
            if (indexOf == -1)
            {
                exactString += ".0";
            }
            indexOf = exactString.IndexOf(".");
            var addZeroestoRight = 36 - (exactString.Length - indexOf);
            var spacesLeft = 10 - indexOf;
            var x = string.Join("", Enumerable.Repeat(" ", addZeroestoRight));
            var y = string.Join("", Enumerable.Repeat(" ", spacesLeft));
            return y + exactString + x;
        }

        private static string ToCsvFixedWidth(List<Particle> particles)
        {
            StringBuilder sb = new StringBuilder();

            var header = $"{"positionX",46}|{"positionY",46}|{"velocityX",46}|{"velocityY",46}|{"radius",5}";
            sb.AppendLine(header);

            foreach (var p in particles)
            {
                var line = $"{MagicFormat(p.Pos.X)}|{MagicFormat(p.Pos.Y)}|{MagicFormat(p.Vel.X)}|{MagicFormat(p.Vel.Y)}|{p.Sig,5}";
                sb.AppendLine(line);
            }

            return sb.ToString();
        }
    }
}
