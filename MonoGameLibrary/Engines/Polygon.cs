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

    public Rectangle AABB()
    {
        float minX = Vertices[0].X,
            maxX = Vertices[0].X;
        float minY = Vertices[0].Y,
            maxY = Vertices[0].Y;

        for (int i = 1; i < Vertices.Length; i++)
        {
            var v = Vertices[i];
            if (v.X < minX)
                minX = v.X;
            if (v.X > maxX)
                maxX = v.X;
            if (v.Y < minY)
                minY = v.Y;
            if (v.Y > maxY)
                maxY = v.Y;
        }

        return new Rectangle(
            (int)MathF.Floor(minX),
            (int)MathF.Floor(minY),
            (int)MathF.Ceiling(maxX - minX),
            (int)MathF.Ceiling(maxY - minY)
        );
    }

    /// <summary>
    /// Checks if this polygon intersects with a rectangle using AABB and point-in-shape tests.
    /// </summary>
    public bool Intersects(Rectangle other)
    {
        // Broad-phase: check AABB first
        if (!AABB().Intersects(other))
            return false;

        // Check if any polygon vertex is inside the rectangle
        foreach (var vertex in Vertices)
        {
            if (other.Contains((int)vertex.X, (int)vertex.Y))
                return true;
        }

        // Check if any rectangle corner is inside this polygon
        if (ContainsPoint(new Vector2(other.Left, other.Top)))
            return true;
        if (ContainsPoint(new Vector2(other.Right, other.Top)))
            return true;
        if (ContainsPoint(new Vector2(other.Right, other.Bottom)))
            return true;
        if (ContainsPoint(new Vector2(other.Left, other.Bottom)))
            return true;

        // Check if any edge of the polygon intersects with rectangle edges
        for (int i = 0; i < Vertices.Length; i++)
        {
            Vector2 p1 = Vertices[i];
            Vector2 p2 = Vertices[(i + 1) % Vertices.Length];

            if (LineIntersectsRectangle(p1, p2, other))
                return true;
        }

        return false;
    }

    /// <summary>
    /// Checks if this polygon intersects with another polygon using SAT (Separating Axis Theorem).
    /// </summary>
    public bool Intersects(Polygon other)
    {
        if (other == null)
            throw new ArgumentNullException(nameof(other));

        // Broad-phase: check AABB first
        if (!AABB().Intersects(other.AABB()))
            return false;

        // Narrow-phase: SAT - check if any separating axis exists
        if (HasSeparatingAxis(this.Vertices, other.Vertices))
            return false;
        if (HasSeparatingAxis(other.Vertices, this.Vertices))
            return false;

        // No separating axis found, polygons intersect
        return true;
    }

    /// <summary>
    /// Checks if a line segment intersects with a rectangle.
    /// </summary>
    private static bool LineIntersectsRectangle(Vector2 p1, Vector2 p2, Rectangle rect)
    {
        // Check if line intersects left/right edges
        if (LineIntersectsVertical(p1, p2, rect.Left, rect.Top, rect.Bottom))
            return true;
        if (LineIntersectsVertical(p1, p2, rect.Right, rect.Top, rect.Bottom))
            return true;

        // Check if line intersects top/bottom edges
        if (LineIntersectsHorizontal(p1, p2, rect.Top, rect.Left, rect.Right))
            return true;
        if (LineIntersectsHorizontal(p1, p2, rect.Bottom, rect.Left, rect.Right))
            return true;

        return false;
    }

    private static bool LineIntersectsVertical(Vector2 p1, Vector2 p2, float x, float y1, float y2)
    {
        float minY = MathF.Min(p1.Y, p2.Y);
        float maxY = MathF.Max(p1.Y, p2.Y);

        if (minY > y2 || maxY < y1)
            return false;

        if (MathF.Abs(p2.X - p1.X) < 1e-6f)
            return MathF.Abs(p1.X - x) < 1e-6f;

        float t = (x - p1.X) / (p2.X - p1.X);
        return t >= 0 && t <= 1;
    }

    private static bool LineIntersectsHorizontal(
        Vector2 p1,
        Vector2 p2,
        float y,
        float x1,
        float x2
    )
    {
        float minX = MathF.Min(p1.X, p2.X);
        float maxX = MathF.Max(p1.X, p2.X);

        if (minX > x2 || maxX < x1)
            return false;

        if (MathF.Abs(p2.Y - p1.Y) < 1e-6f)
            return MathF.Abs(p1.Y - y) < 1e-6f;

        float t = (y - p1.Y) / (p2.Y - p1.Y);
        return t >= 0 && t <= 1;
    }

    private static bool HasSeparatingAxis(Vector2[] a, Vector2[] b)
    {
        // For each edge in polygon A, project both polygons onto the edge normal
        for (int i = 0; i < a.Length; i++)
        {
            Vector2 p1 = a[i];
            Vector2 p2 = a[(i + 1) % a.Length];

            Vector2 edge = p2 - p1;
            Vector2 axis = new Vector2(-edge.Y, edge.X);

            // Skip degenerate edges
            float axisLenSq = axis.LengthSquared();
            if (axisLenSq < 1e-12f)
                continue;

            // Normalize axis
            axis /= MathF.Sqrt(axisLenSq);

            ProjectOntoAxis(a, axis, out float minA, out float maxA);
            ProjectOntoAxis(b, axis, out float minB, out float maxB);

            // If projections don't overlap, we found a separating axis
            if (!Overlaps(minA, maxA, minB, maxB))
                return true;
        }

        return false;
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

    /// <summary>
    /// Translates the polygon by the given offset, returning a new Polygon.
    /// </summary>
    public static Polygon operator +(Polygon polygon, Vector2 offset)
    {
        Vector2[] translatedVertices = new Vector2[polygon.Vertices.Length];

        for (int i = 0; i < polygon.Vertices.Length; i++)
        {
            translatedVertices[i] = polygon.Vertices[i] + offset;
        }

        return new Polygon(translatedVertices);
    }

    /// <summary>
    /// Translates the polygon by the given offset (reverse operand order).
    /// </summary>
    public static Polygon operator +(Vector2 offset, Polygon polygon)
    {
        return polygon + offset;
    }

    /// <summary>
    /// Translates the polygon by the negative offset.
    /// </summary>
    public static Polygon operator -(Polygon polygon, Vector2 offset)
    {
        Vector2[] translatedVertices = new Vector2[polygon.Vertices.Length];

        for (int i = 0; i < polygon.Vertices.Length; i++)
        {
            translatedVertices[i] = polygon.Vertices[i] - offset;
        }

        return new Polygon(translatedVertices);
    }
}
