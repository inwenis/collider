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
            var options = new Options
            {
                Dimensions = new[] {800, 800},
                NumberOfFrames = 1000,
            };

            var serializedToCsv = ToCsvFixedWidth(options, particles);
            File.WriteAllText("out2.csv", serializedToCsv);

            var allLines = File.ReadAllLines("out2.csv");
            ParseCsv(allLines, out var options2, out var particles2);

            var deserializedFromCsv = ToCsvFixedWidth(particles2.ToList());
            File.WriteAllText("out3.csv", deserializedFromCsv);
        }

        private static string ToCsvFixedWidth(Options options, List<Particle> particles)
        {
            var top = ToCsvHeaders(options);
            var csv = ToCsvFixedWidth(particles);
            return $"{top}{csv}";
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

        private static void ParseCsv(string[] lines, out Options options, out IEnumerable<Particle> particles)
        {
            options = ParseFromCsvHeaders(lines);
            particles = ParseParticles(lines);
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

        private static string ToCsvFixedWidth(List<Particle> particles)
        {
            var sb = new StringBuilder();

            var header = $"{"positionX",46}|{"positionY",46}|{"velocityX",46}|{"velocityY",46}|{"radius",5}";
            sb.AppendLine(header);

            foreach (var p in particles)
            {
                var line = $"{Format(p.Pos.X, 10, 35)}|{Format(p.Pos.Y, 10, 35)}|{Format(p.Vel.X, 10, 35)}|{Format(p.Vel.Y, 10, 35)}|{p.Sig,5}";
                sb.AppendLine(line);
            }

            return sb.ToString();

            string Format(float f, int chartBeforeDot, int charsAfterDot)
            {
                var exactString = DoubleConverter.ToExactString(f);
                var indexOfDot = exactString.IndexOf(".");
                if (indexOfDot == -1)
                {
                    exactString += ".0";
                    indexOfDot = exactString.IndexOf(".");
                }
                var spacesRight = charsAfterDot + 1 - (exactString.Length - indexOfDot);
                var spacesLeft = chartBeforeDot - indexOfDot;
                return $"{new string(' ', spacesLeft)}{exactString}{new string(' ', spacesRight)}";
            }
        }
    }
}
