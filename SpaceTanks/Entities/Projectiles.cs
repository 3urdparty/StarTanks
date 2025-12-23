using System;
using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using MonoGameLibrary;
using MonoGameLibrary.Graphics;
using MonoGameLibrary.Graphics;
using nkast.Aether.Physics2D.Dynamics;
using nkast.Aether.Physics2D.Dynamics;
using nkast.Aether.Physics2D.Dynamics;
using nkast.Aether.Physics2D.Dynamics;
using nkast.Aether.Physics2D.Dynamics.Contacts;
using nkast.Aether.Physics2D.Dynamics.Joints;
using AetherVector2 = nkast.Aether.Physics2D.Common.Vector2;

namespace SpaceTanks
{
    public class ProjectilePhysics : PhysicsEntity
    {
        public Body Body { get; private set; }

        /// <summary>
        /// Initialize physics body for a projectile.
        /// </summary>
        public override List<Body> GetBodies()
        {
            return [Body];
        }

        public ProjectilePhysics()
        {
            Mass = 10f;
        }

        public void Initialize(World world, Projectile projectile)
        {
            // Create projectile body
            AetherVector2 physicsPos = new AetherVector2(
                projectile.Position.X / 100f,
                projectile.Position.Y / 100f
            );

            Body = world.CreateBody(physicsPos, 0, BodyType.Dynamic);

            var fixture = Body.CreateRectangle(
                projectile.Width / 100f,
                projectile.Height / 100f,
                Mass,
                AetherVector2.Zero
            );

            fixture.Friction = 0.3f;
            fixture.Restitution = 0.4f;
            // Add collision callbacks
            // fixture.BeginContact += OnBeginContact;
            // fixture.EndContact += OnEndContact;

            // Body.Rotation = projectile.Rotation;

            // Set initial velocity based on gun angle and speed
            // Convert pixel speed to physics speed
            float speed = 10f;
            float physicsSpeed = speed / 100f;
            Body.LinearVelocity = new AetherVector2(
                (float)System.Math.Cos(projectile.Rotation) * physicsSpeed,
                (float)System.Math.Sin(projectile.Rotation) * physicsSpeed
            );
        }

        // In ProjectilePhysics

        public override void Sync(GameObject gameObject)
        {
            Projectile projectile = (Projectile)gameObject;
            if (Body != null)
            {
                // // Update position from physics (convert from physics units to pixels)
                projectile.Position = new Vector2(Body.Position.X * 100f, Body.Position.Y * 100f);

                // Update rotation from physics
                projectile.Rotation = Body.Rotation;
            }
        }

        /// <summary>
        /// Get projectile position in pixels.
        /// </summary>
        public Vector2 GetPosition()
        {
            return new Vector2(Body.Position.X * 100f, Body.Position.Y * 100f);
        }

        public float GetRotation()
        {
            return Body.Rotation;
        }
    }

    public abstract class Projectile : GameObject
    {
        private Tank tank;

        public Projectile() { }

        public virtual void Initialize(ContentManager content) { }

        public override void Update(GameTime gameTime) { }

        public abstract override void Draw(SpriteBatch spriteBatch);
    }
}
