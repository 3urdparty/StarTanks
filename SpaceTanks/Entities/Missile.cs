using System;
using System.Collections.Generic;
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
        Polygon Collision { get; }

        private Animation _explosionAnimation;
        private bool _isExploding = false;

        public Missile(ContentManager content, Vector2 position, float rotation, float speed = 300f)
            : base(content, position, rotation, speed)
        {
            Stationary = false;
            // Mass = 0.005f;
            Mass = 0.05f;
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
            Collision = new Polygon([
                Vector2.Zero,
                new Vector2(Width, 0),
                new Vector2(Width, Height),
                new Vector2(0, Height),
            ]);

            // Load explosion animation from atlas
            _explosionAnimation = atlas.GetAnimation("explosion-anim");
            _explosionAnimation.Loop = false;
        }

        public override void Update(GameTime gameTime)
        {
            float deltaTime = (float)gameTime.ElapsedGameTime.TotalSeconds;

            if (_isExploding)
            {
                _explosionAnimation.Update(deltaTime);
                if (_explosionAnimation.HasFinished)
                {
                    Destroyed = true;
                }
            }
        }

        public override void Draw(SpriteBatch spriteBatch)
        {
            if (_isExploding)
            {
                // Draw explosion animation
                if (_explosionAnimation.CurrentFrame != null)
                {
                    _explosionAnimation.CurrentFrame.Draw(
                        spriteBatch,
                        Position,
                        Color,
                        0f,
                        new Vector2(
                            _explosionAnimation.CurrentFrame.Width * 0.5f,
                            _explosionAnimation.CurrentFrame.Height * 0.5f
                        ),
                        Vector2.One,
                        Effects,
                        LayerDepth
                    );
                }
            }
            else
            {
                // Draw missile sprite
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

        public override Polygon GetBounds()
        {
            return Collision + Position - Origin;
        }

        public override string GetGroupName()
        {
            return "Missile";
        }

        public virtual void OnCollision(CollisionInfo collisionInfo)
        {
            if (!_isExploding)
            {
                Stationary = true;
                _isExploding = true;
                _explosionAnimation.Reset();
                Velocity = Vector2.Zero;
                Acceleration = Vector2.Zero;
            }
        }
    }
}
