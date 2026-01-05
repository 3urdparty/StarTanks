using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using nkast.Aether.Physics2D.Dynamics;
using AetherVector2 = nkast.Aether.Physics2D.Common.Vector2;

namespace SpaceTanks
{
    public class ProjectilePhysics : PhysicsEntity
    {
        public Body Body { get; protected set; }

        public override List<Body> GetBodies()
        {
            return [Body];
        }

        public ProjectilePhysics()
        {
            Mass = 10f;
        }

        public virtual void Initialize(World world, Projectile projectile)
        {
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
            float speed = 10f;
            float physicsSpeed = speed / 100f;
            Body.LinearVelocity = new AetherVector2(
                (float)System.Math.Cos(projectile.Rotation) * physicsSpeed,
                (float)System.Math.Sin(projectile.Rotation) * physicsSpeed
            );
            Body.Tag = "Projectile";
            projectile.PhysicsEntityRef = this;
        }

        

        public override void Update(GameObject gameObject)
        {
            Projectile projectile = (Projectile)gameObject;
            if (Body != null)
            {
                
                projectile.Position = new Vector2(Body.Position.X * 100f, Body.Position.Y * 100f);

                
                projectile.Rotation = Body.Rotation;
            }
        }

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

        protected TextureRegion _sprite;
        public PhysicsEntity PhysicsEntityRef { get; internal set; }

        public Projectile() { }

        public virtual void Initialize(ContentManager content) { }

        public override void Update(GameTime gameTime) { }

        public override void Draw(SpriteBatch spriteBatch)
        {
            {
                
                _sprite.Draw(
                    spriteBatch,
                    Position,
                    Color,
                    Rotation,
                    new Vector2(_sprite.Width * 0.5f, _sprite.Height * 0.5f),
                    Vector2.One,
                    Effects,
                    LayerDepth
                );
            }
        }
    }
}
