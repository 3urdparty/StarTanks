using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using MonoGameLibrary;
using MonoGameLibrary.Graphics;
using nkast.Aether.Physics2D.Dynamics;
using nkast.Aether.Physics2D.Dynamics;
using nkast.Aether.Physics2D.Dynamics.Joints;
using AetherVector2 = nkast.Aether.Physics2D.Common.Vector2;

public enum AmmunitionType
{
    Missile,
    Blaster,
}

namespace SpaceTanks
{
    public class TankPhysics : PhysicsEntity
    {
        public Body ChassisBody { get; private set; }
        public Body TurretBody { get; private set; }

        private RevoluteJoint _turretJoint;
        private float _motorSpeed = 0f;
        private const float MotorTorque = 50f;
        private const float TurretMass = 1f;
        private const float ChassisMass = 10f;

        public override List<Body> GetBodies()
        {
            return [ChassisBody, TurretBody];
        }

        public override void Update(GameObject gameObject)
        {
            Tank tank = (Tank)gameObject;
            if (ChassisBody != null)
            {
                tank.Position = new Vector2(
                    ChassisBody.Position.X * 100f,
                    ChassisBody.Position.Y * 100f
                );

                tank.Rotation = ChassisBody.Rotation;

                tank.Gun.Position = new Vector2(
                    TurretBody.Position.X * 100f,
                    TurretBody.Position.Y * 100f
                );

                tank.Gun.Rotation = TurretBody.Rotation;
            }
        }

        public void Initialize(World world, Tank tank)
        {
            AetherVector2 chassisPhysicsPos = new AetherVector2(
                tank.Position.X / 100f,
                tank.Position.Y / 100f
            );

            ChassisBody = world.CreateBody(chassisPhysicsPos, 0, BodyType.Dynamic);
            ChassisBody.Tag = "TankChassis";
            var chassisFixture = ChassisBody.CreateRectangle(
                tank.Width / 100f,
                tank.Height / 100f,
                10f,
                AetherVector2.Zero
            );
            chassisFixture.Friction = 0.5f;
            chassisFixture.Restitution = 0.1f;

            AetherVector2 turretPhysicsPos = new AetherVector2(
                tank.Position.X / 100f,
                (tank.Position.Y - 0) / 100f
            );

            TurretBody = world.CreateBody(turretPhysicsPos, 0, BodyType.Dynamic);
            TurretBody.Tag = "TurretBody";

            // Increase angular damping to resist rotation
            TurretBody.AngularDamping = 5f; // High damping makes it stiffer
            TurretBody.LinearDamping = 2f; // Also resist linear movement

            TurretBody.Rotation = -MathHelper.PiOver2; // -90 degrees
            var turretFixture = TurretBody.CreateRectangle(
                tank.Gun.Width / 100f,
                tank.Gun.Height / 100f,
                TurretMass * 5f, // Increase mass for more inertia
                new AetherVector2(7f / 100, 0 / 100f)
            );
            turretFixture.Friction = 0.2f;
            turretFixture.Restitution = 0.1f;

            // Connect turret to chassis with revolute joint
            AetherVector2 jointAnchor = new AetherVector2(0 / 100f, 0 / 100f);
            _turretJoint = new RevoluteJoint(ChassisBody, TurretBody, jointAnchor)
            {
                MotorEnabled = true,
                MotorSpeed = 0f,
                MaxMotorTorque = 500f, // Much higher torque for stiffer joint
                LimitEnabled = true,
                LowerLimit = -MathHelper.Pi * 2 / 8,
                UpperLimit = MathHelper.Pi * 2 / 8,
            };

            world.Add(_turretJoint);
            base.Initialize(world, tank);
        }

        public void ApplyForce(AetherVector2 force)
        {
            ChassisBody.ApplyForce(force);
        }

        public void DampenMovement()
        {
            AetherVector2 velocity = ChassisBody.LinearVelocity;
            ChassisBody.LinearVelocity = new AetherVector2(velocity.X * 0.8f, velocity.Y);
        }

        public void RotateTurret(float direction)
        {
            _motorSpeed = direction * 3f;
            _turretJoint.MotorSpeed = _motorSpeed;
        }

        public void StopRotatingTurret()
        {
            _motorSpeed = 0f;
            _turretJoint.MotorSpeed = 0f;
        }

