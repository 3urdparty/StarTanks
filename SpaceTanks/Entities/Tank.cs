using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using MonoGameLibrary;
using MonoGameLibrary.Graphics;

public enum AmmunitionType
{
    Missile,
    Blaster,
}

namespace SpaceTanks
{
    public class Tank : GameObject, ICollidable, IPhysicsEnabled, IGrounded
    {
        private AmmunitionType _ammunitionType;
        private bool _isRecoiling;
        private int _currentFrame;
        private TimeSpan _elapsed;
        private readonly ContentManager _content;
        private TextureRegion _body;
        private TextureRegion _turret;
        private TextureRegion _gun;
        private Animation _body_animation;
        private Animation _recoil_animation;
        public bool isGrounded { set; get; }

        // Physics
        public Vector2 Velocity { get; set; }
        public Vector2 Acceleration { get; set; }
        public float Drag { get; set; } = 0.9f;
        public float Mass { get; set; } = 1f;

        // Rendering
        public Color Color { get; set; } = Color.White;
        public Vector2 Scale { get; set; } = Vector2.One;
        public SpriteEffects Effects { get; set; } = SpriteEffects.None;
        public float LayerDepth { get; set; } = 0.0f;

        // Gun and shooting
        private float _gunRotation = -MathHelper.PiOver2;
        private float _shootCooldown = 0f;
        private const float SHOOT_DELAY = 1f; // Changed to seconds
        private const float AccelerationForce = 200f;

        public bool NeedsTraction { get; set; } = true;

        // Gun position helpers
        private Vector2 _gunPosition => new Vector2(Position.X, Position.Y - Height / 2);
        private float _gunLength => _gun.Width;

        public Tank(ContentManager content, AmmunitionType type = AmmunitionType.Missile)
        {
            _ammunitionType = type;
            _content = content;
            Position = new Vector2(100f, 100f);
            Name = "tank";

            TextureAtlas atlas = TextureAtlas.FromFile(_content, "atlas.xml");
            _body_animation = atlas.GetAnimation("tank-green-moving");
            _body = atlas.GetRegion("tank-green-1");
            _recoil_animation = atlas.GetAnimation("recoil-anim");
            _recoil_animation.Loop = false;
            _turret = atlas.GetRegion("turret");
            _gun = atlas.GetRegion("gun");

            Origin = new Vector2(_body.Width, _body.Height) * 0.5f;
            Width = _body.Width * Scale.X;
            Height = _body.Height * Scale.Y;

            Acceleration = Vector2.Zero;
            Velocity = Vector2.Zero;
        }

        public void MoveLeft()
        {
            Acceleration = new Vector2(-AccelerationForce, 0);
        }

        public void MoveRight()
        {
            Acceleration = new Vector2(AccelerationForce, 0);
        }

        public void StopMoving()
        {
            Acceleration = Vector2.Zero;
        }

        public void RotateGunLeft(float deltaTime)
        {
            _gunRotation -= MathHelper.PiOver2 * deltaTime;
            _gunRotation = MathHelper.Clamp(
                _gunRotation,
                -MathHelper.Pi * 7 / 8,
                -MathHelper.Pi * 1 / 8
            );
        }

        public void RotateGunRight(float deltaTime)
        {
            _gunRotation += MathHelper.PiOver2 * deltaTime;
            _gunRotation = MathHelper.Clamp(
                _gunRotation,
                -MathHelper.Pi * 7 / 8,
                -MathHelper.Pi * 1 / 8
            );
        }

        public Projectile Shoot()
        {
            if (_shootCooldown > 0f)
                return null;

            _shootCooldown = SHOOT_DELAY;
            _isRecoiling = true;
            _recoil_animation.Reset();

            Vector2 gunTip = new Vector2(
                _gunPosition.X + (float)Math.Cos(_gunRotation) * _gunLength,
                _gunPosition.Y + (float)Math.Sin(_gunRotation) * _gunLength
            );

            Missile projectile = new Missile(_content, gunTip, _gunRotation, speed: 250f);
            return projectile;
        }

