using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using MonoGameLibrary;
using MonoGameLibrary.Graphics;

namespace MonoGameLibrary
{
    public abstract class Projectile : GameObject, ICollidable, IPhysicsEnabled
    {
        public Vector2 Velocity { get; set; }
        public Vector2 Acceleration { get; set; }
        public float Drag { get; set; } = 0f; // Projectiles don't have drag
        public float Mass { set; get; } = 0.0f;
        public bool NeedsTraction { set; get; } = false;

        public bool Stationary { set; get; } = false;

        public Projectile(
            ContentManager content,
            Vector2 position,
            float rotation,
            float speed = 300f
        )
        {
            Position = position;
            Rotation = rotation;

            Velocity = new Vector2((float)Math.Cos(Rotation), (float)Math.Sin(Rotation)) * speed;

            Acceleration = Vector2.Zero;
        }

        public override void Update(GameTime gameTime)
        {
            float deltaTime = (float)gameTime.ElapsedGameTime.TotalSeconds;
        }

        public abstract Polygon GetBounds();

        public abstract override void Draw(SpriteBatch spriteBatch);
        public abstract string GetGroupName();
    }
}
