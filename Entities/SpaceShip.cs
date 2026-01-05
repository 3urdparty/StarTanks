using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;

namespace SpaceTanks;

public class SpaceShip
{
    private int _currentFrame;
    private TimeSpan _elapsed;

    private readonly ContentManager _content;
    private TextureRegion _body;
    private Animation _thruster_flame_animation;
    private TextureRegion _thruster;

    public Color Color { get; set; } = Color.White;

    public float Rotation { get; set; } = 0.0f;

    public Vector2 Scale { get; set; } = Vector2.One;

    public Vector2 Origin { get; set; } = Vector2.Zero;

    public SpriteEffects Effects { get; set; } = SpriteEffects.None;

    public float LayerDepth { get; set; } = 0.0f;

    public float Width => _body.Width * Scale.X;

    public float Height => _body.Height * Scale.Y;

    public void CenterOrigin()
    {
        Origin = new Vector2(_body.Width, _body.Height) * 0.5f;
    }

    public void Draw(SpriteBatch spriteBatch, Vector2 position)
    {
        _body.Draw(spriteBatch, position, Color, Rotation, Origin, Scale, Effects, LayerDepth);

        Vector2 pos = new Vector2(16.0f, 16.0f);
        Vector2 thruster_pos = new Vector2(16.0f, 16.0f);
        _thruster.Draw(
            spriteBatch,
            new Vector2(position.X + 13f, position.Y + 24f), 
            Color,
            MathHelper.Pi, 
            new Vector2(16f, 16f), 
            Scale,
            Effects,
            LayerDepth
        );
    }

    public SpaceShip(ContentManager content)
    {
        _content = content;

        TextureAtlas atlas = TextureAtlas.FromFile(_content, "images/atlas-definition.xml");

        _thruster_flame_animation = atlas.GetAnimation("thruster-flame-animation");
        _body = atlas.GetRegion("spaceship-green-1");
        _thruster = atlas.GetRegion("thruster-flame-1");
    }

    public Animation Animation
    {
        get => _thruster_flame_animation;
        set
        {
            _thruster_flame_animation = value;
            _thruster = _thruster_flame_animation.Frames[0];
        }
    }

    public void Update(GameTime gameTime)
    {
        _elapsed += gameTime.ElapsedGameTime;

        if (_elapsed >= _thruster_flame_animation.Delay)
        {
            _elapsed -= _thruster_flame_animation.Delay;
            _currentFrame++;

            if (_currentFrame >= _thruster_flame_animation.Frames.Count)
            {
                _currentFrame = 0;
            }

            _thruster = _thruster_flame_animation.Frames[_currentFrame];
        }
    }
}
