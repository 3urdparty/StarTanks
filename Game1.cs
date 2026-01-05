using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using MonoGameLibrary;
using nkast.Aether.Physics2D.Common;
using nkast.Aether.Physics2D.Dynamics;
using nkast.Aether.Physics2D.Dynamics.Contacts;
using nkast.Aether.Physics2D.Dynamics.Joints;
using SpaceTanks.Extensions;
using MGVector2 = Microsoft.Xna.Framework.Vector2;
using Vector2 = nkast.Aether.Physics2D.Common.Vector2;

namespace SpaceTanks;

public class Game1 : Core
{
    private List<GameObject> _gameObjects = new List<GameObject>();
    private List<PhysicsEntity> _bodies = new List<PhysicsEntity>();
    private Tank _playerTank;
    private TankPhysics _playerTankPhysics;
    private Tank _aiTank;
    private TankPhysics _aiTankPhysics;
    private ComputerTankAgent _computerAgent;
    private Platform _platform;
    private PlatformPhysics _platformPhysics;
    private const float GrenadeRestVelocityThreshold = 0.15f;

    private Camera2D _camera;
    Sky sky;

    private World _physicsWorld;

    private readonly Queue<Action> _postStep = new();
    private readonly Dictionary<Body, Tank> _projectileOwners = new();
    private SoundEffect _explosionSound;

    public Game1()
        : base("Space Tanks", 1280, 720, false) { }

    protected override void Initialize()
    {
        base.Initialize();

        _camera = new Camera2D { Position = MGVector2.Zero };

        sky = new Sky(GraphicsDevice.Viewport.Width, GraphicsDevice.Viewport.Height, seed: 123);
        sky.Initialize(Content, "atlas.xml");
        _physicsWorld = new World(new Vector2(0, 9.81f));

        AetherBoundsRenderer.Initialize(GraphicsDevice);

        _playerTank = new Tank(Content, AmmunitionType.Missile, TankColor.Green, "Player 1");
        _playerTank.Position = new MGVector2(350, 520);
        _playerTank.TrajectoryPreviewEnabled = true;
        _playerTankPhysics = new TankPhysics();

        _playerTankPhysics.Initialize(_physicsWorld, _playerTank);
        _playerTankPhysics.OnCollision += (fixtureA, fixtureB, contact) =>
        {
            if (
                _playerTankPhysics.Contains(fixtureA.Body)
                && IsHostileProjectile(fixtureB.Body, _playerTank)
            )
            {
                _playerTank.TakeHealth(10);
            }

            return true;
        };
        _gameObjects.Add(_playerTank);
        _bodies.Add(_playerTankPhysics);

        _aiTank = new Tank(Content, AmmunitionType.Missile, TankColor.Red, "Computer");
        _aiTank.Position = new MGVector2(700, 520);
        _aiTank.TrajectoryPreviewEnabled = false;
        _aiTankPhysics = new TankPhysics();

        _aiTankPhysics.Initialize(_physicsWorld, _aiTank);

        _aiTankPhysics.OnCollision += (Fixture fixtureA, Fixture fixtureB, Contact contact) =>
        {
            if (
                _aiTankPhysics.Contains(fixtureA.Body)
                && IsHostileProjectile(fixtureB.Body, _aiTank)
            )
            {
                _aiTank.TakeHealth(10);
            }

            return true;
        };

        _gameObjects.Add(_aiTank);
        _bodies.Add(_aiTankPhysics);

        _computerAgent = new ComputerTankAgent(
            _playerTank,
            _aiTank,
            _aiTankPhysics,
            () => TryFireMissile(_aiTank, _aiTankPhysics),
            () => TryFireHomingMissile(_aiTank, _aiTankPhysics, _playerTank),
            () => TryFireGrenade(_aiTank, _aiTankPhysics)
        );

        TileMap ruleTiles = TileMap.FromFile(Content, "tile-rules.xml");
        PlatformGenerator generator = new PlatformGenerator(ruleTiles);

        var options = new PlatformGenerator.Options
        {
            Topology = PlatformTopology.RectangularHillsAndHoles,
            Seed = 1337,
        };

        PlatformDefinition def = generator.CreateDefinition(options, width: 1280, height: 200);

        _platform = new Platform(def, ruleTiles);
        _platform.LoadContent(Content, GraphicsDevice);
        _platform.Position = new MGVector2(0, 720);
        _platformPhysics = new PlatformPhysics();
        _platformPhysics.Construct(_physicsWorld, _platform);

        _gameObjects.Add(_platform);
        _bodies.Add(_platformPhysics);

        _platformPhysics.OnCollision += (Fixture fixtureA, Fixture fixtureB, Contact contact) =>
        {
            Body body = fixtureA.Body;
            Body otherBody = fixtureB.Body;

            Vector2 hitPoint = fixtureA.Body.Position;

            if (otherBody.Tag is string tag && tag == "Projectile")
            {
                Vector2 normal;
                FixedArray2<Vector2> points;
                contact.GetWorldManifold(out normal, out points);

                Vector2 hitPointMeters = points[0];

                var hitPointPx = new MGVector2(hitPointMeters.X * 100f, hitPointMeters.Y * 100f);

                QueuePlatformCrater(hitPointPx, 30f, 5f);
            }

            return true;
        };

        LoadContent();
    }

