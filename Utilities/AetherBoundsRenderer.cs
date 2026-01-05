using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using nkast.Aether.Physics2D.Collision.Shapes;
using nkast.Aether.Physics2D.Common;
using nkast.Aether.Physics2D.Dynamics;
using MonoGameVector2 = Microsoft.Xna.Framework.Vector2;

namespace SpaceTanks
{
    
    
    
    public static class AetherBoundsRenderer
    {
        private static Texture2D _pixelTexture;
        private const float PixelScale = 100f; 

        
        
        
        public static void Initialize(GraphicsDevice graphicsDevice)
        {
            _pixelTexture = new Texture2D(graphicsDevice, 1, 1);
            _pixelTexture.SetData(new[] { Color.White });
        }

        
        
        
        public static void DrawBodyAABB(
            SpriteBatch spriteBatch,
            Body body,
            Color color,
            float thickness = 1f
        )
        {
            if (body == null || body.FixtureList.Count == 0)
                return;

            
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
                        
                        float rotatedX = (float)(
                            vertex.X * Math.Cos(body.Rotation) - vertex.Y * Math.Sin(body.Rotation)
                        );
                        float rotatedY = (float)(
                            vertex.X * Math.Sin(body.Rotation) + vertex.Y * Math.Cos(body.Rotation)
                        );

                        
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

            
            if (minPoint.X != float.MaxValue && maxPoint.X != float.MinValue)
            {
                DrawRectangle(spriteBatch, minPoint, maxPoint, color, thickness);
            }
        }

        
        
        
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

        
        
        
        private static void DrawRectangle(
            SpriteBatch spriteBatch,
            MonoGameVector2 minPoint,
            MonoGameVector2 maxPoint,
            Color color,
            float thickness
        )
        {
            
            DrawLine(
                spriteBatch,
                minPoint,
                new MonoGameVector2(maxPoint.X, minPoint.Y),
                color,
                thickness
            );
            
            DrawLine(
                spriteBatch,
                new MonoGameVector2(maxPoint.X, minPoint.Y),
                maxPoint,
                color,
                thickness
            );
            
            DrawLine(
                spriteBatch,
                maxPoint,
                new MonoGameVector2(minPoint.X, maxPoint.Y),
                color,
                thickness
            );
            
            DrawLine(
                spriteBatch,
                new MonoGameVector2(minPoint.X, maxPoint.Y),
                minPoint,
                color,
                thickness
            );
        }

        
        
        
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
