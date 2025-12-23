using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using nkast.Aether.Physics2D.Collision.Shapes;
using nkast.Aether.Physics2D.Common;
using nkast.Aether.Physics2D.Dynamics;
using MonoGameVector2 = Microsoft.Xna.Framework.Vector2;

namespace SpaceTanks
{
    /// <summary>
    /// Helper class for drawing Aether physics bounds.
    /// </summary>
    public static class AetherBoundsRenderer
    {
        private static Texture2D _pixelTexture;
        private const float PixelScale = 100f; // Convert physics units to pixels

        /// <summary>
        /// Initialize the bounds renderer. Call once at startup.
        /// </summary>
        public static void Initialize(GraphicsDevice graphicsDevice)
        {
            _pixelTexture = new Texture2D(graphicsDevice, 1, 1);
            _pixelTexture.SetData(new[] { Color.White });
        }

        /// <summary>
        /// Draw the actual fixture shapes for a body.
        /// </summary>
        public static void DrawBodyFixtures(
            SpriteBatch spriteBatch,
            Body body,
            Color color,
            float thickness = 2f
        )
        {
            if (body == null || body.FixtureList.Count == 0)
                return;

            foreach (var fixture in body.FixtureList)
            {
                var shape = fixture.Shape;

                if (shape is CircleShape circle)
                {
                    MonoGameVector2 center = new MonoGameVector2(
                        body.Position.X * PixelScale,
                        body.Position.Y * PixelScale
                    );
                    float radius = circle.Radius * PixelScale;

                    DrawCircle(spriteBatch, center, radius, color, thickness, 32);
                }
                else if (shape is PolygonShape polygon)
                {
                    DrawPolygon(spriteBatch, body, polygon, color, thickness);
                }
            }
        }

        /// <summary>
        /// Draw a polygon shape.
        /// </summary>
        private static void DrawPolygon(
            SpriteBatch spriteBatch,
            Body body,
            PolygonShape polygon,
            Color color,
            float thickness
        )
        {
            if (polygon.Vertices.Count < 2)
                return;

            // Convert all vertices to world space
            MonoGameVector2[] worldVertices = new MonoGameVector2[polygon.Vertices.Count];

            for (int i = 0; i < polygon.Vertices.Count; i++)
            {
                var vertex = polygon.Vertices[i];

                // Rotate vertex by body rotation
                float rotatedX = (float)(
                    vertex.X * Math.Cos(body.Rotation) - vertex.Y * Math.Sin(body.Rotation)
                );
                float rotatedY = (float)(
                    vertex.X * Math.Sin(body.Rotation) + vertex.Y * Math.Cos(body.Rotation)
                );

                // Convert to world space (pixels)
                worldVertices[i] = new MonoGameVector2(
                    (body.Position.X + rotatedX) * PixelScale,
                    (body.Position.Y + rotatedY) * PixelScale
                );
            }

            // Draw lines between vertices
            for (int i = 0; i < worldVertices.Length; i++)
            {
                MonoGameVector2 start = worldVertices[i];
                MonoGameVector2 end = worldVertices[(i + 1) % worldVertices.Length]; // Wrap to first vertex

                DrawLine(spriteBatch, start, end, color, thickness);
            }
        }

        /// <summary>
        /// Draw a circle outline.
        /// </summary>
        private static void DrawCircle(
            SpriteBatch spriteBatch,
            MonoGameVector2 center,
            float radius,
            Color color,
            float thickness,
            int segments
        )
        {
            float angleStep = MathHelper.TwoPi / segments;

            for (int i = 0; i < segments; i++)
            {
                float angle1 = i * angleStep;
                float angle2 = (i + 1) * angleStep;

                MonoGameVector2 point1 =
                    center
                    + new MonoGameVector2(
                        (float)Math.Cos(angle1) * radius,
                        (float)Math.Sin(angle1) * radius
                    );

                MonoGameVector2 point2 =
                    center
                    + new MonoGameVector2(
                        (float)Math.Cos(angle2) * radius,
                        (float)Math.Sin(angle2) * radius
                    );

                DrawLine(spriteBatch, point1, point2, color, thickness);
            }
        }

