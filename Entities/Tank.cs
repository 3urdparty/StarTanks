using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using nkast.Aether.Physics2D.Dynamics;
using nkast.Aether.Physics2D.Dynamics.Joints;
using AetherVector2 = nkast.Aether.Physics2D.Common.Vector2;

public enum AmmunitionType
{
    Missile,
    Blaster,
}

public enum TankColor
{
    Green,
    Blue,
    Red,
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

            TurretBody.AngularDamping = 5f;
            TurretBody.LinearDamping = 2f;

            TurretBody.Rotation = -MathHelper.PiOver2;
            var turretFixture = TurretBody.CreateRectangle(
                tank.Gun.Width / 100f,
                tank.Gun.Height / 100f,
                TurretMass * 5f,
                new AetherVector2(7f / 100, 0 / 100f)
            );
            turretFixture.Friction = 0.2f;
            turretFixture.Restitution = 0.1f;

            AetherVector2 jointAnchor = new AetherVector2(0 / 100f, 0 / 100f);
            _turretJoint = new RevoluteJoint(ChassisBody, TurretBody, jointAnchor)
            {
                MotorEnabled = true,
                MotorSpeed = 0f,
                MaxMotorTorque = 500f,
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
        private AmmunitionType _ammunitionType;
        private int _currentFrame;
        private TimeSpan _elapsed;
        private readonly ContentManager _content;
        private readonly TankColor _color;
        private TextureRegion _body;
        private TextureRegion _turret;
        private Animation _body_animation;
        private Animation _deathExplosion;
        private bool _deathTriggered;
        private bool _exploding;
        private float _deathFlashTimer;
        private int _deathFlashStep;
        private const float DeathFlashInterval = 0.12f;
        private const int DeathFlashCount = 4;

        private TimeSpan _damageCooldown;
        public Gun Gun;
        public HealthBar HealthBar;
        private static Texture2D _trajectoryDot;
        private readonly List<Vector2> _trajectoryPoints = new();
        private const int TrajectorySamples = 96;
        private const float TrajectoryStepSeconds = 0.12f;
        private const float MissileLaunchSpeed = 240f;
        private static readonly Vector2 TrajectoryGravity = new Vector2(0f, 9.81f * 15f);
        private const float TrajectoryStartOffset = 18f;
        private float _trajectoryVisibility;
        private const float TrajectoryFadeSpeed = 1f;
        private float _trajectoryHold;
        private const float TrajectoryHoldDuration = 0.75f;
        private bool _trajectoryCommandActive;
        public bool TrajectoryPreviewEnabled { get; set; } = true;
        private SpriteFont _labelFont;
        private const float NameplateOffset = 40f;

        private static readonly TimeSpan DAMAGE_DELAY = TimeSpan.FromSeconds(0.1);
        public bool IsTakingDamage => _elapsed < _damageCooldown;

        private float Health { set; get; } = 100;
        public SoundEffect ExplosionSound { get; set; }

        public Tank(
            ContentManager content,
            AmmunitionType type = AmmunitionType.Missile,
            TankColor color = TankColor.Green,
            string playerName = ""
        )
        {
            _ammunitionType = type;
            _content = content;
            _color = color;
            Position = new Vector2(100f, 100f);
            Name = string.IsNullOrWhiteSpace(playerName) ? "tank" : playerName;

            TextureAtlas atlas = TextureAtlas.FromFile(_content, "atlas.xml");
            _body_animation = BuildBodyAnimation(atlas);
            _body = _body_animation.Frames.Count > 0 ? _body_animation.Frames[0] : null;
            if (_body == null)
                _body = atlas.GetRegion("tank-green-1");
            _turret = atlas.GetRegion("turret");
            _deathExplosion = atlas.GetAnimation("explosion-anim");
            _deathExplosion.Loop = false;
            _deathExplosion.Reset();
            Gun = new Gun();
            HealthBar = new HealthBar();
            Gun.Initialize(content);
            HealthBar.Initialize(content);
            _labelFont = _content.Load<SpriteFont>("fonts/TankLabel");

            Origin = new Vector2(_body.Width, _body.Height) * 0.5f;
            Width = _body.Width;
            Height = _body.Height;
            Gun.Position = new Vector2(Width / 2f, Height - 30f);
        }

        public override void Update(GameTime gameTime)
        {
            if (Destroyed)
                return;
            float dt = (float)gameTime.ElapsedGameTime.TotalSeconds;
            _elapsed += gameTime.ElapsedGameTime;

            if (_exploding)
            {
                _deathExplosion.Update(dt);
                if (_deathExplosion.HasFinished)
                    Destroyed = true;
                return;
            }

            UpdateAnimationFrame(_elapsed);
            Gun.Update(gameTime);
            UpdateTrajectory(gameTime);

            if (_deathTriggered)
            {
                _deathFlashTimer -= dt;
                if (_deathFlashTimer <= 0f)
                {
                    _deathFlashTimer += DeathFlashInterval;
                    _deathFlashStep++;
                    if (_deathFlashStep >= DeathFlashCount)
                        TriggerDeathExplosion();
                }
            }
        }

        public void TakeHealth(float health)
        {
            if (!IsTakingDamage)
            {
                _damageCooldown = _elapsed + DAMAGE_DELAY;
            }

            Health -= health;
            if (Health < 0f)
                Health = 0f;
            HealthBar.Value = Health;
            if (Health <= 0f && !_deathTriggered && !_exploding)
                BeginDeathSequence();
        }

        private void UpdateAnimationFrame(TimeSpan elapsedTime)
        {
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
            if (Destroyed)
                return;
            if (_exploding)
            {
                TextureRegion frame = _deathExplosion.CurrentFrame;
                if (frame != null)
                {
                    frame.Draw(
                        spriteBatch,
                        Position,
                        Color.White,
                        0f,
                        new Vector2(frame.Width * 0.5f, frame.Height * 0.5f),
                        Vector2.One,
                        Effects,
                        LayerDepth + 0.2f
                    );
                }
                return;
            }

            Color tint = Color.White;

            if (IsTakingDamage)
            {
                tint = Color.Lerp(Color.White, Color.Red, 0.5f);
            }

            if (_deathTriggered && !_exploding)
            {
                bool flashWhite = (_deathFlashStep % 2 == 0);
                tint = flashWhite ? Color.White : Color.Lerp(Color.White, Color.Black, 0.4f);
            }

            DrawTrajectory(spriteBatch);
            Gun.Color = tint;
            Gun.Draw(spriteBatch);
            HealthBar.Position = Position + new Vector2(0, -Height - 10f);
            HealthBar.Scale = new Vector2(0.5f, 0.5f);
            HealthBar.Draw(spriteBatch);
            DrawNameplate(spriteBatch);
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
            _body.Draw(spriteBatch, Position, tint, Rotation, Origin, Scale, Effects, LayerDepth);
        }

        private void BeginDeathSequence()
        {
            _deathTriggered = true;
            _deathFlashTimer = DeathFlashInterval;
            _deathFlashStep = 0;
        }

        private void TriggerDeathExplosion()
        {
            if (_exploding)
                return;
            _exploding = true;
            _deathExplosion.Reset();
            ExplosionSound?.Play();
        }

        public void SetTrajectoryInput(bool active)
        {
            _trajectoryCommandActive = active && TrajectoryPreviewEnabled;
        }

        private void UpdateTrajectory(GameTime gameTime)
        {
            if (!TrajectoryPreviewEnabled || Gun == null)
            {
                _trajectoryPoints.Clear();
                _trajectoryVisibility = 0f;
                _trajectoryHold = 0f;
                return;
            }

            float dt = (float)gameTime.ElapsedGameTime.TotalSeconds;

            if (_trajectoryCommandActive)
            {
                _trajectoryHold = TrajectoryHoldDuration;
                _trajectoryVisibility = 1f;
                RebuildTrajectoryPoints();
            }
            else
            {
                if (_trajectoryHold > 0f)
                {
                    _trajectoryHold = Math.Max(0f, _trajectoryHold - dt);
                    _trajectoryVisibility = 1f;
                }
                else
                {
                    _trajectoryVisibility = Math.Max(
                        0f,
                        _trajectoryVisibility - TrajectoryFadeSpeed * dt
                    );
                }
            }
        }

        private void RebuildTrajectoryPoints()
        {
            _trajectoryPoints.Clear();
            Vector2 muzzle = GetMuzzlePosition();
            Vector2 direction = new Vector2(
                (float)Math.Cos(Gun.Rotation),
                (float)Math.Sin(Gun.Rotation)
            );
            Vector2 start = muzzle + direction * TrajectoryStartOffset;
            _trajectoryPoints.Add(start);
            Vector2 velocity = direction * MissileLaunchSpeed;
            for (int i = 1; i <= TrajectorySamples; i++)
            {
                float t = i * TrajectoryStepSeconds;
                Vector2 sample = muzzle + velocity * t + TrajectoryGravity * (0.5f * t * t);
                _trajectoryPoints.Add(sample);
            }
        }

        private Vector2 GetMuzzlePosition()
        {
            if (Gun == null)
                return Position;
            Vector2 direction = new Vector2(
                (float)Math.Cos(Gun.Rotation),
                (float)Math.Sin(Gun.Rotation)
            );
            return Gun.Position + direction * Gun.Width;
        }

        private void DrawTrajectory(SpriteBatch spriteBatch)
        {
            if (
                !TrajectoryPreviewEnabled
                || _trajectoryPoints.Count == 0
                || _trajectoryVisibility <= 0f
            )
                return;
            Texture2D dot = GetTrajectoryTexture(spriteBatch.GraphicsDevice);
            Vector2 origin = new Vector2(dot.Width * 0.5f, dot.Height * 0.5f);
            int count = _trajectoryPoints.Count;
            for (int i = 0; i < count; i++)
            {
                float progress = i / (float)Math.Max(1, count - 1);
                float alpha = MathHelper.Lerp(0.85f, 0.1f, progress) * _trajectoryVisibility;
                float scale = MathHelper.Lerp(0.55f, 0.2f, progress);
                Color color = Color.Lerp(Color.OrangeRed, Color.Orange, progress) * alpha;
                spriteBatch.Draw(
                    dot,
                    _trajectoryPoints[i],
                    null,
                    color,
                    0f,
                    origin,
                    scale,
                    SpriteEffects.None,
                    LayerDepth + 0.06f
                );
            }
        }

        private static Texture2D GetTrajectoryTexture(GraphicsDevice device)
        {
            if (_trajectoryDot == null || _trajectoryDot.IsDisposed)
                _trajectoryDot = BuildTrajectoryTexture(device);
            return _trajectoryDot;
        }

        private static Texture2D BuildTrajectoryTexture(GraphicsDevice device)
        {
            const int size = 6;
            var texture = new Texture2D(device, size, size);
            var data = new Color[size * size];
            float center = (size - 1) * 0.5f;
            float radius = size * 0.5f;
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dx = x - center;
                    float dy = y - center;
                    float dist = MathF.Sqrt(dx * dx + dy * dy);
                    data[y * size + x] = dist <= radius ? Color.White : Color.Transparent;
                }
            }
            texture.SetData(data);
            return texture;
        }

        private Animation BuildBodyAnimation(TextureAtlas atlas)
        {
            string colorKey = GetColorKey(_color);
            var frames = new List<TextureRegion>
            {
                atlas.GetRegion($"tank-{colorKey}-1"),
                atlas.GetRegion($"tank-{colorKey}-2"),
            };
            return new Animation(frames, TimeSpan.FromMilliseconds(200));
        }

        private static string GetColorKey(TankColor color)
        {
            return color switch
            {
                TankColor.Green => "green",
                TankColor.Blue => "blue",
                TankColor.Red => "red",
                _ => throw new ArgumentOutOfRangeException(nameof(color), color, null),
            };
        }

        private void DrawNameplate(SpriteBatch spriteBatch)
        {
            if (_labelFont == null || string.IsNullOrWhiteSpace(Name))
                return;

            Vector2 textSize = _labelFont.MeasureString(Name);
            Vector2 textPosition = Position + new Vector2(0f, -Height - NameplateOffset);
            float textLayer = MathHelper.Clamp(LayerDepth + 0.15f, 0f, 1f);
            spriteBatch.DrawString(
                _labelFont,
                Name,
                textPosition,
                Color.White,
                0f,
                textSize * 0.5f,
                1f,
                SpriteEffects.None,
                textLayer
            );
        }
    }

    public class HealthBar : GameObject
    {
        private TextureRegion _hullBarOuter;
        private TextureRegion _hullBarInner;

        public float Value = 100f;

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
            float health01 = MathHelper.Clamp(Value / 100f, 0f, 1f);

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

            int fullWidth = _hullBarInner.Width;
            int clippedWidth = (int)(fullWidth * health01);

            Rectangle sourceRect = _hullBarInner.SourceRectangle;
            sourceRect.Width = clippedWidth;
            Vector2 leftAnchor = Position + new Vector2(-fullWidth * 0.5f * Scale.X, 0f);
            Vector2 innerOrigin = new Vector2(0f, _hullBarInner.Height * 0.5f);

            spriteBatch.Draw(
                _hullBarInner.Texture,
                leftAnchor,
                sourceRect,
                Color.White,
                0f,
                innerOrigin,
                Scale,
                Effects,
                LayerDepth + 0.001f
            );
        }

        public override void Update(GameTime gameTime) { }
    }
}
