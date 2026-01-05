using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using AetherVector2 = nkast.Aether.Physics2D.Common.Vector2;

namespace SpaceTanks
{
    public class MissilePhysics : ProjectilePhysics
    {
        public override void Update(GameObject gameObject)
        {
            if (Body == null)
                return;

            Projectile projectile = (Projectile)gameObject;

            projectile.Position = new Vector2(Body.Position.X * 100f, Body.Position.Y * 100f);

            AetherVector2 velocity = Body.LinearVelocity;
            if (velocity.LengthSquared() > 0.0001f)
            {
                float angle = (float)Math.Atan2(velocity.Y, velocity.X);
                Body.Rotation = angle;
            }

            projectile.Rotation = Body.Rotation;
        }
    }

    public class Missile : Projectile
    {
        protected Animation _explosionAnimation;
        private bool _isExploding = false;

        public Missile()
            : base()
        {
            Name = "missile";
        }

        public override void Initialize(ContentManager content)
        {
            TextureAtlas atlas = TextureAtlas.FromFile(content, "atlas.xml");
            _sprite = atlas.GetRegion("missile-1");
            Origin = new Vector2(_sprite.Width, _sprite.Height) * 0.5f;
            Width = _sprite.Width;
            Height = _sprite.Height;

            _explosionAnimation = atlas.GetAnimation("explosion-anim");
            _explosionAnimation.Loop = false;
        }

        public override void Update(GameTime gameTime)
        {
            base.Update(gameTime);
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
                base.Draw(spriteBatch);
            }
        }

        public void Explode()
        {
            if (!_isExploding)
            {
                _isExploding = true;
                _explosionAnimation.Reset();
                ExplosionSound?.Play();
            }
        }
    }
}