        public void ApplyRecoil(float gunAngle)
        {
            float recoilAngle = gunAngle + MathHelper.Pi;
            float recoilForce = 50f;

            AetherVector2 recoilImpulse = new AetherVector2(
                (float)System.Math.Cos(recoilAngle) * recoilForce,
                (float)System.Math.Sin(recoilAngle) * recoilForce
            );

            TurretBody.ApplyLinearImpulse(recoilImpulse);
        }

        public Vector2 GetChassisPosition()
        {
            return new Vector2(ChassisBody.Position.X * 100f, ChassisBody.Position.Y * 100f);
        }

        public Vector2 GetTurretPosition()
        {
            return new Vector2(TurretBody.Position.X * 100f, TurretBody.Position.Y * 100f);
        }

        public float GetTurretRotation()
        {
            return TurretBody.Rotation;
        }

        public float GetChassisRotation()
        {
            return ChassisBody.Rotation;
        }
    }

    public class Gun : GameObject
    {
        private TextureRegion _gun;

        private TimeSpan _elapsed;
        private TimeSpan _shootCooldown;

        private float _gunLength => _gun.Width;

        private Animation _recoil_animation;

        private static readonly TimeSpan RELOAD_DELAY = TimeSpan.FromSeconds(1);

        private bool _isRecoiling;

        public void Initialize(ContentManager content)
        {
            TextureAtlas atlas = TextureAtlas.FromFile(content, "atlas.xml");
            _gun = atlas.GetRegion("gun");
            _recoil_animation = atlas.GetAnimation("recoil-anim");
            _recoil_animation.Loop = false;
            Origin = new Vector2(_gun.Width, _gun.Height) * 0.5f;
            Width = _gun.Width;
            Height = _gun.Height;
        }

        public override void Draw(SpriteBatch spriteBatch)
        {
            _gun.Draw(
                spriteBatch,
                Position,
                Color,
                Rotation,
                new Vector2(0, _gun.Height * 0.5f),
                Scale,
                Effects,
                LayerDepth + 0.02f
            );

            if (_isRecoiling)
            {
                TextureRegion recoilFrame = _recoil_animation.CurrentFrame;
                Vector2 gunTip = new Vector2(
                    Position.X + (float)Math.Cos(Rotation) * (_gunLength + 6.0f),
                    Position.Y + (float)Math.Sin(Rotation) * (_gunLength + 6.0f)
                );
                recoilFrame.Draw(
                    spriteBatch,
                    gunTip,
                    Color.White,
                    Rotation,
                    new Vector2(recoilFrame.Width * 0.5f, recoilFrame.Height * 0.5f),
                    Scale,
                    Effects,
                    LayerDepth + 0.05f
                );
            }

            // Draw collision bounds (debug)
        }

        public override void Update(GameTime gameTime)
        {
            _elapsed += gameTime.ElapsedGameTime;
            UpdateAnimationFrame(_elapsed);
        }

        private void UpdateAnimationFrame(TimeSpan elapsedTime)
        {
            float deltaTime = (float)elapsedTime.TotalSeconds;
            if (_isRecoiling)
            {
                _recoil_animation.Update(deltaTime);
                if (_recoil_animation.HasFinished)
                    _isRecoiling = false;
            }
        }

        public bool CanShoot()
        {
            if (_elapsed < _shootCooldown)
                return false;
            return true;
        }

        public void Shoot()
        {
            _shootCooldown = _elapsed + RELOAD_DELAY;
            _isRecoiling = true;
            _recoil_animation.Reset();
        }
    }

    public class Tank : GameObject
    {
        private float _redFlashTimer = 0f;
        private AmmunitionType _ammunitionType;
        private int _currentFrame;
        private TimeSpan _elapsed;
        private readonly ContentManager _content;
        private TextureRegion _body;

        // private TextureRegion _hull_bar_outer;
        // private TextureRegion _hull_bar_inner;
        private TextureRegion _turret;
        private Animation _body_animation;

        private TimeSpan _damageCooldown;
        public Gun Gun;
        public HealthBar HealthBar;

        private static readonly TimeSpan DAMAGE_DELAY = TimeSpan.FromSeconds(0.1);
        public bool IsTakingDamage => _elapsed < _damageCooldown;

        private float Health { set; get; } = 100;

