using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;

namespace MonoGameLibrary
{
    public interface ICollidable
    {
        // Methods
        public virtual void OnCollision(CollisionInfo collisionInfo) { }

        public string GetGroupName();

        public CollisionShape GetBounds();
    }
}