    private Effect _platformClipEffect;

    protected override void LoadContent()
    {
        _platformClipEffect = Content.Load<Effect>("MaskedTile");
        _explosionSound = Content.Load<SoundEffect>("soundfx/pixel-explosion");
        if (_playerTank != null)
            _playerTank.ExplosionSound = _explosionSound;
        if (_aiTank != null)
            _aiTank.ExplosionSound = _explosionSound;
    }

    protected override void Update(GameTime gameTime)
    {
        float deltaTime = (float)gameTime.ElapsedGameTime.TotalSeconds;
        float timeStep = Math.Min(deltaTime, 0.016f);
        if (
            GamePad.GetState(PlayerIndex.One).Buttons.Back == ButtonState.Pressed
            || Keyboard.GetState().IsKeyDown(Keys.Escape)
        )
            Exit();

        KeyboardState keyboardState = Keyboard.GetState();

        if (keyboardState.IsKeyDown(Keys.A))
        {
            _playerTankPhysics.ApplyForce(new Vector2(-5, 0));
        }
        else if (keyboardState.IsKeyDown(Keys.D))
        {
            _playerTankPhysics.ApplyForce(new Vector2(5, 0));
        }
        bool rotatingCommand = false;
        if (keyboardState.IsKeyDown(Keys.Left))
        {
            _playerTankPhysics.RotateTurret(-1);
            rotatingCommand = true;
        }
        else if (keyboardState.IsKeyDown(Keys.Right))
        {
            _playerTankPhysics.RotateTurret(1);
            rotatingCommand = true;
        }
        else
        {
            _playerTankPhysics.StopRotatingTurret();
        }
        _playerTank.SetTrajectoryInput(rotatingCommand);

        if (keyboardState.IsKeyDown(Keys.Q))
        {
            Tank target = _aiTank ?? _playerTank;
            TryFireHomingMissile(_playerTank, _playerTankPhysics, target);
        }

        if (keyboardState.IsKeyDown(Keys.X))
        {
            TryFireMissile(_playerTank, _playerTankPhysics);
        }

        if (keyboardState.IsKeyDown(Keys.C))
        {
            TryFireGrenade(_playerTank, _playerTankPhysics);
        }

        _computerAgent?.Update(gameTime);

        for (int i = 0; i < _bodies.Count; i++)
        {
            if (_gameObjects[i].Destroyed)
            {
                UnregisterProjectileBodies(_bodies[i]);
                _physicsWorld.Remove(_bodies[i]);
                _gameObjects.RemoveAt(i);
                _bodies.RemoveAt(i);
            }
            else
            {
                _bodies[i].Update(_gameObjects[i]);
                _gameObjects[i].Update(gameTime);
            }
        }

        foreach (var body in _bodies) { }
        _physicsWorld.Step(timeStep);
        while (_postStep.Count > 0)
        {
            _postStep.Dequeue().Invoke();
        }

        base.Update(gameTime);
    }

    protected override void Draw(GameTime gameTime)
    {
        GraphicsDevice.Clear(Color.CornflowerBlue);

        SpriteBatch.Begin(samplerState: SamplerState.PointClamp);
        sky.Draw(SpriteBatch, cameraWorldTopLeft: _camera.Position);
        foreach (var go in _gameObjects)
        {
            if (go is not Platform)
                go.Draw(SpriteBatch);
        }

        SpriteBatch.End();

        SpriteBatch.Begin(
            SpriteSortMode.Immediate,
            BlendState.AlphaBlend,
            SamplerState.PointClamp,
            DepthStencilState.None,
            RasterizerState.CullNone,
            effect: _platformClipEffect
        );

        foreach (var go in _gameObjects)
        {
            if (go is Platform platform)
                platform.Draw(SpriteBatch);
        }
        SpriteBatch.End();

        base.Draw(gameTime);
    }

