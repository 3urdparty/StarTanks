using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using MonoGameLibrary.Graphics;
using MonoGameLibrary;
using Microsoft.Xna.Framework.Graphics;

namespace SpaceTanks
{
    public class Tank
    {
        private int _currentFrame;
        private TimeSpan _elapsed;
        private readonly ContentManager _content;
        private TextureRegion _body;
        private TextureRegion _turret;
        private TextureRegion _gun;
        private Animation _body_animation;

        private Vector2 _position;
        private float _velocityX;
        private float _velocityY;
        private int _moveDirection; // -1 for left, 0 for no input, 1 for right

        private const float MaxSpeed = 2f;
        private const float Acceleration = 0.05f;
        private const float Deceleration = 0.15f;
        private const float Gravity = 0.5f; // Gravity acceleration per frame
        private const float MaxFallSpeed = 10f; // Terminal velocity

        private bool _isOnGround;

        public Color Color { get; set; } = Color.White;
        public float GunRotation { get; set; } = 0.0f;
        public Vector2 Scale { get; set; } = new Vector2(1.5f, 1.5f);
        public Vector2 Origin { get; set; } = Vector2.Zero;
        public SpriteEffects Effects { get; set; } = SpriteEffects.None;
        public float LayerDepth { get; set; } = 0.0f;

        public float Width => _body.Width * Scale.X;
        public float Height => _body.Height * Scale.Y;
        public Vector2 Position => _position;
        public bool IsOnGround => _isOnGround;

        public Tank(ContentManager content)
        {
            _content = content;
            _position = new Vector2(100f, 100f);
            _velocityX = 0f;
            _velocityY = 0f;
            _moveDirection = 0;

            // Load animations from atlas
            TextureAtlas atlas = TextureAtlas.FromFile(_content, "tank-atlas.xml");
            _body_animation = atlas.GetAnimation("tank-green-moving");
            _body = atlas.GetRegion("tank-green-1");

            _turret = atlas.GetRegion("turret");
            _gun = atlas.GetRegion("gun");
        }

        // Centers the origin for proper rotation
        public void CenterOrigin()
        {
            Origin = new Vector2(_body.Width, _body.Height) * 0.5f;
        }

        // Set the tank's position
        public void SetPosition(Vector2 position)
        {
            _position = position;
        }

        // Move the tank left
        public void MoveLeft()
        {
            _moveDirection = -1;
        }

        // Move the tank right
        public void MoveRight()
        {
            _moveDirection = 1;
        }

        // Jump
        public void Jump()
        {
            if (_isOnGround)
            {
                _velocityY = -8f; // Jump strength
            }
        }

        // Rotate the tank gun left
        public void RotateGunLeft(float deltaTime)
        {
            GunRotation -= MathHelper.PiOver2 * deltaTime;
            GunRotation = MathHelper.Clamp(GunRotation, -MathHelper.PiOver2, MathHelper.PiOver2);
        }

        // Rotate the tank gun right
        public void RotateGunRight(float deltaTime)
        {
            GunRotation += MathHelper.PiOver2 * deltaTime;
            GunRotation = MathHelper.Clamp(GunRotation, -MathHelper.PiOver2, MathHelper.PiOver2);
        }

        // Draw the tank and its gun
        public void Draw(SpriteBatch spriteBatch)
        {
            // Draw tank body first
            _body.Draw(spriteBatch, _position, Color, 0f, Origin, Scale, Effects, LayerDepth);

            // Calculate turret/gun position
            Vector2 gunPosition = new Vector2(
                _position.X + (_body.Width * Scale.X * 0.5f) - Origin.X,
                _position.Y + (_body.Height * Scale.Y * 0.5f) - 5f * Scale.Y
            );

            // Draw turret
            _turret.Draw(
                spriteBatch,
                gunPosition,
                Color,
                GunRotation,
                new Vector2(_turret.Width * 0.5f, _turret.Height * 0.5f),
                Scale,
                Effects,
                LayerDepth + 0.01f
            );

            // Draw gun
            _gun.Draw(
                spriteBatch,
                gunPosition,
                Color,
                GunRotation - MathHelper.PiOver2,
                new Vector2(_gun.Width * 0.5f, _gun.Height * 0.5f),
                Scale,
                Effects,
                LayerDepth + 0.02f
            );
        }