        public Tank(ContentManager content, AmmunitionType type = AmmunitionType.Missile)
        {
            _ammunitionType = type;
            _content = content;
            Position = new Vector2(100f, 100f);
            Name = "tank";

            TextureAtlas atlas = TextureAtlas.FromFile(_content, "atlas.xml");
            _body_animation = atlas.GetAnimation("tank-green-moving");
            _body = atlas.GetRegion("tank-green-1");
            // _hull_bar_outer = atlas.GetRegion("hull-bar-outer");
            // _hull_bar_inner = atlas.GetRegion("hull-bar-inner");
            _turret = atlas.GetRegion("turret");
            Gun = new Gun();
            HealthBar = new HealthBar();
            Gun.Initialize(content);
            HealthBar.Initialize(content);

            Origin = new Vector2(_body.Width, _body.Height) * 0.5f;
            Width = _body.Width;
            Height = _body.Height;
            Gun.Position = new Vector2(Width / 2f, Height - 30f);
        }

        public override void Update(GameTime gameTime)
        {
            _elapsed += gameTime.ElapsedGameTime;

            UpdateAnimationFrame(_elapsed);
            Gun.Update(gameTime);

            // Sync position from physics body
        }

        public void TakeHealth(float health)
        {
            if (!IsTakingDamage)
            {
                _damageCooldown = _elapsed + DAMAGE_DELAY;
            }

            Health -= health;
            HealthBar.Value = Health;
        }

        private void UpdateAnimationFrame(TimeSpan elapsedTime)
        {
            float deltaTime = (float)elapsedTime.TotalSeconds;

            // Update body animation
            if (elapsedTime >= _body_animation.Delay)
            {
                _currentFrame++;
                if (_currentFrame >= _body_animation.Frames.Count)
                    _currentFrame = 0;
                _body = _body_animation.Frames[_currentFrame];
            }
        }

        public override void Draw(SpriteBatch spriteBatch)
        {
            Color tint = Color.White;

            if (IsTakingDamage)
            {
                tint = Color.Lerp(Color.White, Color.Red, 0.5f);
            }

            Gun.Draw(spriteBatch);
            HealthBar.Position = Position + new Vector2(0, -Height - 10f);
            HealthBar.Scale = new Vector2(0.5f, 0.5f);
            HealthBar.Draw(spriteBatch);
            // Draw turret
            _turret.Draw(
                spriteBatch,
                Position,
                Color,
                0,
                new Vector2(_turret.Width * 0.5f, _turret.Height * 0.5f),
                Scale,
                Effects,
                LayerDepth + 0.01f
            );

            // Draw body
            _body.Draw(spriteBatch, Position, tint, 0f, Origin, Scale, Effects, LayerDepth);

            // Draw collision bounds (debug)
        }
    }

    public class HealthBar : GameObject
    {
        private TextureRegion _hullBarOuter;
        private TextureRegion _hullBarInner;

        public float Value = 100f; // 0..100

        public void Initialize(ContentManager content)
        {
            TextureAtlas atlas = TextureAtlas.FromFile(content, "atlas.xml");

            _hullBarOuter = atlas.GetRegion("hull-bar-outer");
            _hullBarInner = atlas.GetRegion("hull-bar-inner");

            Origin = new Vector2(_hullBarOuter.Width, _hullBarOuter.Height) * 0.5f;
            Width = _hullBarOuter.Width;
            Height = _hullBarOuter.Height;
        }

        public override void Draw(SpriteBatch spriteBatch)
        {
            // Clamp health
            float health01 = MathHelper.Clamp(Value / 100f, 0f, 1f);

            // Draw outer frame
            _hullBarOuter.Draw(
                spriteBatch,
                Position,
                Color.White,
                0f,
                new Vector2(_hullBarOuter.Width * 0.5f, _hullBarOuter.Height * 0.5f),
                Scale,
                Effects,
                LayerDepth
            );

            if (health01 <= 0f)
                return;

            // Compute clipped width
            int fullWidth = _hullBarInner.Width;
            int clippedWidth = (int)(fullWidth * health01);

            Rectangle sourceRect = new Rectangle(0, 0, clippedWidth, _hullBarInner.Height);

            // Adjust origin so left side stays fixed
            Vector2 innerOrigin = new Vector2(
                _hullBarInner.Width * 0.5f,
                _hullBarInner.Height * 0.5f
            );

            // Offset position so bar drains left → right
            Vector2 offset = new Vector2(-(fullWidth - clippedWidth) * 0.5f * Scale.X, 0f);

            _hullBarInner.Draw(
                spriteBatch,
                Position + offset,
                Color.White,
                0f,
                innerOrigin,
                Scale,
                Effects,
                LayerDepth + 0.001f
            );
        }

        public override void Update(GameTime gameTime)
        {
            // No animation needed unless you want smoothing
        }
    }
}
