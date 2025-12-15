using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;

namespace MonoGameLibrary
{
    public interface IPhysicsEnabled
    {
        Vector2 Velocity { get; set; }
        Vector2 Acceleration { get; set; }
        float Drag { get; set; }
        float Mass {get; set; }
        public bool NeedsTraction {get; set;}

        // public Vector2 MaxSpeed;
    }
}
