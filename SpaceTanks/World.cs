using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using MonoGameLibrary.Graphics;

namespace SpaceTanks
{
    public class ProceduralWorld
    {
        private readonly ContentManager _content;
        private TextureAtlas _atlas;
        
        // Tile regions - 3x3 grid (00=top-left, 22=bottom-right)
        private TextureRegion _tile00; // top-left
        private TextureRegion _tile01; // top-mid
        private TextureRegion _tile02; // top-right
        private TextureRegion _tile10; // mid-left
        private TextureRegion _tile11; // mid-mid
        private TextureRegion _tile12; // mid-right
        private TextureRegion _tile20; // bottom-left
        private TextureRegion _tile21; // bottom-mid
        private TextureRegion _tile22; // bottom-right
        
        // Inner tiles for variation
        private TextureRegion _inner00;
        private TextureRegion _inner01;
        private TextureRegion _inner10;
        private TextureRegion _inner11;
        private TextureRegion _innerAlt00;
        private TextureRegion _innerAlt01;
        private TextureRegion _innerAlt10;
        private TextureRegion _innerAlt11;
        
        // World data
        private int[,] _tileMap;
        private int _worldWidth;
        private int _worldHeight;
        private int _tileSize;
        
        // Tile type constants
        private const int Empty = 0;
        private const int Tile00 = 1;  // top-left
        private const int Tile01 = 2;  // top-mid
        private const int Tile02 = 3;  // top-right
        private const int Tile10 = 4;  // mid-left
        private const int Tile11 = 5;  // mid-mid
        private const int Tile12 = 6;  // mid-right
        private const int Tile20 = 7;  // bottom-left
        private const int Tile21 = 8;  // bottom-mid
        private const int Tile22 = 9;  // bottom-right
        private const int Inner = 10;  // inner tiles (will be randomized)
        
        public int TileSize => _tileSize;
        public int WorldWidth => _worldWidth;
        public int WorldHeight => _worldHeight;

private bool _debugDrawCollisions = false;
private Texture2D _debugPixel;

// Collision outline as line segments
private struct LineSegment
{
    public Vector2 Start;
    public Vector2 End;

    public LineSegment(Vector2 start, Vector2 end)
    {
        Start = start;
        End = end;
    }
}

private readonly List<LineSegment> _collisionEdges = new List<LineSegment>();

public bool DebugDrawCollisions
{
    get => _debugDrawCollisions;
    set => _debugDrawCollisions = value;
}
        

        public ProceduralWorld(ContentManager content, int worldWidth, int worldHeight, int tileSize)
        {
            _content = content;
            _worldWidth = worldWidth;
            _worldHeight = worldHeight;
            _tileSize = tileSize;
            _tileMap = new int[worldWidth, worldHeight];
            
            LoadTiles();
        }
        
        private void LoadTiles()
        {
            _atlas = TextureAtlas.FromFile(_content, "tilemap-atlas.xml");
            
            // Load 3x3 grid tiles
            _tile00 = _atlas.GetRegion("00"); // top-left
            _tile01 = _atlas.GetRegion("01"); // top-mid
            _tile02 = _atlas.GetRegion("02"); // top-right
            _tile10 = _atlas.GetRegion("10"); // mid-left
            _tile11 = _atlas.GetRegion("11"); // mid-mid
            _tile12 = _atlas.GetRegion("12"); // mid-right
            _tile20 = _atlas.GetRegion("20"); // bottom-left
            _tile21 = _atlas.GetRegion("21"); // bottom-mid
            _tile22 = _atlas.GetRegion("22"); // bottom-right
            
            // Load inner tiles for variation
            _inner00 = _atlas.GetRegion("inner-00");
            _inner01 = _atlas.GetRegion("inner-01");
            _inner10 = _atlas.GetRegion("inner-10");
            _inner11 = _atlas.GetRegion("inner-11");
            _innerAlt00 = _atlas.GetRegion("inner-alt-00");
            _innerAlt01 = _atlas.GetRegion("inner-alt-01");
            _innerAlt10 = _atlas.GetRegion("inner-alt-10");
            _innerAlt11 = _atlas.GetRegion("inner-alt-11");
        }
        
public void Generate(int seed = 0)
{
    Random random = seed == 0 ? new Random() : new Random(seed);

    // Clear the map
    for (int x = 0; x < _worldWidth; x++)
    {
        for (int y = 0; y < _worldHeight; y++)
        {
            _tileMap[x, y] = Empty;
        }
    }

    // Generate ground platform at bottom
    GeneratePlatform(0, _worldWidth, _worldHeight - 4, 4);

    // Generate random floating platforms
    int platformCount = random.Next(5, 10);
    for (int i = 0; i < platformCount; i++)
    {
        int platformWidth = random.Next(4, 12);
        int platformHeight = random.Next(2, 4);
        int platformX = random.Next(0, _worldWidth - platformWidth);
        int platformY = random.Next(2, _worldHeight - 8);

        GeneratePlatform(platformX, platformWidth, platformY, platformHeight);
    }

    // Fill inner tiles
    FillInnerTiles(random);

    // Build collision edge outline
    BuildCollisionEdges();
}

private void BuildCollisionEdges()
{
    _collisionEdges.Clear();

    for (int x = 0; x < _worldWidth; x++)
    {
        for (int y = 0; y < _worldHeight; y++)
        {
            if (_tileMap[x, y] == Empty)
                continue;

            // World coordinates of this tile
            float left   = x * _tileSize;
            float right  = (x + 1) * _tileSize;
            float top    = y * _tileSize;
            float bottom = (y + 1) * _tileSize;

            // Left edge: neighbor empty?
            if (x == 0 || _tileMap[x - 1, y] == Empty)
            {
                _collisionEdges.Add(new LineSegment(
                    new Vector2(left, top),
                    new Vector2(left, bottom)
                ));
            }

            // Right edge
            if (x == _worldWidth - 1 || _tileMap[x + 1, y] == Empty)
            {
                _collisionEdges.Add(new LineSegment(
                    new Vector2(right, top),
                    new Vector2(right, bottom)
                ));
            }

            // Top edge
            if (y == 0 || _tileMap[x, y - 1] == Empty)
            {
                _collisionEdges.Add(new LineSegment(
                    new Vector2(left, top),
                    new Vector2(right, top)
                ));
            }

            // Bottom edge
            if (y == _worldHeight - 1 || _tileMap[x, y + 1] == Empty)
            {
                _collisionEdges.Add(new LineSegment(
                    new Vector2(left, bottom),
                    new Vector2(right, bottom)
                ));
            }
        }
    }
}
        
