using System.Collections.Generic;
using System.Drawing;

namespace WindowsFormsApp1
{
    public interface IWorker
    {
        IEnumerable<Particle[]> Simulate(IEnumerable<Particle> particles, Size size);
    }
}