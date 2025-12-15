using System;
using Microsoft.Xna.Framework;

public sealed class CollisionShape
{
    public Polygon Shape { get; }
    public Rectangle AABB { get; private set; }

    public CollisionShape(Polygon shape)
    {
        if (shape == null)
            throw new ArgumentNullException(nameof(shape));

        Shape = shape;
        RecomputeAABB();
    }

    public void RecomputeAABB()
    {
        float minX = Shape.Vertices[0].X,
            maxX = Shape.Vertices[0].X;
        float minY = Shape.Vertices[0].Y,
            maxY = Shape.Vertices[0].Y;

        for (int i = 1; i < Shape.Vertices.Length; i++)
        {
            var v = Shape.Vertices[i];
            if (v.X < minX)
                minX = v.X;
            if (v.X > maxX)
                maxX = v.X;
            if (v.Y < minY)
                minY = v.Y;
            if (v.Y > maxY)
                maxY = v.Y;
        }

        AABB = new Rectangle(
            (int)MathF.Floor(minX),
            (int)MathF.Floor(minY),
            (int)MathF.Ceiling(maxX - minX),
            (int)MathF.Ceiling(maxY - minY)
        );
    }

    public bool ContainsPoint(Vector2 p)
    {
        if (!AABB.Contains(p))
            return false;
        return Shape.ContainsPoint(p);
    }

    public bool Intersects(CollisionShape other)
    {
        if (other == null)
            throw new ArgumentNullException(nameof(other));

        // Broad-phase: check AABB first
        if (!AABB.Intersects(other.AABB))
            return false;

        // Narrow-phase: SAT - if any separating axis exists, they do NOT intersect
        if (HasSeparatingAxis(this.Shape.Vertices, other.Shape.Vertices))
            return false;
        if (HasSeparatingAxis(other.Shape.Vertices, this.Shape.Vertices))
            return false;

        // If no separating axis found, they intersect
        return true;
    }

    /// <summary>
    /// True if any vertex of this collision shape is inside the other collision shape.
    /// </summary>
    public bool AnyPointInside(CollisionShape other)
    {
        if (other == null)
            throw new ArgumentNullException(nameof(other));

        for (int i = 0; i < Shape.Vertices.Length; i++)
            if (other.ContainsPoint(Shape.Vertices[i]))
                return true;

        return false;
    }

    // ==================== SAT Helpers ====================

    private static bool HasSeparatingAxis(Vector2[] a, Vector2[] b)
    {
        // For each edge in polygon A, project both polygons onto the edge normal
        // and see if projections overlap. If they don't overlap on any axis => separation.
        for (int i = 0; i < a.Length; i++)
        {
            Vector2 p1 = a[i];
            Vector2 p2 = a[(i + 1) % a.Length];

            Vector2 edge = p2 - p1;

            // Perpendicular axis (normal). Either direction works.
            Vector2 axis = new Vector2(-edge.Y, edge.X);

            // If edge is degenerate, skip
            float axisLenSq = axis.LengthSquared();
            if (axisLenSq < 1e-12f)
                continue;

            // Normalize axis to keep projections numerically stable
            axis /= MathF.Sqrt(axisLenSq);

            ProjectOntoAxis(a, axis, out float minA, out float maxA);
            ProjectOntoAxis(b, axis, out float minB, out float maxB);

            if (!Overlaps(minA, maxA, minB, maxB))
                return true; // Found a separating axis
        }

        return false; // No separating axis found
    }

    private static void ProjectOntoAxis(Vector2[] poly, Vector2 axis, out float min, out float max)
    {
        float dot = Vector2.Dot(poly[0], axis);
        min = dot;
        max = dot;

        for (int i = 1; i < poly.Length; i++)
        {
            dot = Vector2.Dot(poly[i], axis);
            if (dot < min)
                min = dot;
            if (dot > max)
                max = dot;
        }
    }

    private static bool Overlaps(float minA, float maxA, float minB, float maxB)
    {
        // Touching counts as intersecting
        return !(maxA < minB || maxB < minA);
    }
}
