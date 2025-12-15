using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using MonoGameLibrary;
using MonoGameLibrary.Graphics;

namespace SpaceTanks
{
    public class Missile : Projectile
    {
        private readonly ContentManager _content;
        private TextureRegion _sprite;

        public Color Color { get; set; } = Color.White;
        public SpriteEffects Effects { get; set; } = SpriteEffects.None;
        public float LayerDepth { get; set; } = 0.1f;

        public Missile(ContentManager content, Vector2 position, float rotation, float speed = 300f)
            : base(content, position, rotation, speed)
        {
            Mass = 0.25f;
            _content = content;
            Name = "missile";

            TextureAtlas atlas = TextureAtlas.FromFile(_content, "atlas.xml");
            _sprite = atlas.GetRegion("missile-1");
            Origin = new Vector2(_sprite.Width, _sprite.Height) * 0.5f;

            Position = position;
            Rotation = rotation;
            Width = _sprite.Width;
            Height = _sprite.Height;

            // Calculate velocity from rotation
            Velocity = new Vector2((float)Math.Cos(rotation), (float)Math.Sin(rotation)) * speed;

            Acceleration = Vector2.Zero;
        }

        public override void Update(GameTime gameTime)
        {
            float deltaTime = (float)gameTime.ElapsedGameTime.TotalSeconds;
        }

        public override void Draw(SpriteBatch spriteBatch)
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

        public override Rectangle GetBound()
        {
            return new Rectangle(
                (int)(Position.X - Origin.X),
                (int)(Position.Y - Origin.Y),
                (int)Width,
                (int)Height
            );
        }

        public override string GetGroupName()
        {
            return "missile";
        }

        public bool IsOutOfBounds(int screenWidth, int screenHeight, int margin = 100)
        {
            return Position.X < -margin
                || Position.X > screenWidth + margin
                || Position.Y < -margin
                || Position.Y > screenHeight + margin;
        }
    }
}
