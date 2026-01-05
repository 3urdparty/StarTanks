using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;

namespace SpaceTanks
{
    public sealed class PlatformGenerator
    {
        public sealed class Queries
        {
            public Func<int, int, bool> IsSolid { get; init; }
            public Func<int, int, int> DepthFromTop { get; init; }
        }

        public static int[,] Generate(TileMap ruleTiles, int cols, int rows, Queries q, int seed)
        {
            if (ruleTiles == null)
                throw new ArgumentNullException(nameof(ruleTiles));
            if (q == null)
                throw new ArgumentNullException(nameof(q));
            if (q.IsSolid == null)
                throw new ArgumentNullException(nameof(q.IsSolid));
            if (q.DepthFromTop == null)
                throw new ArgumentNullException(nameof(q.DepthFromTop));

            var ids = new int[rows, cols];
            int fallbackId = ruleTiles.TilesById.Count > 0 ? ruleTiles.TilesById.Keys.First() : 0;
            var rng = new Random(seed);

            int topLeftId = ResolveTileId(
                ruleTiles,
                rng,
                TileTag.TopSurface | TileTag.Left,
                fallbackId
            );
            int topCenterId = ResolveTileId(
                ruleTiles,
                rng,
                TileTag.TopSurface | TileTag.Center,
                fallbackId
            );
            int topRightId = ResolveTileId(
                ruleTiles,
                rng,
                TileTag.TopSurface | TileTag.Right,
                fallbackId
            );
            int topCornerLeftId = ResolveTileId(
                ruleTiles,
                rng,
                TileTag.TopSurface | TileTag.CornerTL,
                topLeftId
            );
            int topCornerRightId = ResolveTileId(
                ruleTiles,
                rng,
                TileTag.TopSurface | TileTag.CornerTR,
                topRightId
            );
            int middleLeftId = ResolveTileId(
                ruleTiles,
                rng,
                TileTag.MiddleFill | TileTag.Left,
                fallbackId
            );
            int middleCenterId = ResolveTileId(
                ruleTiles,
                rng,
                TileTag.MiddleFill | TileTag.Center,
                fallbackId
            );
            int middleRightId = ResolveTileId(
                ruleTiles,
                rng,
                TileTag.MiddleFill | TileTag.Right,
                fallbackId
            );
            int bottomLeftId = ResolveTileId(
                ruleTiles,
                rng,
                TileTag.BottomCap | TileTag.Left,
                fallbackId
            );
            int bottomCenterId = ResolveTileId(
                ruleTiles,
                rng,
                TileTag.BottomCap | TileTag.Center,
                fallbackId
            );
            int bottomRightId = ResolveTileId(
                ruleTiles,
                rng,
                TileTag.BottomCap | TileTag.Right,
                fallbackId
            );

            for (int r = 0; r < rows; r++)
            {
                for (int c = 0; c < cols; c++)
                {
                    if (!q.IsSolid(c, r))
                    {
                        ids[r, c] = -1;
                        continue;
                    }

                    bool isTop = q.DepthFromTop(c, r) == 0;
                    bool leftAir = c == 0 || !q.IsSolid(c - 1, r);
                    bool rightAir = c == cols - 1 || !q.IsSolid(c + 1, r);
                    bool belowAir = r == rows - 1 || !q.IsSolid(c, r + 1);

                    if (isTop)
                    {
                        if (c == 0)
                            ids[r, c] = topCornerLeftId;
                        else if (c == cols - 1)
                            ids[r, c] = topCornerRightId;
                        else if (leftAir && !rightAir)
                            ids[r, c] = topLeftId;
                        else if (rightAir && !leftAir)
                            ids[r, c] = topRightId;
                        else
                            ids[r, c] = topCenterId;
                        continue;
                    }

                    if (belowAir)
                    {
                        if (leftAir && !rightAir)
                            ids[r, c] = bottomLeftId;
                        else if (rightAir && !leftAir)
                            ids[r, c] = bottomRightId;
                        else
                            ids[r, c] = bottomCenterId;
                        continue;
                    }

                    if (leftAir && !rightAir)
                        ids[r, c] = middleLeftId;
                    else if (rightAir && !leftAir)
                        ids[r, c] = middleRightId;
                    else
                        ids[r, c] = middleCenterId;
                }
            }

            return ids;
        }

        public sealed class Options
        {
            public PlatformTopology Topology = PlatformTopology.Flat;

            public float SurfaceStepPx = 10f;

            public float FlatTopY;

            public int RectFeatureCount = 8;
            public float RectMinWidthPx = 80f;
            public float RectMaxWidthPx = 240f;
            public float RectMinDeltaY = -80f;
            public float RectMaxDeltaY = +80f;

            public float SurfaceCeilingY = -1f;

            public int Seed = 1337;
        }

        private readonly TileMap _ruleTiles;

        public PlatformGenerator(TileMap ruleTiles)
        {
            _ruleTiles = ruleTiles;
        }

