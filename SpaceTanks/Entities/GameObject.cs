using System;
using System;
using System.Collections.Generic;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Graphics;
using MonoGameLibrary;
using MonoGameLibrary.Graphics;
using nkast.Aether.Physics2D.Dynamics;
using nkast.Aether.Physics2D.Dynamics;
using nkast.Aether.Physics2D.Dynamics.Joints;
using AetherVector2 = nkast.Aether.Physics2D.Common.Vector2;

namespace SpaceTanks
{
    public abstract class GameObject
    {
        public String Name { set; get; }

        public float Rotation { set; get; }
        public Vector2 Position { set; get; }
        public Vector2 Origin;
        public int Height;
        public int Width;
        public float Mass = 10f;
        public Vector2 Scale = new Vector2(1, 1);
        public Color Color = Color.White;

        public SpriteEffects Effects { get; set; } = SpriteEffects.None;

        public float LayerDepth { get; set; } = 0.1f;

        public bool Destroyed { get; protected set; } = false;

        public abstract void Draw(SpriteBatch spriteBatch);
        public abstract void Update(GameTime gameTime);
    }
}