        // Generate a single platform with proper corner tiles
        private void GeneratePlatform(int startX, int width, int startY, int height)
        {
            for (int x = startX; x < startX + width && x < _worldWidth; x++)
            {
                for (int y = startY; y < startY + height && y < _worldHeight; y++)
                {
                    bool isLeft = (x == startX);
                    bool isRight = (x == startX + width - 1);
                    bool isTop = (y == startY);
                    bool isBottom = (y == startY + height - 1);
                    
                    // Corner tiles
                    if (isTop && isLeft)
                        _tileMap[x, y] = Tile00; // top-left
                    else if (isTop && isRight)
                        _tileMap[x, y] = Tile02; // top-right
                    else if (isBottom && isLeft)
                        _tileMap[x, y] = Tile20; // bottom-left
                    else if (isBottom && isRight)
                        _tileMap[x, y] = Tile22; // bottom-right
                    // Edge tiles
                    else if (isTop)
                        _tileMap[x, y] = Tile01; // top-mid
                    else if (isBottom)
                        _tileMap[x, y] = Tile21; // bottom-mid
                    else if (isLeft)
                        _tileMap[x, y] = Tile10; // mid-left
                    else if (isRight)
                        _tileMap[x, y] = Tile12; // mid-right
                    // Inner tiles
                    else
                        _tileMap[x, y] = Inner; // will be randomized later
                }
            }
        }
        
        // Fill inner tiles with random variation
        private void FillInnerTiles(Random random)
        {
            for (int x = 0; x < _worldWidth; x++)
            {
                for (int y = 0; y < _worldHeight; y++)
                {
                    // Only randomize inner tiles
                    if (_tileMap[x, y] == Inner)
                    {
                        // Use mid-mid tile or keep as inner tile marker
                        _tileMap[x, y] = Tile11; // mid-mid (center tile)
                    }
                }
            }
        }
        
        // Check if a tile is on the edge of a platform
        private bool IsEdgeTile(int x, int y)
        {
            // Check if any adjacent tile is empty
            bool hasEmptyNeighbor = false;
            
            if (x > 0 && _tileMap[x - 1, y] == Empty) hasEmptyNeighbor = true;
            if (x < _worldWidth - 1 && _tileMap[x + 1, y] == Empty) hasEmptyNeighbor = true;
            if (y > 0 && _tileMap[x, y - 1] == Empty) hasEmptyNeighbor = true;
            if (y < _worldHeight - 1 && _tileMap[x, y + 1] == Empty) hasEmptyNeighbor = true;
            
            return hasEmptyNeighbor;
        }
        
        // Check if a tile position is solid (for collision detection)
        public bool IsSolid(int tileX, int tileY)
        {
            if (tileX < 0 || tileX >= _worldWidth || tileY < 0 || tileY >= _worldHeight)
                return false;
                
            return _tileMap[tileX, tileY] != Empty;
        }
        
