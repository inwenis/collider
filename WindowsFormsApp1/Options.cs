using System.Collections.Generic;
using CommandLine;

namespace WindowsFormsApp1
{
    class Options
    {
        [Option('f', "frames", Required = false, Default = 1000)]
        public int NumberOfFrames { get; set; }

        [Option('n', "particles", Required = false, Default = 100)]
        public int NumberOfParticles { get; set; }

        [Option('i', "particlesFile", Required = false)]
        public string ParticlesFile  { get; set; }

        [Option('s', "size", Separator = ',', Min = 2, Max = 2, Required = false, Default = new int[] { 400, 400})]
        public IEnumerable<int> Dimensions { get; set; }
    }
}