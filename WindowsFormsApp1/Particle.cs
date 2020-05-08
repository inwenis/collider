using System.Numerics;

namespace WindowsFormsApp1
{
    public class Particle
    {
        public Vector2 Pos { get; set; }
        public Vector2 Vel { get; set; }

        public Particle Clone()
        {
            return new Particle {Pos = Pos, Vel = Vel};
        }

        public override string ToString()
        {
            return $"{Pos} {Vel}";
        }
    }
}