using System;
using Microsoft.Xna.Framework;

public sealed class Polygon
{
    public Vector2[] Vertices { get; }

    public Polygon(Vector2[] vertices)
    {
        if (vertices == null || vertices.Length < 3)
            throw new ArgumentException("Polygon must have at least 3 vertices.");

        Vertices = vertices;
    }

    public bool ContainsPoint(Vector2 p)
    {
        bool inside = false;
        int j = Vertices.Length - 1;

        for (int i = 0; i < Vertices.Length; i++)
        {
            var vi = Vertices[i];
            var vj = Vertices[j];

            bool intersect =
                ((vi.Y > p.Y) != (vj.Y > p.Y))
                && (p.X < (vj.X - vi.X) * (p.Y - vi.Y) / ((vj.Y - vi.Y) + 1e-8f) + vi.X);

            if (intersect)
                inside = !inside;

            j = i;
        }

        return inside;
    }
}
