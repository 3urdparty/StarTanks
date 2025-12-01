using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using Microsoft.Xna.Framework.Content;
using MonoGameLibrary.Graphics;
using MonoGameLibrary;

namespace SpaceTanks
{
    public class Bullet
    {
        private readonly ContentManager _content;
        private Vector2 _position;
        private Vector2 _velocity;
        private TextureRegion _sprite;
        private Animation _animation;
        private bool _isActive;
        private float _rotation;

        public Vector2 Origin { get; set; } = Vector2.Zero;
        public Color Color { get; set; } = Color.White;
        public SpriteEffects Effects { get; set; } = SpriteEffects.None;
        public float LayerDepth { get; set; } = 0.0f;

        public Vector2 Position => _position;
        public bool IsActive => _isActive;
        public float Speed { get; set; }

        public Bullet(ContentManager content, Vector2 position, float rotation, float speed = 300f)
        {
            _content = content;

            TextureAtlas atlas = TextureAtlas.FromFile(_content, "atlas.xml");
            _sprite = atlas.GetRegion("bullet-3");
            Origin = new Vector2(_sprite.Width, _sprite.Height) * 0.5f;

            _animation = atlas.GetAnimation("bullet-anim");
            _animation.Loop = false;

            _position = position;
            _rotation = rotation;
            Speed = speed;
            _isActive = true;

            // Calculate velocity from rotation
            _velocity = new Vector2(
                (float)Math.Cos(rotation),
                (float)Math.Sin(rotation)
            ) * speed;
        }

        // New: Update with world for collision
        public void Update(float deltaTime, ProceduralWorld world)
        {
            if (!_isActive)
                return;

            // Move bullet
            _position += _velocity * deltaTime;


            // Update animation
            _animation.Update(deltaTime);

            // Handle collision with world
            if (world != null)
            {
                HandleCollision(world);
            }
        }

        // Optional: keep old signature (no collision) if you still use it anywhere
        public void Update(float deltaTime)
        {
            Update(deltaTime, null);
        }

        public void Draw(SpriteBatch spriteBatch)
        {
            if (!_isActive)
                return;

            TextureRegion currentFrame = _animation.CurrentFrame ?? _sprite;

            currentFrame.Draw(
                spriteBatch,
                _position,
                Color,
                _rotation,
                new Vector2(currentFrame.Width * 0.5f, currentFrame.Height * 0.5f),
                Vector2.One,
                SpriteEffects.None,
                LayerDepth + 0.1f
            );
        }

        public Rectangle GetBounds()
        {
            return new Rectangle(
                (int)(_position.X - Origin.X),
                (int)(_position.Y - Origin.Y),
                _sprite.Width,
                _sprite.Height
            );
        }

        public void Deactivate()
        {
            _isActive = false;
        }

        public bool IsOutOfBounds(int screenWidth, int screenHeight, int margin = 100)
        {
            return _position.X < -margin ||
                   _position.X > screenWidth + margin ||
                   _position.Y < -margin ||
                   _position.Y > screenHeight + margin;
        }

        // New: world collision, similar style to Tank.HandleCollision
        private void HandleCollision(ProceduralWorld world)
        {
            Rectangle bulletBounds = GetBounds();

            Point topLeft = world.WorldToTile(new Vector2(bulletBounds.Left, bulletBounds.Top));
            Point bottomRight = world.WorldToTile(new Vector2(bulletBounds.Right, bulletBounds.Bottom));

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

                        if (bulletBounds.Intersects(tileBounds))
                        {
                            // Bullet hits solid tile: kill it
                            _isActive = false;
                            return;
                        }
                    }
                }
            }
        }
    }
}
