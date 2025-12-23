using System;
using System.Collections.Generic;
using System.Diagnostics;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

public static class SpriteBatchExtensions
{
    /// <summary>
    /// Draws a polygon outline using the given color and line thickness.
    /// </summary>
    // public static void Draw(
    //     this SpriteBatch spriteBatch,
    //     Polygon polygon,
    //     Color color,
    //     int thickness = 2
    // )
    // {
    //     Texture2D pixel = new Texture2D(spriteBatch.GraphicsDevice, 1, 1);
    //     pixel.SetData(new[] { Color.White });
    //
    //     for (int i = 0; i < polygon.Vertices.Length; i++)
    //     {
    //         Vector2 p1 = polygon.Vertices[i];
    //         Vector2 p2 = polygon.Vertices[(i + 1) % polygon.Vertices.Length];
    //
    //         // Draw a line from p1 to p2
    //         Vector2 edge = p2 - p1;
    //         float length = edge.Length();
    //         float angle = MathF.Atan2(edge.Y, edge.X);
    //
    //         spriteBatch.Draw(
    //             pixel,
    //             p1,
    //             null,
    //             color,
    //             angle,
    //             Vector2.Zero,
    //             new Vector2(length, thickness),
    //             SpriteEffects.None,
    //             0f
    //         );
    //     }
    // }
}
