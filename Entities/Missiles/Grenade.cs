using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using nkast.Aether.Physics2D.Dynamics;
using nkast.Aether.Physics2D.Dynamics.Contacts;
using AetherVector2 = nkast.Aether.Physics2D.Common.Vector2;

namespace SpaceTanks
{
    public class GrenadePhysics : ProjectilePhysics
    {
        public override void Initialize(World world, Projectile projectile)
        {
            AetherVector2 physicsPos = new AetherVector2(
                projectile.Position.X / 100f,
                projectile.Position.Y / 100f
            );

            Body = world.CreateBody(physicsPos, projectile.Rotation, BodyType.Dynamic);

            var fixture = Body.CreateRectangle(
                projectile.Width / 100f,
                projectile.Height / 100f,
                Mass,
                AetherVector2.Zero
            );

            fixture.Restitution = 0.75f;
            fixture.Friction = 0.01f;

            Body.LinearDamping = 0f;
            Body.AngularDamping = 0f;

            Body.IsBullet = true;

            float speed = 10f;
            float physicsSpeed = speed / 100f;

            Body.LinearVelocity = new AetherVector2(
                MathF.Cos(projectile.Rotation) * physicsSpeed,
                MathF.Sin(projectile.Rotation) * physicsSpeed
            );

            Body.Tag = "Grenade";
        }

        public override void Update(GameObject gameObject)
        {
            base.Update(gameObject);
        }

    }

    public class Grenade : Projectile
    {
        private TextureRegion _beepFrameA;
        private TextureRegion _beepFrameB;
        private int _beepIndex = 0;
        private float _beepTimer = 0f;

        private Animation _explosionAnim;
        private bool _isExploding = false;

        private bool _countdownStarted = false;
        private float _fuseSeconds = 2.0f;
        private float _fuseRemaining = -1f;

        private const float BEEP_IDLE_FRAME_TIME = 0.20f;
        private const float BEEP_MIN_FRAME_TIME = 0.05f;
        private const float BEEP_MAX_FRAME_TIME = 0.20f;
        private const float ExplosionDrawScale = 1.8f;
        private const float ExplosionCraterWidth = 90f;
        private const float ExplosionCraterDepth = 18f;
        private const float ExplosionDamageRadius = 85f;
        public float CraterWidth => ExplosionCraterWidth;
        public float CraterDepth => ExplosionCraterDepth;
        public float ExplosionRadius => ExplosionDamageRadius;
        public bool CountdownStarted => _countdownStarted;
        public event Action<Grenade, Vector2> Exploded;

        public Grenade()
            : base()
        {
            Name = "grenade";
        }

        public override void Initialize(ContentManager content)
        {
            base.Initialize(content);

            TextureAtlas atlas = TextureAtlas.FromFile(content, "atlas.xml");

            _beepFrameA = atlas.GetRegion("red-grenade-1");
            _beepFrameB = atlas.GetRegion("red-grenade-2");

            _sprite = _beepFrameA;
            Origin = new Vector2(_sprite.Width, _sprite.Height) * 0.5f;
            Width = _sprite.Width;
            Height = _sprite.Height;

            _explosionAnim = atlas.GetAnimation("explosion-anim");
            _explosionAnim.Loop = false;
            _explosionAnim.Reset();

            _beepIndex = 0;
            _beepTimer = 0f;
        }

        public override void Update(GameTime gameTime)
        {
            float dt = (float)gameTime.ElapsedGameTime.TotalSeconds;

            if (_isExploding)
            {
                _explosionAnim.Update(dt);
                if (_explosionAnim.HasFinished)
                    Destroyed = true;
                return;
            }

            base.Update(gameTime);

            if (_countdownStarted && _fuseRemaining >= 0f)
            {
                _fuseRemaining -= dt;
                if (_fuseRemaining <= 0f)
                {
                    Explode();
                    return;
                }
            }

            UpdateBeep(dt);
        }

        private void UpdateBeep(float dt)
        {
            float frameTime = GetCurrentBeepFrameTime();

            _beepTimer += dt;
            while (_beepTimer >= frameTime)
            {
                _beepTimer -= frameTime;
                _beepIndex = 1 - _beepIndex;
            }
        }

        private float GetCurrentBeepFrameTime()
        {
            if (!_countdownStarted || _fuseSeconds <= 0f || _fuseRemaining < 0f)
                return BEEP_IDLE_FRAME_TIME;

            float t = 1f - MathHelper.Clamp(_fuseRemaining / _fuseSeconds, 0f, 1f);

            float eased = t * t;

            return MathHelper.Lerp(BEEP_MAX_FRAME_TIME, BEEP_MIN_FRAME_TIME, eased);
        }

        public override void Draw(SpriteBatch spriteBatch)
        {
            if (_isExploding)
            {
                DrawFrame(spriteBatch, _explosionAnim.CurrentFrame, ExplosionDrawScale);
                return;
            }

            TextureRegion frame = (_beepIndex == 0) ? _beepFrameA : _beepFrameB;
            DrawFrame(spriteBatch, frame);
        }

        private void DrawFrame(SpriteBatch spriteBatch, TextureRegion frame, float scale = 1f)
        {
            if (frame == null)
                return;

            frame.Draw(
                spriteBatch,
                Position,
                Color,
                Rotation,
                new Vector2(frame.Width * 0.5f, frame.Height * 0.5f),
                new Vector2(scale, scale),
                Effects,
                LayerDepth
            );
        }

        public void StartCountDown(float seconds = 2.0f)
        {
            if (_isExploding || _countdownStarted)
                return;

            _countdownStarted = true;
            _fuseSeconds = Math.Max(0.01f, seconds);
            _fuseRemaining = _fuseSeconds;

            _beepIndex = 0;
            _beepTimer = 0f;
        }

        public void Explode()
        {
            if (_isExploding)
                return;

            _isExploding = true;
            _fuseRemaining = -1f;

            if (PhysicsEntityRef is GrenadePhysics physics && physics.Body != null)
            {
                physics.Body.LinearVelocity = AetherVector2.Zero;
                physics.Body.AngularVelocity = 0f;
                physics.Body.BodyType = BodyType.Static;
            }

            _explosionAnim.Reset();
            Exploded?.Invoke(this, Position);
        }
    }
}
