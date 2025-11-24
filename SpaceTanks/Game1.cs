using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

using MonoGameLibrary;
using MonoGameLibrary.Graphics;

namespace SpaceTanks;

public class Game1 : Core
{

    private Sprite _missile;
    private AnimatedSprite _explosion;

    // private Sprite _character;
    private GraphicsDeviceManager _graphics;
    private SpriteBatch _spriteBatch;

    public Game1() : base("Space Tanks", 1280, 720, false)
    {
    }

    protected override void Initialize()
    {
        // TODO: Add your initialization logic here

        base.Initialize();
    }

    protected override void LoadContent()
    {

        TextureAtlas atlas = TextureAtlas.FromFile(Content, "images/atlas-definition.xml");
        
        _missile = atlas.CreateSprite("missile_1");
        _missile.Scale = new Vector2(2.0f, 2.0f);

        // Create the slime animated sprite from the atlas.
        _explosion = atlas.CreateAnimatedSprite("explosion-animation");
        _explosion.Scale = new Vector2(2.0f, 2.0f);

    }

    protected override void Update(GameTime gameTime)
    {
        if (GamePad.GetState(PlayerIndex.One).Buttons.Back == ButtonState.Pressed || Keyboard.GetState().IsKeyDown(Keys.Escape))
            Exit();

        _explosion.Update(gameTime);

        base.Update(gameTime);
    }

    protected override void Draw(GameTime gameTime)
    {
        GraphicsDevice.Clear(Color.CornflowerBlue);

        SpriteBatch.Begin(samplerState: SamplerState.PointClamp);

        _missile.Draw(SpriteBatch, Vector2.Zero);

        _explosion.Draw(SpriteBatch, new Vector2(_missile.Width + 10, 0));

        SpriteBatch.End();

        base.Draw(gameTime);
    }
}
