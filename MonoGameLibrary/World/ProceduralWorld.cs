// using System;
// using System.Collections.Generic;
// using Microsoft.Xna.Framework;
// using Microsoft.Xna.Framework.Content;
// using Microsoft.Xna.Framework.Graphics;
// using MonoGameLibrary.Graphics;
//
// namespace MonoGameLibrary
// {
//     public class ProceduralWorld
//     {
//         private readonly ContentManager _content;
//         private TextureAtlas _atlas;
//
//         // Tile regions - 3x3 grid (00=top-left, 22=bottom-right)
//         private TextureRegion _tile00;
//         private TextureRegion _tile01;
//         private TextureRegion _tile02;
//         private TextureRegion _tile10;
//         private TextureRegion _tile11;
//         private TextureRegion _tile12;
//         private TextureRegion _tile20;
//         private TextureRegion _tile21;
//         private TextureRegion _tile22;
//
//         // Inner tiles for variation
//         private TextureRegion _inner00;
//         private TextureRegion _inner01;
//         private TextureRegion _inner10;
//         private TextureRegion _inner11;
//         private TextureRegion _innerAlt00;
//         private TextureRegion _innerAlt01;
//         private TextureRegion _innerAlt10;
//         private TextureRegion _innerAlt11;
//
//         // World data
//         private int[,] _tileMap;
//         private int _worldWidth;
//         private int _worldHeight;
//         private int _tileSize;
//
//         // Collision shape
//         private CollisionShape _collisionShape;
//
//         // Tile type constants
//         private const int Empty = 0;
//         private const int Tile00 = 1;
//         private const int Tile01 = 2;
//         private const int Tile02 = 3;
//         private const int Tile10 = 4;
//         private const int Tile11 = 5;
//         private const int Tile12 = 6;
//         private const int Tile20 = 7;
//         private const int Tile21 = 8;
//         private const int Tile22 = 9;
//         private const int Inner = 10;
//
//         public int TileSize => _tileSize;
//         public int WorldWidth => _worldWidth;
//         public int WorldHeight => _worldHeight;
//         private bool _debugDrawCollisions = false;
//         private Texture2D _debugPixel;
//
//         public bool DebugDrawCollisions
//         {
//             get => _debugDrawCollisions;
//             set => _debugDrawCollisions = value;
//         }
//
//         public ProceduralWorld(ContentManager content, int worldWidth, int worldHeight, int tileSize)
//         {
//             _content = content;
//             _worldWidth = worldWidth;
//             _worldHeight = worldHeight;
//             _tileSize = tileSize;
//             _tileMap = new int[worldWidth, worldHeight];
//             _collisionShape = new PolygonCollisionShape();
//
//             LoadTiles();
//         }
//
//         private void LoadTiles()
//         {
//             _atlas = TextureAtlas.FromFile(_content, "tilemap-atlas.xml");
//
//             _tile00 = _atlas.GetRegion("00");
//             _tile01 = _atlas.GetRegion("01");
//             _tile02 = _atlas.GetRegion("02");
//             _tile10 = _atlas.GetRegion("10");
//             _tile11 = _atlas.GetRegion("11");
//             _tile12 = _atlas.GetRegion("12");
//             _tile20 = _atlas.GetRegion("20");
//             _tile21 = _atlas.GetRegion("21");
//             _tile22 = _atlas.GetRegion("22");
//
//             _inner00 = _atlas.GetRegion("inner-00");
//             _inner01 = _atlas.GetRegion("inner-01");
//             _inner10 = _atlas.GetRegion("inner-10");
//             _inner11 = _atlas.GetRegion("inner-11");
//             _innerAlt00 = _atlas.GetRegion("inner-alt-00");
//             _innerAlt01 = _atlas.GetRegion("inner-alt-01");
//             _innerAlt10 = _atlas.GetRegion("inner-alt-10");
//             _innerAlt11 = _atlas.GetRegion("inner-alt-11");
//         }
//
//         public void Generate(int seed = 0)
//         {
//             Random random = seed == 0 ? new Random() : new Random(seed);
//
//             for (int x = 0; x < _worldWidth; x++)
//             {
//                 for (int y = 0; y < _worldHeight; y++)
//                 {
//                     _tileMap[x, y] = Empty;
//                 }
//             }
//
//             GeneratePlatform(0, _worldWidth, _worldHeight - 4, 4);
//
//             int platformCount = random.Next(5, 10);
//             for (int i = 0; i < platformCount; i++)
//             {
//                 int platformWidth = random.Next(4, 12);
//                 int platformHeight = random.Next(2, 4);
//                 int platformX = random.Next(0, _worldWidth - platformWidth);
//                 int platformY = random.Next(2, _worldHeight - 8);
//                 GeneratePlatform(platformX, platformWidth, platformY, platformHeight);
//             }
//
//             FillInnerTiles(random);
//             BuildCollisionShape();
//         }
//
//         private void BuildCollisionShape()
//         {
//             if (_collisionShape is PolygonCollisionShape polygonShape)
//             {
//                 polygonShape.Clear();
//
//                 for (int x = 0; x < _worldWidth; x++)
//                 {
//                     for (int y = 0; y < _worldHeight; y++)
//                     {
//                         if (_tileMap[x, y] == Empty)
//                             continue;
//
//                         float left = x * _tileSize;
//                         float right = (x + 1) * _tileSize;
//                         float top = y * _tileSize;
//                         float bottom = (y + 1) * _tileSize;
//
//                         // Left edge
//                         if (x == 0 || _tileMap[x - 1, y] == Empty)
//                         {
//                             polygonShape.AddEdge(
//                                 new Vector2(left, top),
//                                 new Vector2(left, bottom)
//                             );
//                         }
//
//                         // Right edge
//                         if (x == _worldWidth - 1 || _tileMap[x + 1, y] == Empty)
//                         {
//                             polygonShape.AddEdge(
//                                 new Vector2(right, top),
//                                 new Vector2(right, bottom)
//                             );
//                         }
//
//                         // Top edge
//                         if (y == 0 || _tileMap[x, y - 1] == Empty)
//                         {
//                             polygonShape.AddEdge(
//                                 new Vector2(left, top),
//                                 new Vector2(right, top)
//                             );
//                         }
//
//                         // Bottom edge
//                         if (y == _worldHeight - 1 || _tileMap[x, y + 1] == Empty)
//                         {
//                             polygonShape.AddEdge(
//                                 new Vector2(left, bottom),
//                                 new Vector2(right, bottom)
//                             );
//                         }
//                     }
//                 }
//             }
//         }
//
//         private void GeneratePlatform(int startX, int width, int startY, int height)
//         {
//             for (int x = startX; x < startX + width && x < _worldWidth; x++)
//             {
//                 for (int y = startY; y < startY + height && y < _worldHeight; y++)
//                 {
//                     bool isLeft = (x == startX);
//                     bool isRight = (x == startX + width - 1);
//                     bool isTop = (y == startY);
//                     bool isBottom = (y == startY + height - 1);
//
//                     if (isTop && isLeft)
//                         _tileMap[x, y] = Tile00;
//                     else if (isTop && isRight)
//                         _tileMap[x, y] = Tile02;
//                     else if (isBottom && isLeft)
//                         _tileMap[x, y] = Tile20;
//                     else if (isBottom && isRight)
//                         _tileMap[x, y] = Tile22;
//                     else if (isTop)
//                         _tileMap[x, y] = Tile01;
//                     else if (isBottom)
//                         _tileMap[x, y] = Tile21;
//                     else if (isLeft)
//                         _tileMap[x, y] = Tile10;
//                     else if (isRight)
//                         _tileMap[x, y] = Tile12;
//                     else
//                         _tileMap[x, y] = Inner;
//                 }
//             }
//         }
//
//         private void FillInnerTiles(Random random)
//         {
//             for (int x = 0; x < _worldWidth; x++)
//             {
//                 for (int y = 0; y < _worldHeight; y++)
//                 {
//                     if (_tileMap[x, y] == Inner)
//                     {
//                         _tileMap[x, y] = Tile11;
//                     }
//                 }
//             }
//         }
//
//         private bool IsEdgeTile(int x, int y)
//         {
//             bool hasEmptyNeighbor = false;
//
//             if (x > 0 && _tileMap[x - 1, y] == Empty) hasEmptyNeighbor = true;
//             if (x < _worldWidth - 1 && _tileMap[x + 1, y] == Empty) hasEmptyNeighbor = true;
//             if (y > 0 && _tileMap[x, y - 1] == Empty) hasEmptyNeighbor = true;
//             if (y < _worldHeight - 1 && _tileMap[x, y + 1] == Empty) hasEmptyNeighbor = true;
//
//             return hasEmptyNeighbor;
//         }
//
//         public bool IsSolid(int tileX, int tileY)
//         {
//             if (tileX < 0 || tileX >= _worldWidth || tileY < 0 || tileY >= _worldHeight)
//                 return false;
//
//             return _tileMap[tileX, tileY] != Empty;
//         }
//
//         public Point WorldToTile(Vector2 worldPosition)
//         {
//             return new Point((int)(worldPosition.X / _tileSize), (int)(worldPosition.Y / _tileSize));
//         }
//
//         /// <summary>
//         /// Get collision bounds from the collision shape.
//         /// </summary>
//         public List<Rectangle> GetCollisionBounds()
//         {
//             return _collisionShape.GetBounds();
//         }
//
//         public void Draw(SpriteBatch spriteBatch, Rectangle cameraBounds)
//         {
//             int startX = Math.Max(0, cameraBounds.Left / _tileSize);
//             int endX = Math.Min(_worldWidth, (cameraBounds.Right / _tileSize) + 1);
//             int startY = Math.Max(0, cameraBounds.Top / _tileSize);
//             int endY = Math.Min(_worldHeight, (cameraBounds.Bottom / _tileSize) + 1);
//
//             for (int x = startX; x < endX; x++)
//             {
//                 for (int y = startY; y < endY; y++)
//                 {
//                     int tileType = _tileMap[x, y];
//                     if (tileType == Empty) continue;
//
//                     TextureRegion region = GetTileRegion(tileType);
//                     Vector2 position = new Vector2(x * _tileSize, y * _tileSize);
//
//                     region.Draw(
//                         spriteBatch,
//                         position,
//                         Color.White,
//                         0f,
//                         Vector2.Zero,
//                         new Vector2((float)_tileSize / region.Width, (float)_tileSize / region.Height),
//                         SpriteEffects.None,
//                         0f
//                     );
//                 }
//             }
//
//             if (_debugDrawCollisions)
//             {
//                 DrawCollisionDebug(spriteBatch, cameraBounds);
//             }
//         }
//
//         private void DrawCollisionDebug(SpriteBatch spriteBatch, Rectangle cameraBounds)
//         {
//             if (_debugPixel == null)
//             {
//                 var gfxService = (IGraphicsDeviceService)_content.ServiceProvider.GetService(typeof(IGraphicsDeviceService));
//                 var graphicsDevice = gfxService.GraphicsDevice;
//                 _debugPixel = new Texture2D(graphicsDevice, 1, 1);
//                 _debugPixel.SetData(new[] { Color.White });
//             }
//
//             // Draw solid tile rectangles in transparent red
//             int startX = Math.Max(0, cameraBounds.Left / _tileSize);
//             int endX = Math.Min(_worldWidth, (cameraBounds.Right / _tileSize) + 1);
//             int startY = Math.Max(0, cameraBounds.Top / _tileSize);
//             int endY = Math.Min(_worldHeight, (cameraBounds.Bottom / _tileSize) + 1);
//
//             Color fillColor = Color.Red * 0.25f;
//             for (int x = startX; x < endX; x++)
//             {
//                 for (int y = startY; y < endY; y++)
//                 {
//                     if (_tileMap[x, y] == Empty)
//                         continue;
//
//                     Rectangle tileBounds = new Rectangle(
//                         x * _tileSize,
//                         y * _tileSize,
//                         _tileSize,
//                         _tileSize
//                     );
//                     spriteBatch.Draw(_debugPixel, tileBounds, fillColor);
//                 }
//             }
//
//             // Draw collision shape using the abstract interface
//             Color edgeColor = Color.Red * 0.7f;
//             int thickness = 2;
//             // _collisionShape.Draw(spriteBatch, _debugPixel, edgeColor, thickness);
//         }
//
//         private TextureRegion GetTileRegion(int tileType)
//         {
//             return tileType switch
//             {
//                 Tile00 => _tile00,
//                 Tile01 => _tile01,
//                 Tile02 => _tile02,
//                 Tile10 => _tile10,
//                 Tile11 => _tile11,
//                 Tile12 => _tile12,
//                 Tile20 => _tile20,
//                 Tile21 => _tile21,
//                 Tile22 => _tile22,
//                 Inner => _tile11,
//                 _ => _tile11
//             };
//         }
//     }
// }
