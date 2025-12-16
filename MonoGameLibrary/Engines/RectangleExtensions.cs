using System;
using Microsoft.Xna.Framework;

public static class RectangleExtensions
{
    public static bool Intersects(this Rectangle rect, Polygon polygon)
    {
        return polygon.AABB().Intersects(rect);
    }
}
