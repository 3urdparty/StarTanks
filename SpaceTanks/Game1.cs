using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using MonoGameLibrary;
using MonoGameLibrary.Graphics;
using System.Collections.Generic;
namespace SpaceTanks;
using System;
public class Game1 : Core
{
    // private ProceduralWorld _world;
    private Tank _tank;
    private Platform _platform;
    private GraphicsDeviceManager _graphics;
    private CollisionEngine collisionEngine;
    private PhysicsEngine physicsEngine;
    // In your Game class
    List<Projectile> projectiles = new List<Projectile>();
    public Game1() : base("Space Tanks", 1280, 720, false)
    {
    }
    void HandleCollision(ICollidable a, ICollidable b)
    {
      IPhysicsEnabled obj = (IPhysicsEnabled) a;
      // obj.Velocity = new Vector2(0,0);
      // obj.Acceleration = new Vector2(0,0);
    }
    protected override void Initialize()
    {
        base.Initialize();
        _tank = new Tank(Content);
        _tank.Position = new Vector2(300, 0);
        _platform = new Platform(Content, GraphicsDevice);
        _platform.CreatePlatform(new Vector2(300, 300), 30, 6, 32); // 30 tiles wide, 6 tiles tall, 32 pixels per tile
        collisionEngine = new CollisionEngine();
        collisionEngine.RegisterCollidableObject(_tank);
        collisionEngine.RegisterCollidableObject(_platform);

        //
        collisionEngine.Listen( _tank, _platform,
                                CollisionEngine.AABB, HandleCollision);
        physicsEngine = new PhysicsEngine();
        physicsEngine.RegisterGameObject(_tank);
        physicsEngine.Enabled = true;

        physicsEngine = new PhysicsEngine();
        physicsEngine.RegisterGameObject(_tank);
        physicsEngine.RegisterObstacle(_platform);  // Register platform as obstacle
        physicsEngine.Enabled = true;
    }
    protected override void LoadContent()
    {
        // Create world: 100 tiles wide, 30 tiles tall, 32 pixels per tile
        // _world = new ProceduralWorld(Content, 100, 30, 16);
        // _world.Generate(10); // Use seed for consistent level
        // _world.DebugDrawCollisions = true; // toggle debug view on
    }
    protected override void Update(GameTime gameTime)
    {
        if (GamePad.GetState(PlayerIndex.One).Buttons.Back == ButtonState.Pressed || 
            Keyboard.GetState().IsKeyDown(Keys.Escape))
            Exit();
        float deltaTime = (float)gameTime.ElapsedGameTime.TotalSeconds;
        KeyboardState keyboardState = Keyboard.GetState();
        
        // Tank movement - only apply acceleration when key is held
        if (keyboardState.IsKeyDown(Keys.A))
        {
            _tank.MoveLeft();
        }
        else if (keyboardState.IsKeyDown(Keys.D))
        {
            _tank.MoveRight();
        }
        else
        {
            // Stop accelerating when no movement key is pressed
            _tank.StopMoving();
        }
        
        // Gun rotation
        if (keyboardState.IsKeyDown(Keys.Left))
        {
            _tank.RotateGunLeft(deltaTime);
        }
        if (keyboardState.IsKeyDown(Keys.Right))
        {
            _tank.RotateGunRight(deltaTime);
        }
        if (keyboardState.IsKeyDown(Keys.Space))
        {
            // When tank shoots
            Projectile projectile = _tank.Shoot();
            // collisionEngine.RegisterCollidableObject(newBullet);
            // collisionEngine.Listen(newBullet, _platform,
            //                         CollisionEngine.AABB, HandleCollision);
            physicsEngine.RegisterGameObject(projectile);
            // physicsEngine.RegisterObstacle(newBullet);  // Register platform as obstacle

            if (projectile != null)
            {
                projectiles.Add(projectile);
            }
        }
        
        // for (int i = bullets.Count - 1; i >= 0; i--)
        // {
        //     bullets[i].Update(deltaTime, _world);
        //
        //
        //     if (bullets[i].IsOutOfBounds(_world.WorldWidth * 16 , _world.WorldHeight * 16))
        //     {
        //
        //         bullets[i].Deactivate();
        //         bullets.RemoveAt(i);
        //     }
        // }
        
        _tank.Update(gameTime);
        _platform.Update(gameTime);
        collisionEngine.Update();
        physicsEngine.Update(gameTime);
        base.Update(gameTime);


    }
    protected override void Draw(GameTime gameTime)
    {
        GraphicsDevice.Clear(Color.CornflowerBlue);
        SpriteBatch.Begin(samplerState: SamplerState.PointClamp);
        // Draw world with camera bounds (entire screen for now)
        Rectangle cameraBounds = new Rectangle(0, 0, 
            GraphicsDevice.Viewport.Width, 
            GraphicsDevice.Viewport.Height);
        // _world.Draw(SpriteBatch, cameraBounds);
        // Draw tank
        _tank.Draw(SpriteBatch);
 
        _platform.Draw(SpriteBatch);
        // Draw all bullets
        foreach (var projectile in projectiles)
        {
            projectile.Draw(SpriteBatch);
        }
        SpriteBatch.End();
        base.Draw(gameTime);
    }
}
