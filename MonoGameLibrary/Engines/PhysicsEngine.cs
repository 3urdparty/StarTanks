using System;
using System.Collections.Generic;
using System.Diagnostics;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using MonoGameLibrary;
using MonoGameLibrary.Graphics;

namespace MonoGameLibrary
{
    public class PhysicsEngine
    {
        // Fields and Properties
        public bool Enabled { get; set; }
        public List<GameObject> objects = new List<GameObject>();
        public Vector2 GravityForce { get; set; } = new Vector2(0, 500f);
        public float MaxFallSpeed { get; set; } = 300f;
        public List<ICollidable> Obstacles { get; set; } = new List<ICollidable>();

        // Constructors and Methods
        public PhysicsEngine()
        {
            Enabled = true;
        }

        public void Update(GameTime gameTime)
        {
            if (!Enabled)
                return;

            float deltaTime = (float)gameTime.ElapsedGameTime.TotalSeconds;

            foreach (GameObject obj in objects)
            {
                if (obj is IPhysicsEnabled physicsObj)
                {
                    // Apply gravity to acceleration
                    Vector2 acceleration = physicsObj.Acceleration + GravityForce * physicsObj.Mass;

                    // Update velocity: v = v + a * dt
                    physicsObj.Velocity += acceleration * deltaTime;

                    // Apply drag/friction if needed
                    if (physicsObj.Drag > 0)
                    {
                        physicsObj.Velocity *= (1 - physicsObj.Drag * deltaTime);
                    }

                    // Clamp fall speed to prevent infinite acceleration
                    if (physicsObj.Velocity.Y > MaxFallSpeed)
                    {
                        physicsObj.Velocity = new Vector2(physicsObj.Velocity.X, MaxFallSpeed);
                    }

                    // Check if object is on the ground
                    bool isOnGround = IsOnGround(obj as ICollidable);

                    // Calculate new position
                    Vector2 newPosition = obj.Position + physicsObj.Velocity * deltaTime;
                    Vector2 finalPosition = obj.Position;
                    float newRotation = obj.Rotation;

                    // Only allow horizontal movement if on the ground
                    if (physicsObj is IGrounded)
                    {
                        if (isOnGround)
                        {
                            // Try horizontal movement only
                            Vector2 horizontalPosition = new Vector2(newPosition.X, obj.Position.Y);
                            if (CanMoveHorizontally(obj as ICollidable, horizontalPosition))
                            {
                                finalPosition = new Vector2(horizontalPosition.X, finalPosition.Y);
                            }
                            else
                            {
                                // Block horizontal movement
                                physicsObj.Velocity = new Vector2(0, physicsObj.Velocity.Y);
                            }
                        }
                        else
                        {
                            physicsObj.Acceleration = new Vector2(0, physicsObj.Acceleration.Y);
                        }

                        // Try vertical movement
                        Vector2 verticalPosition = new Vector2(finalPosition.X, newPosition.Y);
                        if (CanMoveVertically(obj as ICollidable, verticalPosition))
                        {
                            finalPosition = verticalPosition;
                        }
                        else
                        {
                            // Block vertical movement (hit ground or ceiling)
                            physicsObj.Velocity = new Vector2(physicsObj.Velocity.X, 0);
                        }
                    }
                    else
                    {
                        finalPosition = new Vector2(newPosition.X, newPosition.Y);
                        Vector2 direction = finalPosition - obj.Position;
                        newRotation = MathF.Atan2(direction.Y, direction.X);
                    }

                    obj.Position = finalPosition;
                    if (obj is Projectile)
                    {
                        obj.Rotation = newRotation;
                    }

                    Debug.WriteLine(
                        $"[PhysicsEngine]: {obj.Name} - Pos: {obj.Position}, Vel: {physicsObj.Velocity}, OnGround: {isOnGround}"
                    );
                }
            }
        }

        private bool IsOnGround(ICollidable moving)
        {
            if (moving == null)
                return false;

            GameObject movingObj = moving as GameObject;

            // Check slightly below the object to see if it's touching ground
            Rectangle belowBounds = new Rectangle(
                (int)movingObj.Position.X,
                (int)movingObj.Position.Y + (int)movingObj.Height + 2,
                (int)movingObj.Width,
                2
            );

            foreach (ICollidable obstacle in Obstacles)
            {
                if (obstacle == moving)
                    continue;

                if (belowBounds.Intersects(obstacle.GetBounds()))
                {
                    return true;
                }
            }

            return false;
        }

        private bool CanMoveHorizontally(ICollidable moving, Vector2 newPosition)
        {
            if (moving == null)
                return true;

            GameObject movingObj = moving as GameObject;

            // Create a test rectangle at the new horizontal position
            Rectangle testBounds = new Rectangle(
                (int)newPosition.X,
                (int)movingObj.Position.Y,
                (int)movingObj.Width,
                (int)movingObj.Height
            );

            // Check collision with all obstacles
            foreach (ICollidable obstacle in Obstacles)
            {
                if (obstacle == moving)
                    continue;

                // Strict collision check - no overlap allowed
                if (testBounds.Intersects(obstacle.GetBounds()))
                {
                    return false;
                }
            }

            return true;
        }

        private bool CanMoveVertically(ICollidable moving, Vector2 newPosition)
        {
            if (moving == null)
                return true;

            GameObject movingObj = moving as GameObject;

            // Create a test rectangle at the new vertical position
            Rectangle testBounds = new Rectangle(
                (int)movingObj.Position.X,
                (int)newPosition.Y,
                (int)movingObj.Width,
                (int)movingObj.Height
            );

            // Check collision with all obstacles
            foreach (ICollidable obstacle in Obstacles)
            {
                if (obstacle == moving)
                    continue;

                // Strict collision check - no overlap allowed
                if (testBounds.Intersects(obstacle.GetBounds()))
                {
                    return false;
                }
            }

            return true;
        }

        public void RegisterGameObject(GameObject obj)
        {
            if (obj is IPhysicsEnabled)
            {
                objects.Add(obj);
                Debug.WriteLine($"[PhysicsEngine]: Added Physics object {obj.Name}");
            }
        }

        public void DeregisterGameObject(GameObject obj)
        {
            if (objects.Contains(obj))
            {
                objects.Remove(obj);
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
