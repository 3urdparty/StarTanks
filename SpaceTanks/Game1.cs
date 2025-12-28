using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using MonoGameLibrary;
using MonoGameLibrary.Graphics;
using nkast.Aether.Physics2D.Common; // causes MGVector2 ambiguity
using nkast.Aether.Physics2D.Dynamics;
using nkast.Aether.Physics2D.Dynamics.Contacts;
using nkast.Aether.Physics2D.Dynamics.Joints;
using SpaceTanks.Extensions;
using MGVector2 = Microsoft.Xna.Framework.Vector2;
using Vector2 = nkast.Aether.Physics2D.Common.Vector2;

// using FixedArray2<T> = nkast.Aether.Physics2D.Common.FixedArray2;

namespace SpaceTanks;

public class Game1 : Core
{
    private GraphicsDeviceManager _graphics;
    private List<GameObject> _gameObjects = new List<GameObject>();
    private List<PhysicsEntity> _bodies = new List<PhysicsEntity>();

    private Camera2D _camera;
    Sky sky;

    // Physics world
    private World _physicsWorld;
    private bool _debugMode = true;

    private readonly Queue<Action> _postStep = new();

    public Game1()
        : base("Space Tanks", 1280, 720, false) { }

    protected override void Initialize()
    {
        base.Initialize();

        _camera = new Camera2D
        {
            Position = MGVector2.Zero, // start at world origin
        };

        sky = new Sky(GraphicsDevice.Viewport.Width, GraphicsDevice.Viewport.Height, seed: 123);
        sky.Initialize(Content, "atlas.xml");
        // Initialize physics world with gravity
        _physicsWorld = new World(new Vector2(0, 9.81f));

        // Initialize debug renderers
        AetherBoundsRenderer.Initialize(GraphicsDevice);

        // Initialize game objects
        Tank tank1 = new Tank(Content);
        tank1.Position = new MGVector2(350, 0);
        TankPhysics TankPhysics1 = new TankPhysics();

        TankPhysics1.Initialize(_physicsWorld, tank1);
        _gameObjects.Add(tank1);
        _bodies.Add(TankPhysics1);

        Tank tank2 = new Tank(Content);
        tank2.Position = new MGVector2(700, 0);
        TankPhysics TankPhysics2 = new TankPhysics();

        TankPhysics2.Initialize(_physicsWorld, tank2);

        TankPhysics2.OnCollision += (Fixture fixtureA, Fixture fixtureB, Contact contact) =>
        {
            if (fixtureB.Body.Tag is string tag && tag == "Projectile")
            {
                if (TankPhysics2.Contains(fixtureA.Body))
                {
                    tank2.TakeHealth(10);
                }
            }

            return true;
        };

        _gameObjects.Add(tank2);
        _bodies.Add(TankPhysics2);

        Platform platform = new Platform(1280, 200);
        platform.LoadContent(Content, GraphicsDevice);
        platform.Position = new MGVector2(0, 720);
        PlatformPhysics platformPhysics = new PlatformPhysics();
        platformPhysics.Construct(_physicsWorld, platform);

        _gameObjects.Add(platform);
        _bodies.Add(platformPhysics);

        platformPhysics.OnCollision += (Fixture fixtureA, Fixture fixtureB, Contact contact) =>
        {
            Body body = fixtureA.Body;
            Body otherBody = fixtureB.Body;

            Vector2 hitPoint = fixtureA.Body.Position;

            if (otherBody.Tag is string tag && tag == "Projectile")
            {
                Vector2 normal;
                FixedArray2<Vector2> points;
                contact.GetWorldManifold(out normal, out points);

                // Primary contact point (meters)
                Vector2 hitPointMeters = points[0];

                // Convert to pixel/world space for your game
                var hitPointPx = new MGVector2(hitPointMeters.X * 100f, hitPointMeters.Y * 100f);

                float leftWorldX = platform.Position.X;
                float localX = hitPointPx.X - leftWorldX;

                _postStep.Enqueue(() =>
                {
                    platform.Shake(6f, 0.18f); // magnitude px, duration sec
                    platform.AddCrater(new MGVector2(localX, 0f), 30f, 5f);
                    platformPhysics.Construct(_physicsWorld, platform);
                    platform.PrepareRenderTargets();
                });
            }

            return true;
        };

        LoadContent();
    }

