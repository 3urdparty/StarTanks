using System;
using System;
using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using MonoGameLibrary;
using MonoGameLibrary.Graphics;
using SpaceTanks.Extensions;

namespace SpaceTanks
{
    public sealed class Camera2D
    {
        public Vector2 Position; // world-space top-left
        public float Zoom = 1f;
        public float Rotation = 0f;

        public Matrix GetTransform()
        {
            return Matrix.CreateTranslation(new Vector3(-Position, 0f))
                * Matrix.CreateRotationZ(Rotation)
                * Matrix.CreateScale(Zoom, Zoom, 1f);
        }
    }
}
