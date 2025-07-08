using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Text;

namespace GraphPlotter
{
    public static class Extentions
    {
        public static void DrawLine(this SpriteBatch spriteBatch, Texture2D pixel, Vector2 start, Vector2 end, Color color, float thickness = 1f)
        {
            Vector2 edge = end - start;
            float angle = (float)Math.Atan2(edge.Y, edge.X);
            spriteBatch.Draw(pixel,
                             new Rectangle((int)start.X, (int)start.Y, (int)edge.Length(), (int)thickness),
                             null,
                             color,
                             angle,
                             Vector2.Zero,
                             SpriteEffects.None,
                             0);
        }
    }
}
