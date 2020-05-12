namespace WindowsFormsApp1
{
    public class Collision
    {
        public Particle ParticleI { get; }
        public Particle ParticleJ { get; }
        public float Dt { get; }
        public bool IsWallCollision { get; }
        public string Wall { get; }
        public int IndexI { get; set; }
        public int IndexJ { get; set; }

        public Collision(Particle particleI, int indexI, float dt, bool isWallCollision, string wall)
        {
            ParticleI = particleI;
            IndexI = indexI;
            Dt = dt;
            IsWallCollision = isWallCollision;
            Wall = wall;
        }

        public Collision(Particle particleI, int indexI, Particle particleJ, int indexJ, float dt)
        {
            ParticleI = particleI;
            IndexI = indexI;
            ParticleJ = particleJ;
            IndexJ = indexJ;
            Dt = dt;
            IsWallCollision = false;
        }
    }
}