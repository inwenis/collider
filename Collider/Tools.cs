using System.Collections.Generic;
using System.Linq;

namespace Collider
{
    public static class Tools
    {
        public static (List<int> framesWithDifferences, Dictionary<int, FrameDiff> framesComparisons) Compare(List<Particle[]> framesA, List<Particle[]> framesB)
        {
            var count = framesA[0].Length;
            var framesWithDifferences = new List<int>();
            var framesComparisons = new Dictionary<int, FrameDiff>();

            for (int i = 0; i < framesA.Count; i++)
            {
                var pairs = framesA[i].Zip(framesB[i], (a, b) => (a, b)).ToList();
                var diff = pairs.Sum(x => (x.a.Pos - x.b.Pos).Length());
                if (pairs.Any(x => (x.a.Pos - x.b.Pos).Length() > 0.001))
                {
                    framesWithDifferences.Add(i);
                    framesComparisons.Add(i, new FrameDiff{ TotalDiff = diff, AverageDiff = diff / count});
                }

            }

            return (framesWithDifferences, framesComparisons);
        }
    }

    public class FrameDiff
    {
        public float TotalDiff { get; set; }
        public float AverageDiff { get; set; }
    }
}