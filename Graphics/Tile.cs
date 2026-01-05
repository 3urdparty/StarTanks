using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Xml;
using System.Xml.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;

namespace SpaceTanks
{
    public sealed class Tile
    {
        public int Id { get; }
        public string AtlasKey { get; }
        public TextureRegion Region { get; }
        public TileTag Tags { get; }
        public float Weight { get; }

        
        public HashSet<int> AllowAbove { get; } = new();
        public HashSet<int> AllowBelow { get; } = new();
        public HashSet<int> AllowLeft { get; } = new();
        public HashSet<int> AllowRight { get; } = new();

        public Tile(int id, string atlasKey, TextureRegion region, TileTag tags, float weight)
        {
            Id = id;
            AtlasKey = atlasKey;
            Region = region;
            Tags = tags;
            Weight = weight;
        }

        public bool Has(TileTag t) => (Tags & t) != 0;
    }
}
