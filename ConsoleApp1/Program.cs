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
            ParseCsv(allLines, out IEnumerable<Particle> particles2, out Options options2);

            csv = ToCsvFixedWidth(particles2.ToList());
            File.WriteAllText("out3.csv", csv);
        }

        private static string ToCsvHeaders(Options o)
        {
            // Only NumberOfFrame and size matter when reading options from a file.
            // Dimensions are called 'size' for user's convenience
            var s = $"{nameof(o.NumberOfFrames)}={o.NumberOfFrames}\n" +
                    $"size={string.Join(",", o.Dimensions)}\n";
            return s;
        }

        private static Options ParseFromCsvHeaders(string[] lines)
        {
            // expected format:
            // NumberOfFrames=1000
            // size = 800,800
            // ...
            var n = lines[0].Split('=')[1];
            var s = lines[1].Split('=')[1].Split(',');
            return new Options
            {
                NumberOfFrames = int.Parse(n),
                Dimensions = s.Select(x => int.Parse(x))
            };
        }


        private static void ParseCsv(string[] lines, out IEnumerable<Particle> particles, out Options options)
        {
            particles = ParseParticles(lines);
            options = ParseFromCsvHeaders(lines);
        }

        private static IEnumerable<Particle> ParseParticles(string[] lines)
        {
            var headers = lines[2].Split('|').Select(x => x.Trim()).ToArray();
            var particles = lines
                .Skip(3)
                .Select(x => RowToDict(x))
                .Select(x => DictToParticle(x));
            return particles;

            Dictionary<string, string> RowToDict(string row)
            {
                var values = row.Split('|').Select(x => x.Trim());
                var rowAsDict = headers.Zip(values, (h, v) => (h, v)).ToDictionary(x => x.h, x => x.v);
                return rowAsDict;
            }

            Particle DictToParticle(Dictionary<string, string> dictionary)
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
