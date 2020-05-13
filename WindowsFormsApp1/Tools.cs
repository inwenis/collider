using System;
using System.Collections.Generic;
using WindowsFormsApp1;

static internal class Tools
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
}