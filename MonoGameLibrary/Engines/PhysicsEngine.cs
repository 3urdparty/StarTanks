using System;
using System.Collections.Generic;
using System.Diagnostics;
using Microsoft.Xna.Framework;
using MonoGameLibrary;

namespace MonoGameLibrary
{
    public class PhysicsEngine
    {
        public bool Enabled { get; set; } = true;
        public List<GameObject> Objects { get; set; } = new List<GameObject>();
        public Vector2 GravityForce { get; set; } = new Vector2(0, 500f);
        public float MaxFallSpeed { get; set; } = 300f;
        public List<ICollidable> Obstacles { get; set; } = new List<ICollidable>();

        public void Update(GameTime gameTime)
        {
            if (!Enabled)
                return;

            float deltaTime = (float)gameTime.ElapsedGameTime.TotalSeconds;

            foreach (GameObject obj in Objects)
            {
                if (obj is not IPhysicsEnabled physicsObj || physicsObj.Stationary)
                    continue;

                // Check if grounded (only for IGrounded objects)
                bool isOnGround = (obj is IGrounded) && IsOnGround(obj as ICollidable);

                // Apply gravity only when airborne
                if (!isOnGround)
                {
                    physicsObj.Acceleration += GravityForce * physicsObj.Mass;
                }

                // Update velocity
                physicsObj.Velocity += physicsObj.Acceleration * deltaTime;

                // Apply drag
                if (physicsObj.Drag > 0)
                {
                    physicsObj.Velocity *= (1 - physicsObj.Drag * deltaTime);
                }

                // Clamp fall speed
                if (physicsObj.Velocity.Y > MaxFallSpeed)
                {
                    physicsObj.Velocity = new Vector2(physicsObj.Velocity.X, MaxFallSpeed);
                }

                // Zero out vertical velocity when grounded
                if (isOnGround)
                {
                    physicsObj.Velocity = new Vector2(physicsObj.Velocity.X, 0);
                }

                // Update position
                Vector2 newPosition = obj.Position + physicsObj.Velocity * deltaTime;
                obj.Position = newPosition;

                // Update rotation for projectiles
                if (obj is Projectile)
                {
                    Vector2 direction = physicsObj.Velocity;
                    obj.Rotation = MathF.Atan2(direction.Y, direction.X);
                }

                Debug.WriteLine(
                    $"[PhysicsEngine]: {obj.Name} - Pos: {obj.Position}, Vel: {physicsObj.Velocity}, OnGround: {isOnGround}"
                );
            }
        }

        public void HandleCollision(ICollidable thisCollidable, ICollidable thatCollidable)
        {
            // Collision response logic goes here
            // Example: bounce, stop, etc.
            if (thisCollidable is GameObject thisObj && thisObj is IPhysicsEnabled thisPhysics)
            {
                // Handle collision based on object type
            }
        }

        /// <summary>
        /// Checks if an object is standing on a platform below it.
        /// </summary>
        private bool IsOnGround(ICollidable moving)
        {
            if (moving == null)
                return false;

            GameObject movingObj = moving as GameObject;

            // Check slightly below the object

            foreach (ICollidable obstacle in Obstacles)
            {
                if (obstacle == moving)
                    continue;

                if (moving.GetBounds().Intersects(obstacle.GetBounds()))
                    return true;
            }

            return false;
        }

        public void RegisterGameObject(GameObject obj)
        {
            if (obj is IPhysicsEnabled)
            {
                Objects.Add(obj);
                Debug.WriteLine($"[PhysicsEngine]: Added Physics object {obj.Name}");
            }
        }

        public void DeregisterGameObject(GameObject obj)
        {
            if (Objects.Contains(obj))
            {
                Objects.Remove(obj);
                Debug.WriteLine($"[PhysicsEngine]: Removed Physics object {obj.Name}");
            }
        }

        public void RegisterObstacle(ICollidable obstacle)
        {
            if (!Obstacles.Contains(obstacle))
            {
                Obstacles.Add(obstacle);
                Debug.WriteLine($"[PhysicsEngine]: Added obstacle");
            }
        }

        public void DeregisterObstacle(ICollidable obstacle)
        {
            Obstacles.Remove(obstacle);
        }
    }
}
