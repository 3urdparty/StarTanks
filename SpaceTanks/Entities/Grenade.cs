using System;
using System;
using System;
using System.Collections.Generic;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using MonoGameLibrary;
using MonoGameLibrary;
using MonoGameLibrary.Graphics;
using MonoGameLibrary.Graphics;
using MonoGameLibrary.Graphics;
using nkast.Aether.Physics2D.Dynamics;
using nkast.Aether.Physics2D.Dynamics;
using nkast.Aether.Physics2D.Dynamics;
using nkast.Aether.Physics2D.Dynamics;
using nkast.Aether.Physics2D.Dynamics.Contacts;
using nkast.Aether.Physics2D.Dynamics.Joints;
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

            // ---- Bounce tuning ----
            fixture.Restitution = 0.75f; // low bounce
            fixture.Friction = 0.01f; // Almost no energy loss on contact

            // Prevent damping from killing bounce
            Body.LinearDamping = 0f;
            Body.AngularDamping = 0f;

            // Optional but recommended for fast projectiles
            Body.IsBullet = true;

            // Initial velocity
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

        // Stop motion immediately if your Projectile has velocity/physics.
        // Adjust these lines to match your implementation.
        // Velocity = Vector2.Zero; // if you have Velocity
        // If you have a physics Body:
        // Body.LinearVelocity = Vector2.Zero;
        // Body.AngularVelocity = 0f;
        // Body.Enabled = false; // or remove body from world
    }

    public class Grenade : Projectile
    {
        // Beep "animation" frames (manual timing so we can change speed)
        private TextureRegion _beepFrameA;
        private TextureRegion _beepFrameB;
        private int _beepIndex = 0;
        private float _beepTimer = 0f;

        // Explosion (your existing Animation API)
        private Animation _explosionAnim;
        private bool _isExploding = false;

        // Fuse
        private bool _countdownStarted = false;
        private float _fuseSeconds = 2.0f;
        private float _fuseRemaining = -1f;

        // Beep timing (seconds per frame)
        // Before countdown (idle beep): slow
        private const float BEEP_IDLE_FRAME_TIME = 0.20f; // ~200ms per frame

        // During countdown: speeds up as it approaches zero
        private const float BEEP_MIN_FRAME_TIME = 0.05f; // fastest near detonation (50ms)
        private const float BEEP_MAX_FRAME_TIME = 0.20f; // slowest right when countdown starts

        public Grenade()
            : base()
        {
            Name = "grenade";
        }

        public void Initialize(ContentManager content)
        {
            base.Initialize(content);

            TextureAtlas atlas = TextureAtlas.FromFile(content, "atlas.xml");

            // Use the same regions referenced by your grenade-beeping-anim
            _beepFrameA = atlas.GetRegion("red-grenade-1");
            _beepFrameB = atlas.GetRegion("red-grenade-2");

            // Size/origin from a representative frame
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

            // Countdown
            if (_countdownStarted && _fuseRemaining >= 0f)
            {
                _fuseRemaining -= dt;
                if (_fuseRemaining <= 0f)
                {
                    Explode();
                    return;
                }
            }

            // Update beep frames with variable speed
            UpdateBeep(dt);
        }

        private void UpdateBeep(float dt)
        {
            float frameTime = GetCurrentBeepFrameTime();

            _beepTimer += dt;
            while (_beepTimer >= frameTime)
            {
                _beepTimer -= frameTime;
                _beepIndex = 1 - _beepIndex; // toggles 0<->1
            }
        }

        // Returns seconds per frame (smaller = faster)
        private float GetCurrentBeepFrameTime()
        {
            if (!_countdownStarted || _fuseSeconds <= 0f || _fuseRemaining < 0f)
                return BEEP_IDLE_FRAME_TIME;

            // t = 0 at countdown start, t = 1 at detonation
            float t = 1f - MathHelper.Clamp(_fuseRemaining / _fuseSeconds, 0f, 1f);

            // Smooth the ramp so it doesn't feel linear/mechanical
            // (ease-in: slow at first, fast near end)
            float eased = t * t;

            // Interpolate from max -> min
            return MathHelper.Lerp(BEEP_MAX_FRAME_TIME, BEEP_MIN_FRAME_TIME, eased);
        }

        public override void Draw(SpriteBatch spriteBatch)
        {
            if (_isExploding)
            {
                DrawFrame(spriteBatch, _explosionAnim.CurrentFrame);
                return;
            }

            TextureRegion frame = (_beepIndex == 0) ? _beepFrameA : _beepFrameB;
            DrawFrame(spriteBatch, frame);
        }

        private void DrawFrame(SpriteBatch spriteBatch, TextureRegion frame)
        {
            if (frame == null)
                return;

            frame.Draw(
                spriteBatch,
                Position,
                Color,
                Rotation,
                new Vector2(frame.Width * 0.5f, frame.Height * 0.5f),
                Vector2.One,
                Effects,
                LayerDepth
            );
        }

        public void StartCountDown(float seconds = 2.0f)
        {
            if (_isExploding)
                return;

            _countdownStarted = true;
            _fuseSeconds = Math.Max(0.01f, seconds);
            _fuseRemaining = _fuseSeconds;

            // Optional: reset beep phase on countdown start
            _beepIndex = 0;
            _beepTimer = 0f;
        }

        public void Explode()
        {
            if (_isExploding)
                return;

            _isExploding = true;
            _fuseRemaining = -1f;

            // Optional: stop motion
            // Velocity = Vector2.Zero;

            _explosionAnim.Reset();
        }
    }
}