        public override void Update(GameTime gameTime)
        {
            _elapsed += gameTime.ElapsedGameTime;
            UpdateAnimationFrame(_elapsed);
        }

        private void UpdateAnimationFrame(TimeSpan elapsedTime)
        {
            float deltaTime = (float)elapsedTime.TotalSeconds;

            // Update shoot cooldown
            if (_shootCooldown > 0f)
                _shootCooldown -= deltaTime;

            // Update body animation
            if (elapsedTime >= _body_animation.Delay)
            {
                _elapsed -= _body_animation.Delay;
                _currentFrame++;
                if (_currentFrame >= _body_animation.Frames.Count)
                    _currentFrame = 0;
                _body = _body_animation.Frames[_currentFrame];
            }

            // Update recoil animation
            if (_isRecoiling)
            {
                _recoil_animation.Update(deltaTime);
                if (_recoil_animation.HasFinished)
                    _isRecoiling = false;
            }
        }

        public override void Draw(SpriteBatch spriteBatch)
        {
            // Draw gun
            _gun.Draw(
                spriteBatch,
                _gunPosition,
                Color,
                _gunRotation,
                new Vector2(0, _gun.Height * 0.5f),
                Scale,
                Effects,
                LayerDepth + 0.02f
            );

            // Draw turret
            _turret.Draw(
                spriteBatch,
                _gunPosition,
                Color,
                0,
                new Vector2(_turret.Width * 0.5f, _turret.Height * 0.5f),
                Scale,
                Effects,
                LayerDepth + 0.01f
            );

            // Draw body
            _body.Draw(spriteBatch, Position, Color, 0f, Origin, Scale, Effects, LayerDepth);

            // Draw recoil effect
            if (_isRecoiling)
            {
                TextureRegion recoilFrame = _recoil_animation.CurrentFrame;
                Vector2 gunTip = new Vector2(
                    _gunPosition.X + (float)Math.Cos(_gunRotation) * (_gunLength + 6.0f),
                    _gunPosition.Y + (float)Math.Sin(_gunRotation) * (_gunLength + 6.0f)
                );
                recoilFrame.Draw(
                    spriteBatch,
                    gunTip,
                    Color.White,
                    _gunRotation,
                    new Vector2(recoilFrame.Width * 0.5f, recoilFrame.Height * 0.5f),
                    Scale,
                    Effects,
                    LayerDepth + 0.05f
                );
            }

            // Draw collision bounds (debug)
            DrawBounds(spriteBatch);
        }

        private void DrawBounds(SpriteBatch spriteBatch)
        {
            Rectangle bounds = GetBound();
            Texture2D pixel = new Texture2D(spriteBatch.GraphicsDevice, 1, 1);
            pixel.SetData(new[] { Color.White });

            int thickness = 2;

            spriteBatch.Draw(
                pixel,
                new Rectangle(bounds.Left, bounds.Top, bounds.Width, thickness),
                Color.Green
            );
            spriteBatch.Draw(
                pixel,
                new Rectangle(bounds.Left, bounds.Bottom - thickness, bounds.Width, thickness),
                Color.Green
            );
            spriteBatch.Draw(
                pixel,
                new Rectangle(bounds.Left, bounds.Top, thickness, bounds.Height),
                Color.Green
            );
            spriteBatch.Draw(
                pixel,
                new Rectangle(bounds.Right - thickness, bounds.Top, thickness, bounds.Height),
                Color.Green
            );
        }

        public Rectangle GetBound()
        {
            return new Rectangle(
                (int)(Position.X - Origin.X),
                (int)(Position.Y - Origin.Y),
                (int)Width,
                (int)Height
            );
        }

        public string GetGroupName()
        {
            return "tank";
        }
    }
}
