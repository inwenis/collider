namespace WindowsFormsApp1
{
    internal class Collision
    {
        public Particle ParticleI { get; }
        public Particle ParticleJ { get; }
        public float Dt { get; }
        public bool IsWallCollision { get; }
        public string Wall { get; }

        public Collision(Particle particleI, float dt, bool isWallCollision, string wall)
        {
            ParticleI = particleI;
            Dt = dt;
            IsWallCollision = isWallCollision;
            Wall = wall;
        }

        public Collision(Particle particleI, Particle particleJ, float dt)
        {
            ParticleI = particleI;
            ParticleJ = particleJ;
            Dt = dt;
            IsWallCollision = false;
        }
    }
}