        /// <summary>
        /// Draw the bounds for a body based on its position and fixture shapes.
        /// </summary>
        public static void DrawBodyAABB(
            SpriteBatch spriteBatch,
            Body body,
            Color color,
            float thickness = 1f
        )
        {
            if (body == null || body.FixtureList.Count == 0)
                return;

            // Calculate bounds by examining all fixtures
            MonoGameVector2 minPoint = new MonoGameVector2(float.MaxValue, float.MaxValue);
            MonoGameVector2 maxPoint = new MonoGameVector2(float.MinValue, float.MinValue);

            foreach (var fixture in body.FixtureList)
            {
                var shape = fixture.Shape;

                if (shape is CircleShape circle)
                {
                    MonoGameVector2 center = new MonoGameVector2(
                        body.Position.X * PixelScale,
                        body.Position.Y * PixelScale
                    );
                    float radius = circle.Radius * PixelScale;

                    minPoint.X = Math.Min(minPoint.X, center.X - radius);
                    minPoint.Y = Math.Min(minPoint.Y, center.Y - radius);
                    maxPoint.X = Math.Max(maxPoint.X, center.X + radius);
                    maxPoint.Y = Math.Max(maxPoint.Y, center.Y + radius);
                }
                else if (shape is PolygonShape polygon)
                {
                    foreach (var vertex in polygon.Vertices)
                    {
                        // Rotate vertex
                        float rotatedX = (float)(
                            vertex.X * Math.Cos(body.Rotation) - vertex.Y * Math.Sin(body.Rotation)
                        );
                        float rotatedY = (float)(
                            vertex.X * Math.Sin(body.Rotation) + vertex.Y * Math.Cos(body.Rotation)
                        );

                        // Convert to world space
                        MonoGameVector2 worldPos = new MonoGameVector2(
                            (body.Position.X + rotatedX) * PixelScale,
                            (body.Position.Y + rotatedY) * PixelScale
                        );

                        minPoint.X = Math.Min(minPoint.X, worldPos.X);
                        minPoint.Y = Math.Min(minPoint.Y, worldPos.Y);
                        maxPoint.X = Math.Max(maxPoint.X, worldPos.X);
                        maxPoint.Y = Math.Max(maxPoint.Y, worldPos.Y);
                    }
                }
            }

            // Draw the bounding box
            if (minPoint.X != float.MaxValue && maxPoint.X != float.MinValue)
            {
                DrawRectangle(spriteBatch, minPoint, maxPoint, color, thickness);
            }
        }

        /// <summary>
        /// Draw all body bounds in a world.
        /// </summary>
        public static void DrawWorldAABBs(
            SpriteBatch spriteBatch,
            World world,
            Color staticColor,
            Color dynamicColor,
            float thickness = 1f
        )
        {
            if (world == null)
                return;

            foreach (var body in world.BodyList)
            {
                Color color = body.BodyType == BodyType.Static ? staticColor : dynamicColor;
                DrawBodyAABB(spriteBatch, body, color, thickness);
            }
        }

        /// <summary>
        /// Draw all fixture shapes in a world.
        /// </summary>
        public static void DrawWorldFixtures(
            SpriteBatch spriteBatch,
            World world,
            Color staticColor,
            Color dynamicColor,
            float thickness = 2f
        )
        {
            if (world == null)
                return;

            foreach (var body in world.BodyList)
            {
                Color color = body.BodyType == BodyType.Static ? staticColor : dynamicColor;
                DrawBodyFixtures(spriteBatch, body, color, thickness);
            }
        }

        /// <summary>
        /// Draw a rectangle outline.
        /// </summary>
        private static void DrawRectangle(
            SpriteBatch spriteBatch,
            MonoGameVector2 minPoint,
            MonoGameVector2 maxPoint,
            Color color,
            float thickness
        )
        {
            // Top
            DrawLine(
                spriteBatch,
                minPoint,
                new MonoGameVector2(maxPoint.X, minPoint.Y),
                color,
                thickness
            );
            // Right
            DrawLine(
                spriteBatch,
                new MonoGameVector2(maxPoint.X, minPoint.Y),
                maxPoint,
                color,
                thickness
            );
            // Bottom
            DrawLine(
                spriteBatch,
                maxPoint,
                new MonoGameVector2(minPoint.X, maxPoint.Y),
                color,
                thickness
            );
            // Left
            DrawLine(
                spriteBatch,
                new MonoGameVector2(minPoint.X, maxPoint.Y),
                minPoint,
                color,
                thickness
            );
        }

        /// <summary>
        /// Draw a line between two points.
        /// </summary>
        private static void DrawLine(
            SpriteBatch spriteBatch,
            MonoGameVector2 start,
            MonoGameVector2 end,
            Color color,
            float thickness
        )
        {
            MonoGameVector2 direction = end - start;
            float length = direction.Length();

            if (length < 0.01f)
                return;

            spriteBatch.Draw(
                _pixelTexture,
                start,
                null,
                color,
                (float)Math.Atan2(direction.Y, direction.X),
                new MonoGameVector2(0, 0.5f),
                new MonoGameVector2(length, thickness),
                SpriteEffects.None,
                0f
            );
        }
    }
}