    private bool TryFireMissile(Tank shooter, TankPhysics shooterPhysics)
    {
        if (shooter == null || shooterPhysics == null)
            return false;

        if (!shooter.Gun.CanShoot())
            return false;

        shooter.Gun.Shoot();
        Missile projectile = new Missile();
        projectile.Initialize(Content);
        projectile.ExplosionSound = _explosionSound;

        projectile.Position = shooter.Gun.Position + new MGVector2(0, -shooter.Gun.Width);
        projectile.Rotation = shooter.Gun.Rotation;
        MissilePhysics projectilePhysics = new MissilePhysics();
        projectilePhysics.Initialize(_physicsWorld, projectile);

        projectilePhysics.Body.OnCollision += (
            Fixture fixtureA,
            Fixture fixtureB,
            Contact contact
        ) =>
        {
            _postStep.Enqueue(() =>
            {
                if (!shooterPhysics.Contains(fixtureB.Body))
                {
                    projectile.Explode();
                    projectilePhysics.Body.BodyType = BodyType.Static;
                }
            });
            return true;
        };

        float forceStrength = 20f;

        Vector2 force = new Vector2(
            (float)System.Math.Cos(projectile.Rotation) * forceStrength,
            (float)System.Math.Sin(projectile.Rotation) * forceStrength
        );

        var v = projectilePhysics.Body.LinearVelocity;

        if (v.LengthSquared() > 0.0001f)
        {
            float angle = (float)Math.Atan2(v.Y, v.X);
            projectilePhysics.Body.Rotation = angle;
        }
        projectilePhysics.Body.ApplyForce(force);

        float speed = projectilePhysics.Body.LinearVelocity.Length();
        projectilePhysics.Body.AngularVelocity = speed * 25f;

        RegisterProjectileOwner(projectilePhysics.Body, shooter);

        _gameObjects.Add(projectile);
        _bodies.Add(projectilePhysics);

        return true;
    }

    private bool TryFireHomingMissile(Tank shooter, TankPhysics shooterPhysics, Tank targetTank)
    {
        if (shooter == null || shooterPhysics == null)
            return false;

        if (!shooter.Gun.CanShoot())
            return false;

        shooter.Gun.Shoot();
        HomingMissile projectile = new HomingMissile();

        Tank resolvedTarget = targetTank;
        if (resolvedTarget == null || resolvedTarget.Destroyed)
        {
            resolvedTarget = GetOpposingTank(shooter);
        }
        if (resolvedTarget == null || resolvedTarget.Destroyed)
        {
            resolvedTarget = shooter;
        }
        projectile.Target = resolvedTarget;

        projectile.Initialize(Content);
        projectile.ExplosionSound = _explosionSound;

        projectile.Position = shooter.Gun.Position + new MGVector2(0, -shooter.Gun.Width);
        projectile.Rotation = shooter.Gun.Rotation;
        HomingMissilePhysics projectilePhysics = new HomingMissilePhysics();
        projectilePhysics.Initialize(_physicsWorld, projectile);
        projectilePhysics.Body.OnCollision += (
            Fixture fixtureA,
            Fixture fixtureB,
            Contact contact
        ) =>
        {
            Body otherBody = fixtureB.Body;

            if (!shooterPhysics.Contains(otherBody))
            {
                _postStep.Enqueue(() =>
                {
                    projectilePhysics.Body.BodyType = BodyType.Static;
                    projectile.Explode();
                });
            }

            return true;
        };

        float forceStrength = 20f;

        Vector2 force = new Vector2(
            (float)System.Math.Cos(shooter.Gun.Rotation) * forceStrength,
            (float)System.Math.Sin(shooter.Gun.Rotation) * forceStrength
        );

        projectilePhysics.Body.Rotation = shooter.Gun.Rotation;
        projectilePhysics.Body.ApplyForce(force);

        float speed = projectilePhysics.Body.LinearVelocity.Length();
        projectilePhysics.Body.AngularVelocity = speed * 20f;

        RegisterProjectileOwner(projectilePhysics.Body, shooter);

        _gameObjects.Add(projectile);
        _bodies.Add(projectilePhysics);
        return true;
    }

    private Tank GetOpposingTank(Tank shooter)
    {
        if (shooter == _playerTank)
            return _aiTank;
        if (shooter == _aiTank)
            return _playerTank;
        return null;
    }

