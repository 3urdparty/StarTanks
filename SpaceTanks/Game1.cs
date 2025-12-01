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
    private ProceduralWorld _world;
    private Tank _tank;
    private GraphicsDeviceManager _graphics;
    // In your Game class
    List<Bullet> bullets = new List<Bullet>();

    private const float MOVEMENT_SPEED = 5.0f;

    public Game1() : base("Space Tanks", 1280, 720, false)
    {
    }

    protected override void Initialize()
    {
        base.Initialize();
        
        // Create tank after Content is available
        _tank = new Tank(Content);
        _tank.SetPosition(new Vector2(300, 0));
        _tank.CenterOrigin();
    }

    protected override void LoadContent()
    {
        // Create world: 100 tiles wide, 30 tiles tall, 32 pixels per tile
        _world = new ProceduralWorld(Content, 100, 30, 16);

        _world.Generate(10); // Use seed for consistent level
_world.DebugDrawCollisions = true; // toggle debug view on
    }

    protected override void Update(GameTime gameTime)
    {
        if (GamePad.GetState(PlayerIndex.One).Buttons.Back == ButtonState.Pressed || 
            Keyboard.GetState().IsKeyDown(Keys.Escape))
            Exit();

        float deltaTime = (float)gameTime.ElapsedGameTime.TotalSeconds;
        KeyboardState keyboardState = Keyboard.GetState();

        // Tank movement
        if (keyboardState.IsKeyDown(Keys.A))
        {
            _tank.MoveLeft();
        }
        if (keyboardState.IsKeyDown(Keys.D))
        {
            _tank.MoveRight();
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
            Bullet newBullet = _tank.Shoot();
            if (newBullet != null)
            {
                bullets.Add(newBullet);
            }
        }

          for (int i = bullets.Count - 1; i >= 0; i--)
          {
              bullets[i].Update(deltaTime, _world);


              if (bullets[i].IsOutOfBounds(_world.WorldWidth * 16 , _world.WorldHeight * 16))
              {

                  bullets[i].Deactivate();
                  bullets.RemoveAt(i);
              }
          }



        _tank.Update(gameTime, _world);
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
        _world.Draw(SpriteBatch, cameraBounds);

        // Draw tank
        _tank.Draw(SpriteBatch);
 
        // Draw all bullets
        foreach (var bullet in bullets)
        {
            bullet.Draw(SpriteBatch);
        }

        SpriteBatch.End();

        base.Draw(gameTime);
    }
}
