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
    [Flags]
    public enum TileTag
    {
        None = 0,

        TopSurface = 1 << 0,
        MiddleFill = 1 << 1,
        BottomCap = 1 << 2,
        Decor = 1 << 3,
        Edge = 1 << 4,

        Left = 1 << 5,
        Right = 1 << 6,
        Center = 1 << 7,

        CornerTL = 1 << 8,
        CornerTR = 1 << 9,
        CornerBL = 1 << 10,
        CornerBR = 1 << 11,

        SlopeUpLeft = 1 << 12,
        SlopeUpRight = 1 << 13,

        InnerCornerTL = 1 << 14,
        InnerCornerTR = 1 << 15,
        InnerCornerBL = 1 << 16,
        InnerCornerBR = 1 << 17,
    }
}