        public Platform CreatePlatform(Options opt, int width, int height)
        {
            var def = CreateDefinition(opt, width, height);
            return new Platform(def, _ruleTiles);
        }

        public PlatformDefinition CreateDefinition(Options opt, int width, int height)
        {
            if (opt.FlatTopY == 0f)
                opt.FlatTopY = -height;

            var surface = BuildSurface(opt, width, height);

            var poly = BuildPolygonFromSurface(width, height, surface);

            int[,] tileIds = BuildTileIdsFromPolygon(width, height, poly, opt.Seed);

            return new PlatformDefinition(width, height, poly, tileIds, surface);
        }

        private List<Vector2> BuildSurface(Options opt, int width, int height)
        {
            var rng = new Random(opt.Seed);

            int count = (int)Math.Ceiling(width / opt.SurfaceStepPx);
            var surface = new List<Vector2>(count + 1);

            for (float x = 0; x <= width; x += opt.SurfaceStepPx)
                surface.Add(new Vector2(x, opt.FlatTopY));

            if (opt.Topology == PlatformTopology.Flat)
                return surface;

            for (int i = 0; i < opt.RectFeatureCount; i++)
            {
                float featureW = Lerp(
                    opt.RectMinWidthPx,
                    opt.RectMaxWidthPx,
                    (float)rng.NextDouble()
                );
                float left = Lerp(0f, width - featureW, (float)rng.NextDouble());
                float right = left + featureW;

                float dy = Lerp(opt.RectMinDeltaY, opt.RectMaxDeltaY, (float)rng.NextDouble());

                for (int s = 0; s < surface.Count; s++)
                {
                    float x = surface[s].X;
                    if (x < left || x > right)
                        continue;

                    float newY = surface[s].Y + dy;

                    newY = MathF.Min(newY, opt.SurfaceCeilingY);

                    newY = MathF.Max(newY, -height);

                    surface[s] = new Vector2(x, newY);
                }
            }

            return surface;
        }

        private static List<Vector2> BuildPolygonFromSurface(
            int width,
            int height,
            List<Vector2> surface
        )
        {
            var poly = new List<Vector2>(surface.Count + 3);

            poly.Add(new Vector2(0, 0));
            poly.Add(new Vector2(width, 0));

            for (int i = surface.Count - 1; i >= 0; i--)
                poly.Add(surface[i]);

            if (surface.Count > 0 && surface[0].X != 0f)
                poly.Add(new Vector2(0f, surface[0].Y));

            return poly;
        }

        private int[,] BuildTileIdsFromPolygon(int width, int height, List<Vector2> poly, int seed)
        {
            int tileW = _ruleTiles.TileWidth;
            int tileH = _ruleTiles.TileHeight;

            int cols = (int)Math.Ceiling((float)width / tileW);
            int rows = (int)Math.Ceiling((float)height / tileH);

            Func<int, int, bool> isSolid = (c, r) =>
            {
                float px = c * tileW + tileW * 0.5f;
                float py = r * tileH + tileH * 0.5f;

                Vector2 local = new Vector2(px, py - height);
                return PointInPolygon(poly, local);
            };

            Func<int, int, int> depthFromTop = (c, r) =>
            {
                int d = 0;
                for (int y = r; y >= 0; y--)
                {
                    if (!isSolid(c, y))
                        break;
                    d++;
                }
                return Math.Max(0, d - 1);
            };

            var queries = new PlatformGenerator.Queries
            {
                IsSolid = isSolid,
                DepthFromTop = depthFromTop,
            };

            return PlatformGenerator.Generate(_ruleTiles, cols, rows, queries, seed: seed);
        }

        private static bool PointInPolygon(IReadOnlyList<Vector2> poly, Vector2 p)
        {
            bool inside = false;
            for (int i = 0, j = poly.Count - 1; i < poly.Count; j = i++)
            {
                Vector2 a = poly[i];
                Vector2 b = poly[j];

                bool intersect =
                    ((a.Y > p.Y) != (b.Y > p.Y))
                    && (p.X < (b.X - a.X) * (p.Y - a.Y) / ((b.Y - a.Y) + 1e-6f) + a.X);

                if (intersect)
                    inside = !inside;
            }
            return inside;
        }

        private static int ResolveTileId(TileMap map, Random rng, TileTag tags, int fallback)
        {
            var candidates = map.TilesById.Values.Where(t => (t.Tags & tags) == tags).ToList();
            if (candidates.Count == 0)
                return fallback;
            if (candidates.Count == 1)
                return candidates[0].Id;

            float totalWeight = candidates.Sum(t => t.Weight);
            float roll = (float)rng.NextDouble() * totalWeight;
            foreach (var tile in candidates)
            {
                roll -= tile.Weight;
                if (roll <= 0f)
                    return tile.Id;
            }
            return candidates[^1].Id;
        }

        private static float Lerp(float a, float b, float t) => a + (b - a) * t;
    }
}
