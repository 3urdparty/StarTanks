using System.Collections.Generic;
using Microsoft.Xna.Framework;
using MonoGameLibrary;
using nkast.Aether.Physics2D.Dynamics;

namespace SpaceTanks
{
    /// <summary>
    /// Base class for all physics-enabled game entities.
    /// Manages syncing between visual properties and physics body.
    /// </summary>
    public abstract class PhysicsEntity
    {
        public float Mass { set; get; } = 1f;
        public abstract List<Body> GetBodies();

        public delegate void CollisionHandler(Body otherBody, World world, Vector2 contactPosition);
        public event CollisionHandler OnCollision; // Add Vector2 for contact position

        public List<Body> IgnoreCollisions { get; set; } = new List<Body>();

        public void CheckCollisions(World world)
        {
            var bodies = GetBodies();
            if (bodies == null)
                return;

            foreach (var body in bodies)
            {
                if (body.ContactList != null && body.ContactList.Contact.IsTouching)
                {
                    Body otherBody = body.ContactList.Other;
                    if (!IgnoreCollisions.Contains(otherBody))
                    {
                        OnCollision?.Invoke(otherBody, world, new Vector2(0, 0));
                    }
                }
            }
        }

        /// <summary>
        /// Sync physics bodies to a game object's visual properties.
        /// </summary>
        public virtual void Sync(GameObject gameObject)
        {
            if (gameObject == null)
                return;

            var bodies = GetBodies();
            if (bodies == null || bodies.Count == 0)
                return;

            // Sync primary body (usually chassis/main body)
            Body primaryBody = bodies[0];
            if (primaryBody != null)
            {
                // Update position from physics (convert from physics units to pixels)
                gameObject.Position = new Vector2(
                    primaryBody.Position.X * 100f,
                    primaryBody.Position.Y * 100f
                );

                // Update rotation from physics
                gameObject.Rotation = primaryBody.Rotation;
            }
        }
    }
}
