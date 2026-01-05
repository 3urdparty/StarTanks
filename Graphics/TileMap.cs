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
    public sealed class TileMap
    {
        public string Name { get; }
        public TextureAtlas Atlas { get; }

        public int TileWidth { get; }
        public int TileHeight { get; }

        public int TopThickness { get; }
        public float DecorChance { get; }
        public bool UseAdjacency { get; }

        private readonly Dictionary<int, Tile> _tilesById = new();
        private readonly Dictionary<string, Tile> _tilesByKey = new(
            StringComparer.OrdinalIgnoreCase
        );

        public IReadOnlyDictionary<int, Tile> TilesById => _tilesById;

        public TileMap(
            string name,
            TextureAtlas atlas,
            int tileW,
            int tileH,
            int topThickness,
            float decorChance,
            bool useAdjacency
        )
        {
            Name = name;
            Atlas = atlas;
            TileWidth = tileW;
            TileHeight = tileH;
            TopThickness = Math.Max(1, topThickness);
            DecorChance = Math.Max(0f, decorChance);
            UseAdjacency = useAdjacency;
        }

        public void AddTile(Tile t)
        {
            _tilesById[t.Id] = t;
            _tilesByKey[t.AtlasKey] = t;
        }

        public Tile GetTile(int id) => _tilesById[id];

        public IEnumerable<Tile> WithTag(TileTag tags) =>
            _tilesById.Values.Where(t => (t.Tags & tags) != 0);

        public TextureRegion GetRegionById(int id) => _tilesById[id].Region;

        internal static TileTag ParseTags(string tags)
        {
            if (string.IsNullOrWhiteSpace(tags))
                return TileTag.None;

            static TileTag MapAlias(string s)
            {
                s = s.Trim();

                
                return s.ToLowerInvariant() switch
                {
                    "top" => TileTag.TopSurface,
                    "topsurface" => TileTag.TopSurface,

                    "mid" => TileTag.MiddleFill,
                    "middle" => TileTag.MiddleFill,
                    "inner" => TileTag.MiddleFill, 

                    "bottom" => TileTag.BottomCap,
                    "bottomcap" => TileTag.BottomCap,

                    "edge" => TileTag.Edge,
                    "decor" => TileTag.Decor,

                    "left" => TileTag.Left,
                    "right" => TileTag.Right,
                    "center" => TileTag.Center,

                    "topleft" or "corner_tl" or "cornertl" => TileTag.CornerTL,
                    "topright" or "corner_tr" or "cornertr" => TileTag.CornerTR,
                    "bottomleft" or "corner_bl" or "cornerbl" => TileTag.CornerBL,
                    "bottomright" or "corner_br" or "cornerbr" => TileTag.CornerBR,

                    "slopeupleft" or "slope_ul" => TileTag.SlopeUpLeft,
                    "slopeupright" or "slope_ur" => TileTag.SlopeUpRight,

                    "innercornertl" => TileTag.InnerCornerTL,
                    "innercornertr" => TileTag.InnerCornerTR,
                    "innercornerbl" => TileTag.InnerCornerBL,
                    "innercornerbr" => TileTag.InnerCornerBR,

                    _ => TileTag.None,
                };
            }

            TileTag result = TileTag.None;
            var parts = tags.Split(new[] { ',', '|', ' ' }, StringSplitOptions.RemoveEmptyEntries);

            foreach (var p in parts)
            {
                
                if (Enum.TryParse<TileTag>(p.Trim(), true, out var t))
                {
                    result |= t;
                    continue;
                }

                
                result |= MapAlias(p);
            }

            return result;
        }

        internal static List<int> ParseIdList(string s)
        {
            if (string.IsNullOrWhiteSpace(s))
                return new List<int>();
            return s.Split(
                    new[] { ' ', '\t', '\r', '\n', ',' },
                    StringSplitOptions.RemoveEmptyEntries
                )
                .Select(x => int.Parse(x, CultureInfo.InvariantCulture))
                .ToList();
        }

        public static TileMap FromFile(ContentManager content, string rulesXmlPath)
        {
            string filePath = Path.Combine(content.RootDirectory, rulesXmlPath);

            using Stream stream = TitleContainer.OpenStream(filePath);
            using XmlReader reader = XmlReader.Create(stream);
            XDocument doc = XDocument.Load(reader);

            XElement root = doc.Root ?? throw new InvalidDataException("Missing root element.");
            if (!string.Equals(root.Name.LocalName, "Tilemap", StringComparison.OrdinalIgnoreCase))
                throw new InvalidDataException("Root must be <Tilemap>.");

            string name = root.Attribute("name")?.Value ?? "unnamed";
            string atlasFile =
                root.Attribute("atlas")?.Value
                ?? throw new InvalidDataException("Tilemap requires 'atlas' attribute.");

            int tileW = int.Parse(
                root.Attribute("tileWidth")?.Value ?? "16",
                CultureInfo.InvariantCulture
            );
            int tileH = int.Parse(
                root.Attribute("tileHeight")?.Value ?? "16",
                CultureInfo.InvariantCulture
            );

            int topThickness = int.Parse(
                root.Attribute("topThickness")?.Value ?? "1",
                CultureInfo.InvariantCulture
            );
            float decorChance = float.Parse(
                root.Attribute("decorChance")?.Value ?? "0",
                CultureInfo.InvariantCulture
            );
            bool useAdjacency = bool.Parse(root.Attribute("useAdjacency")?.Value ?? "false");

            
            TextureAtlas atlas = TextureAtlas.FromFile(content, atlasFile);

            var set = new TileMap(
                name,
                atlas,
                tileW,
                tileH,
                topThickness,
                decorChance,
                useAdjacency
            );

            XElement paletteEl =
                root.Elements().FirstOrDefault(e => e.Name.LocalName == "Palette")
                ?? throw new InvalidDataException("Missing <Palette>.");
            
            foreach (var tileEl in paletteEl.Elements().Where(e => e.Name.LocalName == "Tile"))
            {
                int id = int.Parse(
                    tileEl.Attribute("id")?.Value
                        ?? throw new InvalidDataException("Tile missing id."),
                    CultureInfo.InvariantCulture
                );
                string key =
                    tileEl.Attribute("key")?.Value
                    ?? throw new InvalidDataException("Tile missing key.");

                string tagsStr = tileEl.Attribute("tags")?.Value ?? "";
                TileTag tags = ParseTags(tagsStr);

                float weight = float.Parse(
                    tileEl.Attribute("weight")?.Value ?? "1",
                    CultureInfo.InvariantCulture
                );

                TextureRegion region = atlas.GetRegion(key);
                set.AddTile(new Tile(id, key, region, tags, weight));
            }

            
            XElement adjEl = root.Elements().FirstOrDefault(e => e.Name.LocalName == "Adjacency");
            if (adjEl != null)
            {
                foreach (var ruleEl in adjEl.Elements().Where(e => e.Name.LocalName == "Rule"))
                {
                    int id = int.Parse(
                        ruleEl.Attribute("tile")?.Value
                            ?? throw new InvalidDataException("Adjacency Rule missing tile."),
                        CultureInfo.InvariantCulture
                    );
                    var t = set.GetTile(id);

                    var above = ParseIdList(ruleEl.Attribute("above")?.Value);
                    var below = ParseIdList(ruleEl.Attribute("below")?.Value);
                    var left = ParseIdList(ruleEl.Attribute("left")?.Value);
                    var right = ParseIdList(ruleEl.Attribute("right")?.Value);

                    foreach (var a in above)
                        t.AllowAbove.Add(a);
                    foreach (var b in below)
                        t.AllowBelow.Add(b);
                    foreach (var l in left)
                        t.AllowLeft.Add(l);
                    foreach (var r in right)
                        t.AllowRight.Add(r);
                }
            }

            return set;
        }
    }
}
