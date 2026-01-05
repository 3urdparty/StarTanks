using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;

namespace SpaceTanks
{
    public class HomingMissilePhysics : MissilePhysics
    {
        public void Seek(Vector2 desiredDir, float desiredAngle)
        {
            var dir = new nkast.Aether.Physics2D.Common.Vector2(desiredDir.X, desiredDir.Y);

            float thrust = 7.5f;

            Body.ApplyForce(dir * thrust);

            Body.Rotation = desiredAngle;

            float maxSpeed = 5f;
            var v = Body.LinearVelocity;
            if (v.LengthSquared() > maxSpeed * maxSpeed)
            {
                v.Normalize();
                Body.LinearVelocity = v * maxSpeed;
            }
        }

        public override void Update(GameObject gameObject)
        {
            base.Update(gameObject);
            HomingMissile missile = (HomingMissile)gameObject;
            if (missile.IsSeeking)
                Seek(missile.DesiredDir, missile.DesiredAngle);
        }
    }

    public class HomingMissile : Missile
    {
        public Vector2 DesiredDir { get; private set; }
        public float DesiredAngle { get; private set; }

        private float _seekDelay = 0.5f;
        private float _seekTimer = 0f;
        public bool IsSeeking { protected set; get; } = false;

        public Tank Target { set; get; }

        public HomingMissile()
            : base()
        {
            Name = "homing-missile";
        }

        public override void Initialize(ContentManager content)
        {
            base.Initialize(content);
            TextureAtlas atlas = TextureAtlas.FromFile(content, "atlas.xml");
            _sprite = atlas.GetRegion("missile-2");
            Origin = new Vector2(_sprite.Width, _sprite.Height) * 0.5f;
            Width = _sprite.Width;
            Height = _sprite.Height;

            _explosionAnimation = atlas.GetAnimation("explosion-anim");
            _explosionAnimation.Loop = false;
        }

        public override void Update(GameTime gameTime)
        {
            base.Update(gameTime);

            float dt = (float)gameTime.ElapsedGameTime.TotalSeconds;

            _seekTimer += dt;

            if (!IsSeeking && _seekTimer >= _seekDelay)
                IsSeeking = true;

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