        // Update the tank's animation and movement
        public void Update(GameTime gameTime, ProceduralWorld world)
        {
            float deltaTime = (float)gameTime.ElapsedGameTime.TotalSeconds;

            // Apply horizontal acceleration or deceleration based on input
            if (_moveDirection != 0)
            {
                // Accelerate in the direction of input
                _velocityX += _moveDirection * Acceleration;
            }
            else
            {
                // Decelerate when no input
                if (_velocityX > 0)
                {
                    _velocityX -= Deceleration;
                    if (_velocityX < 0) _velocityX = 0;
                }
                else if (_velocityX < 0)
                {
                    _velocityX += Deceleration;
                    if (_velocityX > 0) _velocityX = 0;
                }
            }

            // Clamp velocity to max speed
            _velocityX = MathHelper.Clamp(_velocityX, -MaxSpeed, MaxSpeed);

            // Apply gravity
            _velocityY += Gravity;
            _velocityY = Math.Min(_velocityY, MaxFallSpeed); // Cap fall speed

            // Update horizontal position
            _position.X += _velocityX;

            // Update vertical position
            _position.Y += _velocityY;

            // Handle collision with world
            HandleCollision(world);

            // Reset move direction for next frame (must be set each frame by input)
            _moveDirection = 0;

            // Update animation
            _elapsed += gameTime.ElapsedGameTime;

            if (_elapsed >= _body_animation.Delay)
            {
                _elapsed -= _body_animation.Delay;
                _currentFrame++;

                if (_currentFrame >= _body_animation.Frames.Count)
                {
                    _currentFrame = 0;
                }

                _body = _body_animation.Frames[_currentFrame];
            }
        }

        private void HandleCollision(ProceduralWorld world)
        {
            // Get tank bounds
            Rectangle tankBounds = new Rectangle(
                (int)(_position.X - Origin.X),
                (int)(_position.Y - Origin.Y),
                (int)Width,
                (int)Height
            );

            // Check tiles around tank
            Point topLeft = world.WorldToTile(new Vector2(tankBounds.Left, tankBounds.Top));
            Point bottomRight = world.WorldToTile(new Vector2(tankBounds.Right, tankBounds.Bottom));

            _isOnGround = false;

            for (int x = topLeft.X; x <= bottomRight.X; x++)
            {
                for (int y = topLeft.Y; y <= bottomRight.Y; y++)
                {
                    if (world.IsSolid(x, y))
                    {
                        Rectangle tileBounds = new Rectangle(
                            x * world.TileSize,
                            y * world.TileSize,
                            world.TileSize,
                            world.TileSize
                        );

                        if (tankBounds.Intersects(tileBounds))
                        {
                            // Resolve collision
                            ResolveCollision(tankBounds, tileBounds);
                        }
                    }
                }
            }
        }

        private void ResolveCollision(Rectangle tank, Rectangle tile)
        {
            // Calculate overlap on each axis
            int overlapLeft = tank.Right - tile.Left;
            int overlapRight = tile.Right - tank.Left;
            int overlapTop = tank.Bottom - tile.Top;
            int overlapBottom = tile.Bottom - tank.Top;

            // Find minimum overlap
            int minOverlap = Math.Min(
                Math.Min(overlapLeft, overlapRight),
                Math.Min(overlapTop, overlapBottom)
            );

            // Push tank out on axis with least overlap
            if (minOverlap == overlapTop && _velocityY > 0)
            {
                // Hit ground
                _position.Y = tile.Top - Height + Origin.Y;
                _velocityY = 0;
                _isOnGround = true;
            }
            else if (minOverlap == overlapBottom && _velocityY < 0)
            {
                // Hit ceiling
                _position.Y = tile.Bottom + Origin.Y;
                _velocityY = 0;
            }
            else if (minOverlap == overlapLeft)
            {
                // Hit from left
                _position.X = tile.Left - Width + Origin.X;
                _velocityX = 0;
            }
            else if (minOverlap == overlapRight)
            {
                // Hit from right
                _position.X = tile.Right + Origin.X;
                _velocityX = 0;
            }
        }
    }
}
