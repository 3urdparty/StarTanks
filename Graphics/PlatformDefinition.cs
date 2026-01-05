using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;

namespace SpaceTanks
{
    public sealed class PlatformDefinition
    {
        public int Width { get; }
        public int Height { get; }
        public List<Vector2> Polygon { get; }
        public int[,] TileIds { get; }
        public List<Vector2> Surface { get; } 

        public PlatformDefinition(
            int width,
            int height,
            List<Vector2> polygon,
            int[,] tileIds,
            List<Vector2> surface
        )
        {
            Width = width;
            Height = height;
            Polygon = polygon ?? throw new ArgumentNullException(nameof(polygon));
            TileIds = tileIds ?? throw new ArgumentNullException(nameof(tileIds));
            Surface = surface ?? throw new ArgumentNullException(nameof(surface));
        }
    }
}
