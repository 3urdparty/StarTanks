using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using MonoGameLibrary;
using MonoGameLibrary.Graphics;

namespace SpaceTanks;

public class Game1 : Core
{
    private ProceduralWorld _world;
    private Tank _tank;
    private GraphicsDeviceManager _graphics;
    private SpriteBatch _spriteBatch;

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
        _world = new ProceduralWorld(Content, 100, 30, 32);
        _world.Generate(10); // Use seed for consistent level
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

        SpriteBatch.End();

        base.Draw(gameTime);
    }
}
