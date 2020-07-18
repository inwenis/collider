using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using CommandLine;

namespace WindowsFormsApp1
{
    public class Options
    {
        [Option('f', "frames", Required = false, Default = 1000)]
        public int NumberOfFrames { get; set; }

        [Option('n', "particles", Required = false, Default = 100)]
        public int NumberOfParticles { get; set; }

        [Option('r', "radius", Required = false, Default = 5)]
        public int Radius { get; set; }

        [Option('i', "file", Required = false)]
        public string ParticlesFile  { get; set; }

        [Option('s', "size", Separator = ',', Min = 2, Max = 2, Required = false, Default = new []{ 400, 400})]
        public IEnumerable<int> Dimensions { get; set; }

        public Size Size
        {
            get
            {
                var size = Dimensions.ToArray();
                return new Size(size[0], size[1]);
            }
        }

        public string ToInputArgumentsString()
        {
            if (ParticlesFile != null)
            {
                return $"--{nameof(NumberOfFrames)}={NumberOfFrames} " +
                       $"--{nameof(Radius)}={Radius} " +
                       $"--{nameof(ParticlesFile)}={ParticlesFile} " +
                       $"--size={string.Join(",", Dimensions)}";
            }
            else
            {
                return $"--{nameof(NumberOfFrames)}={NumberOfFrames} " +
                       $"--{nameof(Radius)}={Radius} " +
                       $"--{nameof(NumberOfParticles)}={NumberOfParticles} " +
                       $"--size={string.Join(",", Dimensions)}";
            }
        }
    }
}