        // Convert world position to tile coordinates
        public Point WorldToTile(Vector2 worldPosition)
        {
            return new Point((int)(worldPosition.X / _tileSize), (int)(worldPosition.Y / _tileSize));
        }
        
        // Draw the world
        public void Draw(SpriteBatch spriteBatch, Rectangle cameraBounds)
        {
            // Calculate visible tile range
            int startX = Math.Max(0, cameraBounds.Left / _tileSize);
            int endX = Math.Min(_worldWidth, (cameraBounds.Right / _tileSize) + 1);
            int startY = Math.Max(0, cameraBounds.Top / _tileSize);
            int endY = Math.Min(_worldHeight, (cameraBounds.Bottom / _tileSize) + 1);

// Existing tile drawing loop...
for (int x = startX; x < endX; x++)
{
    for (int y = startY; y < endY; y++)
    {
        int tileType = _tileMap[x, y];
        if (tileType == Empty) continue;
        
        TextureRegion region = GetTileRegion(tileType);
        Vector2 position = new Vector2(x * _tileSize, y * _tileSize);
        
        region.Draw(
            spriteBatch,
            position,
            Color.White,
            0f,
            Vector2.Zero,
            new Vector2((float)_tileSize / region.Width, (float)_tileSize / region.Height),
            SpriteEffects.None,
            0f
        );
    }
}

// Debug overlay
if (_debugDrawCollisions)
{
    DrawCollisionDebug(spriteBatch, cameraBounds);
}

        }
        

private void DrawCollisionDebug(SpriteBatch spriteBatch, Rectangle cameraBounds)
{
    if (_debugPixel == null)
    {
        // Get GraphicsDevice from ContentManager's service provider
        var gfxService = (IGraphicsDeviceService)_content.ServiceProvider.GetService(typeof(IGraphicsDeviceService));
        var graphicsDevice = gfxService.GraphicsDevice;

        _debugPixel = new Texture2D(graphicsDevice, 1, 1);
        _debugPixel.SetData(new[] { Color.White });
    }

    // 1) Draw solid tile rectangles in transparent red
    int startX = Math.Max(0, cameraBounds.Left / _tileSize);
    int endX   = Math.Min(_worldWidth, (cameraBounds.Right / _tileSize) + 1);
    int startY = Math.Max(0, cameraBounds.Top / _tileSize);
    int endY   = Math.Min(_worldHeight, (cameraBounds.Bottom / _tileSize) + 1);

    Color fillColor = Color.Red * 0.25f;
    int thickness = 2;

    for (int x = startX; x < endX; x++)
    {
        for (int y = startY; y < endY; y++)
        {
            if (_tileMap[x, y] == Empty)
                continue;

            Rectangle tileBounds = new Rectangle(
                x * _tileSize,
                y * _tileSize,
                _tileSize,
                _tileSize
            );

            spriteBatch.Draw(
                _debugPixel,
                tileBounds,
                fillColor
            );
        }
    }

    // 2) Draw collision edges as slightly stronger red lines
    Color edgeColor = Color.Red * 0.7f;

    foreach (var edge in _collisionEdges)
    {
        // Simple culling: skip edges far outside the camera
        if (edge.End.X < cameraBounds.Left - _tileSize ||
            edge.Start.X > cameraBounds.Right + _tileSize ||
            edge.End.Y < cameraBounds.Top - _tileSize ||
            edge.Start.Y > cameraBounds.Bottom + _tileSize)
        {
            continue;
        }

        // Axis-aligned edges only (our BuildCollisionEdges only makes those)
        if (Math.Abs(edge.Start.X - edge.End.X) < 0.1f)
        {
            // Vertical line
            float x = edge.Start.X;
            float y = Math.Min(edge.Start.Y, edge.End.Y);
            float length = Math.Abs(edge.End.Y - edge.Start.Y);

            Rectangle rect = new Rectangle(
                (int)x,
                (int)y,
                thickness,
                (int)length
            );

            spriteBatch.Draw(_debugPixel, rect, edgeColor);
        }
        else if (Math.Abs(edge.Start.Y - edge.End.Y) < 0.1f)
        {
            // Horizontal line
            float y = edge.Start.Y;
            float x = Math.Min(edge.Start.X, edge.End.X);
            float length = Math.Abs(edge.End.X - edge.Start.X);

            Rectangle rect = new Rectangle(
                (int)x,
                (int)y,
                (int)length,
                thickness
            );

            spriteBatch.Draw(_debugPixel, rect, edgeColor);
        }
    }
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
                Inner => _tile11, // Use mid-mid as default inner
                _ => _tile11
            };
        }
    }
}
