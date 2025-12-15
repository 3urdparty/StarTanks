using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using MonoGameLibrary;
using MonoGameLibrary.Graphics;

namespace SpaceTanks
{
    public class Platform : GameObject, ICollidable
    {
        private readonly ContentManager _content;
        private TextureAtlas _atlas;

        // Tile regions - 3x3 grid
        private TextureRegion _tile00;
        private TextureRegion _tile01;
        private TextureRegion _tile02;
        private TextureRegion _tile10;
        private TextureRegion _tile11;
        private TextureRegion _tile12;
        private TextureRegion _tile20;
        private TextureRegion _tile21;
        private TextureRegion _tile22;

        // Platform tile data
        private int[,] _platformTiles;
        private int _platformWidth;
        private int _platformHeight;
        private int _tileSize = 32;

        // Tile type constants
        private const int Empty = 0;
        private const int Tile00 = 1;
        private const int Tile01 = 2;
        private const int Tile02 = 3;
        private const int Tile10 = 4;
        private const int Tile11 = 5;
        private const int Tile12 = 6;
        private const int Tile20 = 7;
        private const int Tile21 = 8;
        private const int Tile22 = 9;

        private Texture2D _debugPixel;

        public Platform(ContentManager content, GraphicsDevice graphicsDevice)
        {
            _content = content;
            LoadTiles();

            _debugPixel = new Texture2D(graphicsDevice, 1, 1);
            _debugPixel.SetData(new[] { Color.White });
        }

        private void LoadTiles()
        {
            _atlas = TextureAtlas.FromFile(_content, "tilemap-atlas.xml");

            _tile00 = _atlas.GetRegion("00");
            _tile01 = _atlas.GetRegion("01");
            _tile02 = _atlas.GetRegion("02");
            _tile10 = _atlas.GetRegion("10");
            _tile11 = _atlas.GetRegion("11");
            _tile12 = _atlas.GetRegion("12");
            _tile20 = _atlas.GetRegion("20");
            _tile21 = _atlas.GetRegion("21");
            _tile22 = _atlas.GetRegion("22");
        }

        /// <summary>
        /// Create a rectangular platform at the given position with the specified dimensions.
        /// </summary>
        public void CreatePlatform(
            Vector2 position,
            int platformWidth,
            int platformHeight,
            int tileSize = 32
        )
        {
            Position = position;
            _tileSize = tileSize;
            _platformWidth = platformWidth;
            _platformHeight = platformHeight;

            // Create tile map for the platform
            _platformTiles = new int[platformWidth, platformHeight];

            for (int x = 0; x < platformWidth; x++)
            {
                for (int y = 0; y < platformHeight; y++)
                {
                    bool isLeft = (x == 0);
                    bool isRight = (x == platformWidth - 1);
                    bool isTop = (y == 0);
                    bool isBottom = (y == platformHeight - 1);

                    if (isTop && isLeft)
                        _platformTiles[x, y] = Tile00;
                    else if (isTop && isRight)
                        _platformTiles[x, y] = Tile02;
                    else if (isBottom && isLeft)
                        _platformTiles[x, y] = Tile20;
                    else if (isBottom && isRight)
                        _platformTiles[x, y] = Tile22;
                    else if (isTop)
                        _platformTiles[x, y] = Tile01;
                    else if (isBottom)
                        _platformTiles[x, y] = Tile21;
                    else if (isLeft)
                        _platformTiles[x, y] = Tile10;
                    else if (isRight)
                        _platformTiles[x, y] = Tile12;
                    else
                        _platformTiles[x, y] = Tile11;
                }
            }

            // Set width and height based on tile dimensions
            Width = platformWidth * tileSize;
            Height = platformHeight * tileSize;
            Origin = Vector2.Zero;
        }

        public override void Update(GameTime gameTime)
        {
            // Platform is static
        }

        public override void Draw(SpriteBatch spriteBatch)
        {
            if (_platformTiles == null)
                return;

            for (int x = 0; x < _platformWidth; x++)
            {
                for (int y = 0; y < _platformHeight; y++)
                {
                    int tileType = _platformTiles[x, y];
                    if (tileType == Empty)
                        continue;

                    TextureRegion region = GetTileRegion(tileType);
                    Vector2 tilePosition = Position + new Vector2(x * _tileSize, y * _tileSize);

                    region.Draw(
                        spriteBatch,
                        tilePosition,
                        Color.White,
                        0f,
                        Vector2.Zero,
                        new Vector2(
                            (float)_tileSize / region.Width,
                            (float)_tileSize / region.Height
                        ),
                        SpriteEffects.None,
                        0.0f
                    );
                }
            }

            // Draw collision bounds (debug)
            DrawBounds(spriteBatch);
        }

        private void DrawBounds(SpriteBatch spriteBatch)
        {
            Rectangle bounds = GetBound();

            int thickness = 2;

            spriteBatch.Draw(
                _debugPixel,
                new Rectangle(bounds.Left, bounds.Top, bounds.Width, thickness),
                Color.Yellow
            );
            spriteBatch.Draw(
                _debugPixel,
                new Rectangle(bounds.Left, bounds.Bottom - thickness, bounds.Width, thickness),
                Color.Yellow
            );
            spriteBatch.Draw(
                _debugPixel,
                new Rectangle(bounds.Left, bounds.Top, thickness, bounds.Height),
                Color.Yellow
            );
            spriteBatch.Draw(
                _debugPixel,
                new Rectangle(bounds.Right - thickness, bounds.Top, thickness, bounds.Height),
                Color.Yellow
            );
        }

        public Rectangle GetBound()
        {
            return new Rectangle((int)Position.X, (int)Position.Y, (int)Width, (int)Height);
        }

        public string GetGroupName()
        {
            return "platform";
        }

        private TextureRegion GetTileRegion(int tileType)
        {
            return tileType switch
            {
                Tile00 => _tile00,
                Tile01 => _tile01,
                Tile02 => _tile02,
                Tile10 => _tile10,
                Tile11 => _tile11,
                Tile12 => _tile12,
                Tile20 => _tile20,
                Tile21 => _tile21,
                Tile22 => _tile22,
                _ => _tile11,
            };
        }
    }
}
