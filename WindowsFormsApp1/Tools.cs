using System;
using System.Collections.Generic;
using System.IO;
using System.Xml.Serialization;
using WindowsFormsApp1;

public static class Tools
{
    public static void Compare(List<Frame> framesA, List<Frame> framesB)
    {
        var particlesCount = framesA[0].Positions.Count;
        for (int i = 0; i < framesA.Count; i++)
        {
            for (int j = 0; j < particlesCount; j++)
            {
                var a = framesA[i].Positions[j];
                var b = framesB[i].Positions[j];
                if ((a - b).Length() > 0.001)
                {
                    Console.WriteLine($"diff in frame {i}");
                }
            }
        }
    }

    public static void DumpToFile(List<Particle> particles, string fileName)
    {
        XmlSerializer ser = new XmlSerializer(typeof(List<Particle>));
        TextWriter writer = new StreamWriter(fileName);
        ser.Serialize(writer, particles);
        writer.Close();
    }

    public static List<Particle> ReadFromFile(string fileName)
    {
        XmlSerializer ser = new XmlSerializer(typeof(List<Particle>));
        using (var reader = new StreamReader(fileName))
        {
            var deserialize = ser.Deserialize(reader);
            reader.Close();
            return (List<Particle>) deserialize;
        }
    }
}