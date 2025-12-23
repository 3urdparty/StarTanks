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

    /// <summary>
    /// Triangulates a simple polygon (no self-intersections) into triangles.
    /// Input vertices must define the boundary in order (CW or CCW).
    /// </summary>
    public static List<Triangle> Triangulate(IReadOnlyList<Vector2> polygon)
    {
        if (polygon == null)
            throw new ArgumentNullException(nameof(polygon));
        if (polygon.Count < 3)
            throw new ArgumentException("Polygon must have at least 3 vertices.");

        // Copy & sanitize (remove duplicate last point if closed)
        var pts = new List<Vector2>(polygon.Count);
        for (int i = 0; i < polygon.Count; i++)
            pts.Add(polygon[i]);

        if (pts.Count >= 2 && NearlyEqual(pts[0], pts[pts.Count - 1]))
            pts.RemoveAt(pts.Count - 1);

        // Remove consecutive duplicates
        for (int i = pts.Count - 1; i > 0; i--)
        {
            if (NearlyEqual(pts[i], pts[i - 1]))
                pts.RemoveAt(i);
        }
        if (pts.Count < 3)
            throw new ArgumentException("Polygon degenerated after removing duplicates.");

        // Ensure CCW winding (ear clipping is easier/consistent)
        if (SignedArea(pts) < 0f)
            pts.Reverse();

        // Index list for clipping
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

                // Check no other point lies inside triangle (a,b,c)
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

                // It's an ear
                result.Add(new Triangle(a, b, c));
                indices.RemoveAt(i);
                earFound = true;
                break;
            }

            if (!earFound)
            {
                // This usually means polygon is self-intersecting or numerically degenerate.
                throw new InvalidOperationException(
                    "Failed to triangulate polygon. Ensure it is simple (non self-intersecting) and non-degenerate."
                );
            }

            // Guard against infinite loops in bad input cases
            if (++guard > 100000)
                throw new InvalidOperationException(
                    "Triangulation guard triggered (bad polygon input)."
                );
        }

        // Last triangle
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
        // CCW polygon: convex if cross((b-a),(c-b)) > 0
        return Cross(b - a, c - b) > 1e-7f;
    }

    private static float Cross(Vector2 u, Vector2 v) => u.X * v.Y - u.Y * v.X;

    private static bool PointInTriangle(Vector2 p, Vector2 a, Vector2 b, Vector2 c)
    {
        // Barycentric technique with sign checks
        float c1 = Cross(b - a, p - a);
        float c2 = Cross(c - b, p - b);
        float c3 = Cross(a - c, p - c);

        bool hasNeg = (c1 < 0f) || (c2 < 0f) || (c3 < 0f);
        bool hasPos = (c1 > 0f) || (c2 > 0f) || (c3 > 0f);

        // Point is inside or on edges if not both negative and positive
        return !(hasNeg && hasPos);
    }

    private static bool NearlyEqual(Vector2 a, Vector2 b)
    {
        const float eps = 1e-6f;
        return Math.Abs(a.X - b.X) < eps && Math.Abs(a.Y - b.Y) < eps;
    }
}
