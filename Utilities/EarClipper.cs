using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;

public static class EarClipper
{
    public readonly struct Triangle
    {
        public readonly Vector2 A;
        public readonly Vector2 B;
        public readonly Vector2 C;

        public Triangle(Vector2 a, Vector2 b, Vector2 c)
        {
            A = a;
            B = b;
            C = c;
        }
    }

    
    
    
    
    public static List<Triangle> Triangulate(IReadOnlyList<Vector2> polygon)
    {
        if (polygon == null)
            throw new ArgumentNullException(nameof(polygon));
        if (polygon.Count < 3)
            throw new ArgumentException("Polygon must have at least 3 vertices.");

        
        var pts = new List<Vector2>(polygon.Count);
        for (int i = 0; i < polygon.Count; i++)
            pts.Add(polygon[i]);

        if (pts.Count >= 2 && NearlyEqual(pts[0], pts[pts.Count - 1]))
            pts.RemoveAt(pts.Count - 1);

        
        for (int i = pts.Count - 1; i > 0; i--)
        {
            if (NearlyEqual(pts[i], pts[i - 1]))
                pts.RemoveAt(i);
        }
        if (pts.Count < 3)
            throw new ArgumentException("Polygon degenerated after removing duplicates.");

        
        if (SignedArea(pts) < 0f)
            pts.Reverse();

        
        var indices = new List<int>(pts.Count);
        for (int i = 0; i < pts.Count; i++)
            indices.Add(i);

        var result = new List<Triangle>(pts.Count - 2);

        int guard = 0;
        while (indices.Count > 3)
        {
            bool earFound = false;

            for (int i = 0; i < indices.Count; i++)
            {
                int i0 = indices[(i - 1 + indices.Count) % indices.Count];
                int i1 = indices[i];
                int i2 = indices[(i + 1) % indices.Count];

                var a = pts[i0];
                var b = pts[i1];
                var c = pts[i2];

                if (!IsConvex(a, b, c))
                    continue;

                
                bool anyInside = false;
                for (int j = 0; j < indices.Count; j++)
                {
                    int ij = indices[j];
                    if (ij == i0 || ij == i1 || ij == i2)
                        continue;

                    if (PointInTriangle(pts[ij], a, b, c))
                    {
                        anyInside = true;
                        break;
                    }
                }

                if (anyInside)
                    continue;

                
                result.Add(new Triangle(a, b, c));
                indices.RemoveAt(i);
                earFound = true;
                break;
            }

            if (!earFound)
            {
                
                throw new InvalidOperationException(
                    "Failed to triangulate polygon. Ensure it is simple (non self-intersecting) and non-degenerate."
                );
            }

            
            if (++guard > 100000)
                throw new InvalidOperationException(
                    "Triangulation guard triggered (bad polygon input)."
                );
        }

        
        {
            var a = pts[indices[0]];
            var b = pts[indices[1]];
            var c = pts[indices[2]];
            result.Add(new Triangle(a, b, c));
        }

        return result;
    }

    private static float SignedArea(IReadOnlyList<Vector2> pts)
    {
        float area = 0f;
        for (int i = 0; i < pts.Count; i++)
        {
            var p = pts[i];
            var q = pts[(i + 1) % pts.Count];
            area += (p.X * q.Y) - (q.X * p.Y);
        }
        return area * 0.5f;
    }

    private static bool IsConvex(Vector2 a, Vector2 b, Vector2 c)
    {
        
        return Cross(b - a, c - b) > 1e-7f;
    }

    private static float Cross(Vector2 u, Vector2 v) => u.X * v.Y - u.Y * v.X;

    private static bool PointInTriangle(Vector2 p, Vector2 a, Vector2 b, Vector2 c)
    {
        
        float c1 = Cross(b - a, p - a);
        float c2 = Cross(c - b, p - b);
        float c3 = Cross(a - c, p - c);

        bool hasNeg = (c1 < 0f) || (c2 < 0f) || (c3 < 0f);
        bool hasPos = (c1 > 0f) || (c2 > 0f) || (c3 > 0f);

        
        return !(hasNeg && hasPos);
    }

    private static bool NearlyEqual(Vector2 a, Vector2 b)
    {
        const float eps = 1e-6f;
        return Math.Abs(a.X - b.X) < eps && Math.Abs(a.Y - b.Y) < eps;
    }
}
