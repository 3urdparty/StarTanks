using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using MonoGameLibrary;
using MonoGameLibrary.Graphics;
using AetherVector2 = nkast.Aether.Physics2D.Common.Vector2;

namespace SpaceTanks
{
    public class MinePhysics : ProjectilePhysics
    {
        public override void Update(GameObject gameObject)
        {
            base.Update(gameObject);
        }
    }

    public class Mine : Missile
    {
        public Vector2 DesiredDir { get; private set; }
        public float DesiredAngle { get; private set; }

        private float _seekDelay = 0.5f; // seconds before homing activates
        private float _seekTimer = 0f;
        public bool IsSeeking { protected set; get; } = false;

        public Tank Target { set; get; }

        public Mine()
            : base()
        {
            Name = "homing-missile";
        }

        public void Initialize(ContentManager content)
        {
            base.Initialize(content);
            TextureAtlas atlas = TextureAtlas.FromFile(content, "atlas.xml");
            _sprite = atlas.GetRegion("missile-2");
            Origin = new Vector2(_sprite.Width, _sprite.Height) * 0.5f;
            Width = _sprite.Width;
            Height = _sprite.Height;

            // Load explosion animation from atlas
            _explosionAnimation = atlas.GetAnimation("explosion-anim");
            _explosionAnimation.Loop = false;
        }

        public override void Update(GameTime gameTime)
        {
            base.Update(gameTime);

            float dt = (float)gameTime.ElapsedGameTime.TotalSeconds;

            // Accumulate time since launch
            _seekTimer += dt;

            // Enable seeking after delay
            if (!IsSeeking && _seekTimer >= _seekDelay)
                IsSeeking = true;

            // Do nothing until seeking is active
            if (!IsSeeking || Target == null)
                return;

            Vector2 toTarget = Target.Position - Position;
            if (toTarget.LengthSquared() < 0.0001f)
                return;

            toTarget.Normalize();

            DesiredDir = toTarget;
            DesiredAngle = (float)Math.Atan2(toTarget.Y, toTarget.X);
        }
    }
}
