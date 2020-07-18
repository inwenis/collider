using System.Collections.Generic;
using System.Drawing;

namespace Collider
{
    public interface IWorker
    {
        IEnumerable<Particle[]> Simulate(IEnumerable<Particle> particles, Size size);
    }
}