    private Effect _platformClipEffect;

    protected override void LoadContent()
    {
        // Load any additional content here
        _platformClipEffect = Content.Load<Effect>("MaskedTile");
    }

    protected override void Update(GameTime gameTime)
    {
        float deltaTime = (float)gameTime.ElapsedGameTime.TotalSeconds;
        float timeStep = Math.Min(deltaTime, 0.016f); // Cap at ~60fps
        Tank tank = (Tank)_gameObjects[0];
        TankPhysics tankBody = (TankPhysics)_bodies[0];
        if (
            GamePad.GetState(PlayerIndex.One).Buttons.Back == ButtonState.Pressed
            || Keyboard.GetState().IsKeyDown(Keys.Escape)
        )
            Exit();

        KeyboardState keyboardState = Keyboard.GetState();

        // Tank movement
        if (keyboardState.IsKeyDown(Keys.A))
        {
            tankBody.ApplyForce(new Vector2(-5, 0));
        }
        else if (keyboardState.IsKeyDown(Keys.D))
        {
            tankBody.ApplyForce(new Vector2(5, 0));
        }

        // Gun rotation
        if (keyboardState.IsKeyDown(Keys.Left))
        {
            tankBody.RotateTurret(-1);
        }
        else if (keyboardState.IsKeyDown(Keys.Right))
        {
            tankBody.RotateTurret(1);
        }
        else
        {
            tankBody.StopRotatingTurret();
        }

        if (keyboardState.IsKeyDown(Keys.Q))
        {
            if (tank.Gun.CanShoot())
            {
                tank.Gun.Shoot();
                HomingMissile projectile = new HomingMissile();

                Tank tank2 = _gameObjects
                    .OfType<Tank>()
                    .FirstOrDefault(t => !ReferenceEquals(t, tank));
                projectile.Target = tank2;

                projectile.Initialize(Content);

                projectile.Position = tank.Gun.Position + new MGVector2(0, -tank.Gun.Width);
                projectile.Rotation = tank.Gun.Rotation;
                HomingMissilePhysics projectilePhysics = new HomingMissilePhysics();
                projectilePhysics.Initialize(_physicsWorld, projectile);
                projectilePhysics.Body.OnCollision += (
                    Fixture fixtureA,
                    Fixture fixtureB,
                    Contact contact
                ) =>
                {
                    Body otherBody = fixtureB.Body;

                    if (!tankBody.Contains(otherBody))
                    {
                        _postStep.Enqueue(() =>
                        {
                            projectilePhysics.Body.BodyType = BodyType.Static;
                            projectile.Explode();
                        });
                    }

                    return true;
                };

                // Apply force in the direction the gun is pointing
                float gunAngle = tank.Gun.Rotation;
                float forceStrength = 20f; // Adjust as needed

                Vector2 force = new Vector2(
                    (float)System.Math.Cos(tank.Gun.Rotation) * forceStrength,
                    (float)System.Math.Sin(tank.Gun.Rotation) * forceStrength
                );

                projectilePhysics.Body.Rotation = tank.Gun.Rotation;
                projectilePhysics.Body.ApplyForce(force);

                // Auto-spin based on velocity
                // The faster it moves, the faster it spins
                float speed = projectilePhysics.Body.LinearVelocity.Length();
                projectilePhysics.Body.AngularVelocity = speed * 20f; // Adjust multiplier to taste

                _gameObjects.Add(projectile);
                _bodies.Add(projectilePhysics);
            }
        }

        // Shooting
        if (keyboardState.IsKeyDown(Keys.X))
        {
            if (tank.Gun.CanShoot())
            {
                tank.Gun.Shoot();
                Missile projectile = new Missile();
                projectile.Initialize(Content);

                projectile.Position = tank.Gun.Position + new MGVector2(0, -tank.Gun.Width);
                projectile.Rotation = tank.Gun.Rotation;
                MissilePhysics projectilePhysics = new MissilePhysics();
                projectilePhysics.Initialize(_physicsWorld, projectile);

                projectile.Position = tank.Gun.Position + new MGVector2(0, -tank.Gun.Width);
                projectile.Rotation = tank.Gun.Rotation;

                projectilePhysics.Body.OnCollision += (
                    Fixture fixtureA,
                    Fixture fixtureB,
                    Contact contact
                ) =>
                {
                    _postStep.Enqueue(() =>
                    {
                        if (!tankBody.Contains(fixtureB.Body))
                        {
                            projectile.Explode();
                            projectilePhysics.Body.BodyType = BodyType.Static;
                        }
                    });

                    return true;
                };

                // Apply force in the direction the gun is pointing
                float forceStrength = 20f; // Adjust as needed

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
                projectilePhysics.Body.AngularVelocity = speed * 25f; // Adjust multiplier to taste

                _gameObjects.Add(projectile);
                _bodies.Add(projectilePhysics);
            }
        }

        // Shooting
        if (keyboardState.IsKeyDown(Keys.C))
        {
            if (tank.Gun.CanShoot())
            {
                tank.Gun.Shoot();
                Grenade grenade = new Grenade();
                grenade.Initialize(Content);

                grenade.Position = tank.Gun.Position + new MGVector2(0, -tank.Gun.Width);
                grenade.Rotation = tank.Gun.Rotation;
                GrenadePhysics grenadePhysics = new GrenadePhysics();
                grenadePhysics.Initialize(_physicsWorld, grenade);

                grenade.Position = tank.Gun.Position + new MGVector2(0, -tank.Gun.Width);
                grenade.Rotation = tank.Gun.Rotation;

                grenadePhysics.Body.OnCollision += (
                    Fixture fixtureA,
                    Fixture fixtureB,
                    Contact contact
                ) =>
                {
                    if (!tankBody.Contains(fixtureB.Body))
                    {
                        _postStep.Enqueue(() =>
                        {
                            grenade.StartCountDown();
                        });
                    }
                    return true;
                };

                // Apply force in the direction the gun is pointing
                float forceStrength = 40f; // Adjust as needed

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
                grenadePhysics.Body.AngularVelocity = speed * 25f; // Adjust multiplier to taste

                _gameObjects.Add(grenade);
                _bodies.Add(grenadePhysics);
            }
        }

        for (int i = 0; i < _bodies.Count; i++)
        {
            if (_gameObjects[i].Destroyed)
            {
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

        // Use smaller time steps for accuracy

        foreach (var body in _bodies) { }
        _physicsWorld.Step(timeStep);
        // World is now unlocked
        while (_postStep.Count > 0)
        {
            _postStep.Dequeue().Invoke();
        }

        base.Update(gameTime);
    }

    protected override void Draw(GameTime gameTime)
    {
        GraphicsDevice.Clear(Color.CornflowerBlue);

        // Pass 1: normal sprites (everything except platform)
        SpriteBatch.Begin(samplerState: SamplerState.PointClamp);
        sky.Draw(SpriteBatch, cameraWorldTopLeft: _camera.Position);
        foreach (var go in _gameObjects)
        {
            if (go is not Platform)
                go.Draw(SpriteBatch);
        }
        SpriteBatch.End();

        // Pass 2: platform with clip shader
        SpriteBatch.Begin(
            SpriteSortMode.Immediate,
            BlendState.AlphaBlend,
            SamplerState.PointClamp,
            DepthStencilState.None,
            RasterizerState.CullNone,
            effect: _platformClipEffect
        );

        if (_debugMode)
        {
            // AetherBoundsRenderer.DrawWorldAABBs(
            //     SpriteBatch,
            //     _physicsWorld,
            //     Color.Orange,
            //     Color.Green
            // );
        }

        foreach (var go in _gameObjects)
        {
            if (go is Platform platform)
                platform.Draw(SpriteBatch); // draws only the final quad, no state changes
        }
        SpriteBatch.End();

        base.Draw(gameTime);
    }
}
