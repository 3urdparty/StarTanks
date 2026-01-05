using System.Collections.Generic;
using Microsoft.Xna.Framework;
using nkast.Aether.Physics2D.Dynamics;
using nkast.Aether.Physics2D.Dynamics.Contacts;

namespace SpaceTanks
{
    public abstract class PhysicsEntity
    {
        public event OnCollisionEventHandler OnCollision;

        protected bool ForwardOnCollision(Fixture fixtureA, Fixture fixtureB, Contact contact)
        {
            if (OnCollision != null)
                return OnCollision.Invoke(fixtureA, fixtureB, contact);
            return true;
        }

        public float Mass { set; get; } = 1f;
        public abstract List<Body> GetBodies();

        public delegate void CollisionHandler(Body otherBody, World world, Vector2 contactPosition);

        public virtual void Initialize(World world, GameObject gameObject)
        {
            foreach (Body body in GetBodies())
            {
                body.OnCollision += ForwardOnCollision;
            }
        }

        public List<Body> IgnoreCollisions { get; set; } = new List<Body>();

        public bool Contains(Body body)
        {
            if (body == null)
                return false;

            var bodies = GetBodies();
            if (bodies == null)
                return false;

            return bodies.Contains(body);
        }

        public virtual void Update(GameObject gameObject)
        {
            if (gameObject == null)
                return;

            var bodies = GetBodies();
            if (bodies == null || bodies.Count == 0)
                return;

            Body primaryBody = bodies[0];
            if (primaryBody != null)
            {
                gameObject.Position = new Vector2(
                    primaryBody.Position.X * 100f,
                    primaryBody.Position.Y * 100f
                );

                gameObject.Rotation = primaryBody.Rotation;
            }
        }
    }
}