    private bool TryFireGrenade(Tank shooter, TankPhysics shooterPhysics)
    {
        if (shooter == null || shooterPhysics == null)
            return false;

        if (!shooter.Gun.CanShoot())
            return false;

        shooter.Gun.Shoot();
        Grenade grenade = new Grenade();
        grenade.Initialize(Content);
        grenade.ExplosionSound = _explosionSound;

        grenade.Position = shooter.Gun.Position + new MGVector2(0, -shooter.Gun.Width);
        grenade.Rotation = shooter.Gun.Rotation;
        GrenadePhysics grenadePhysics = new GrenadePhysics();
        grenadePhysics.Initialize(_physicsWorld, grenade);

        grenade.Position = shooter.Gun.Position + new MGVector2(0, -shooter.Gun.Width);
        grenade.Rotation = shooter.Gun.Rotation;
        grenade.Exploded += HandleGrenadeExplosion;

        grenadePhysics.Body.OnCollision += (Fixture fixtureA, Fixture fixtureB, Contact contact) =>
        {
            if (!shooterPhysics.Contains(fixtureB.Body))
            {
                _postStep.Enqueue(() =>
                {
                    if (grenade.CountdownStarted)
                        return;
                    var v = grenadePhysics.Body.LinearVelocity;
                    float thresholdSq = GrenadeRestVelocityThreshold * GrenadeRestVelocityThreshold;
                    if (v.LengthSquared() <= thresholdSq)
                        grenade.StartCountDown();
                });
            }
            return true;
        };

        float forceStrength = 40f;

        Vector2 force = new Vector2(
            (float)System.Math.Cos(grenade.Rotation) * forceStrength,
            (float)System.Math.Sin(grenade.Rotation) * forceStrength
        );

        var v = grenadePhysics.Body.LinearVelocity;

        if (v.LengthSquared() > 0.0001f)
        {
            float angle = (float)Math.Atan2(v.Y, v.X);
            grenadePhysics.Body.Rotation = angle;
        }
        grenadePhysics.Body.ApplyForce(force);

        float speed = grenadePhysics.Body.LinearVelocity.Length();
        grenadePhysics.Body.AngularVelocity = speed * 25f;

        RegisterProjectileOwner(grenadePhysics.Body, shooter);

        _gameObjects.Add(grenade);
        _bodies.Add(grenadePhysics);
        return true;
    }

    private void HandleGrenadeExplosion(Grenade grenade, MGVector2 position)
    {
        grenade.Exploded -= HandleGrenadeExplosion;
        QueuePlatformCrater(position, grenade.CraterWidth, grenade.CraterDepth);
        ApplyGrenadeDamage(position, grenade.ExplosionRadius);
    }

    private bool IsHostileProjectile(Body body, Tank potentialVictim)
    {
        if (body == null || potentialVictim == null)
            return false;

        if (body.Tag is not string tag)
            return false;

        if (tag != "Projectile" && tag != "Grenade")
            return false;

        if (_projectileOwners.TryGetValue(body, out Tank owner) && owner == potentialVictim)
            return false;

        return true;
    }

    private void RegisterProjectileOwner(Body body, Tank owner)
    {
        if (body == null || owner == null)
            return;
        _projectileOwners[body] = owner;
    }

    private void UnregisterProjectileBodies(PhysicsEntity physicsEntity)
    {
        if (physicsEntity == null)
            return;

        var bodies = physicsEntity.GetBodies();
        if (bodies == null)
            return;

        foreach (var body in bodies)
        {
            if (body != null)
                _projectileOwners.Remove(body);
        }
    }

    private void QueuePlatformCrater(MGVector2 craterWorldPos, float craterWidth, float craterDepth)
    {
        if (_platform == null || _platformPhysics == null)
            return;
        float localX = craterWorldPos.X - _platform.Position.X;
        _postStep.Enqueue(() =>
        {
            _platform.Shake(10f, 0.25f);
            _platform.AddCrater(new MGVector2(localX, 0f), craterWidth, craterDepth);
            _platformPhysics.Construct(_physicsWorld, _platform);
            _platform.PrepareRenderTargets();
        });
    }

    private void ApplyGrenadeDamage(MGVector2 explosionPos, float radius)
    {
        float radiusSq = radius * radius;
        TryDamageTank(_playerTank, explosionPos, radiusSq);
        TryDamageTank(_aiTank, explosionPos, radiusSq);
    }

    private static void TryDamageTank(Tank tank, MGVector2 explosionPos, float radiusSq)
    {
        if (tank == null || tank.Destroyed)
            return;
        MGVector2 delta = tank.Position - explosionPos;
        if (delta.LengthSquared() <= radiusSq)
            tank.TakeHealth(35f);
    }
}
