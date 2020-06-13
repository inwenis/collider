using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using WindowsFormsApp1;
using CsvHelper;
using CsvHelper.Configuration;
using CsvHelper.TypeConversion;

namespace ConsoleApp1
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine(MagicFormat(3.5f));
            Console.WriteLine(MagicFormat(3.51f));
            Console.WriteLine(MagicFormat(1.51f));
            Console.WriteLine(MagicFormat(12.51f));
            Console.WriteLine(MagicFormat(-12.51f));

            //Console.ReadLine();

            var particles = Tools.ReadFromFile(@"C:\git\collider\WindowsFormsApp1\bin\Debug\2020-06-12--23-20-03.xml");
            using (var writer = new StreamWriter("out.csv"))
            using (var csv = new CsvWriter(writer, CultureInfo.InvariantCulture))
            {
                csv.Configuration.RegisterClassMap<FooMap>();
                csv.WriteRecords(particles);
                writer.Flush();
            }

            var csvfw = ToCsvFixedWidth(particles);
            File.WriteAllText("out2.csv", csvfw);

            var allLines = File.ReadAllLines("out2.csv");
            var particles2 = Read(allLines);

            csvfw = ToCsvFixedWidth(particles2.ToList());
            File.WriteAllText("out3.csv", csvfw);

            Console.WriteLine(particles2);
            Console.ReadKey();
        }

        private static IEnumerable<Particle> Read(string[] allLines)
        {
            var header = allLines[0];
            var split = header.Split('|');
            var headers = split.Select(x => x.Replace(" ", ""));
            var rows = new List<Dictionary<string, string>>();
            foreach (var row in allLines.Skip(1))
            {
                split = row.Split('|');
                var values = split.Select(x => x.Replace(" ", ""));
                var rowAsDict = headers.Zip(values, (s, s1) => (s, s1)).ToDictionary(x => x.s, x => x.s1);
                rows.Add(rowAsDict);
            }

            var particles = rows.Select(x => ToParticle(x));
            return particles;
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

    public class Vector2Converter : DefaultTypeConverter
    {
        public override object ConvertFromString(string text, IReaderRow row, MemberMapData memberMapData)
        {
            return "";
            //return JsonConvert.DeserializeObject<T>(text);
        }

        public override string ConvertToString(object value, IWriterRow row, MemberMapData memberMapData)
        {
            Vector2 v = (Vector2) value;
            return $"({v.X} {v.Y})";
        }
    }

    public class FooMap : ClassMap<Particle>
    {
        public FooMap()
        {
            //Map(m => m.Id);
            //Map(m => m.Name);
            Map(m => m.Pos).TypeConverter<Vector2Converter>();
            Map(m => m.Vel).TypeConverter<Vector2Converter>();
        }
    }
}
