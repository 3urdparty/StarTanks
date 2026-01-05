using nkast.Aether.Physics2D.Dynamics;
using SpaceTanks;
using SpaceTanks.Extensions;

namespace SpaceTanks.Extensions
{
    public static class WorldExtensions
    {
        public static void AddEntity(this World world, PhysicsEntity entity)
        {
            if (entity == null)
                return;

            var bodies = entity.GetBodies();
            if (bodies != null)
            {
                foreach (var body in bodies)
                {
                    world.Add(body);
                }
            }
        }

        public static void Remove(this World world, PhysicsEntity entity)
        {
            if (entity == null)
                return;

            var bodies = entity.GetBodies();
            if (bodies != null)
            {
                foreach (var body in bodies)
                {
                    world.Remove(body);
                }
            }
        }
    }
}
