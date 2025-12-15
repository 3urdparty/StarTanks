using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace MonoGameLibrary
{
    public abstract class GameObject
    {
        public String Name { set; get; }

        public float Rotation { set; get; }
        public Vector2 Position { set; get; }
        public Vector2 Origin;
        public float Height;
        public float Width;

        public abstract void Draw(SpriteBatch spriteBatch);
        public abstract void Update(GameTime gameTime);
    }
}
