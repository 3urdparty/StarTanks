using System;
using System.Collections.Generic;
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
    private List<Projectile> projectiles = new List<Projectile>();
    private List<GameObject> _gameObjects = new List<GameObject>();
    private List<PhysicsEntity> _bodies = new List<PhysicsEntity>();

    // Physics world
    private World _physicsWorld;
    private bool _debugMode = true;

    private readonly Queue<Action> _postStep = new();

    public Game1()
        : base("Space Tanks", 1280, 720, false) { }

    protected override void Initialize()
    {
        base.Initialize();

        // Initialize physics world with gravity
        _physicsWorld = new World(new Vector2(0, 9.81f));

        // Initialize debug renderers
        AetherBoundsRenderer.Initialize(GraphicsDevice);

        // Initialize game objects
        Tank tank = new Tank(Content);
        tank.Position = new MGVector2(300, 0);
        TankPhysics TankPhysics = new TankPhysics();
        TankPhysics.Initialize(_physicsWorld, tank);
        _gameObjects.Add(tank);
        _bodies.Add(TankPhysics);

        Platform platform = new Platform(500, 100);
        platform.LoadContent(Content, GraphicsDevice);
        platform.Position = new MGVector2(300, 300);
        PlatformPhysics platformPhysics = new PlatformPhysics();
        platformPhysics.Construct(_physicsWorld, platform);

        _gameObjects.Add(platform);
        _bodies.Add(platformPhysics);
    }

    protected override void LoadContent()
    {
        // Load any additional content here
    }

    protected override void Update(GameTime gameTime)
    {
        Tank tank = (Tank)_gameObjects[0];
        TankPhysics tankBody = (TankPhysics)_bodies[0];
        if (
            GamePad.GetState(PlayerIndex.One).Buttons.Back == ButtonState.Pressed
            || Keyboard.GetState().IsKeyDown(Keys.Escape)
        )
            Exit();

        float deltaTime = (float)gameTime.ElapsedGameTime.TotalSeconds;
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
        else
        {
            // Dampen horizontal velocity when no input
            // Vector2 currentVelocity = tankBody.LinearVelocity;
            // tankBody.LinearVelocity = new Vector2(
            //     currentVelocity.X * 0.8f,
            //     currentVelocity.Y
            // );
        }

        // Gun rotation
        if (keyboardState.IsKeyDown(Keys.Left))
        {
            // tank.RotateGunLeft(deltaTime);
            //
            tankBody.RotateTurret(-1);
        }
        else if (keyboardState.IsKeyDown(Keys.Right))
        {
            // tank.RotateGunLeft(deltaTime);

            tankBody.RotateTurret(1);
        }
        else
        {
            tankBody.StopRotatingTurret();
        }

        // Shooting
        if (keyboardState.IsKeyDown(Keys.Space))
        {
            if (tank.Gun.CanShoot())
            {
                tank.Gun.Shoot();
                Missile projectile = new Missile();
                projectile.Initialize(Content);
                projectile.Position = tank.Gun.Position + new MGVector2(0, -tank.Gun.Width);
                projectile.Rotation = tank.Gun.Rotation;

                ProjectilePhysics projectilePhysics = new ProjectilePhysics();
                projectilePhysics.Initialize(_physicsWorld, projectile);
                projectilePhysics.Body.OnCollision += (
                    Fixture fixtureA,
                    Fixture fixtureB,
                    Contact contact
                ) =>
                {
                    Body otherBody = fixtureB.Body;

                    Vector2 hitPoint = fixtureA.Body.Position;

                    if (otherBody.Tag is string tag && tag == "Platform")
                    {
                        Platform platform = (Platform)_gameObjects[1];
                        PlatformPhysics platformPhysics = (PlatformPhysics)_bodies[1];

                        Vector2 normal;
                        FixedArray2<Vector2> points;
                        contact.GetWorldManifold(out normal, out points);

                        // Primary contact point (meters)
                        Vector2 hitPointMeters = points[0];

                        // Convert to pixel/world space for your game
                        var hitPointPx = new MGVector2(
                            hitPointMeters.X * 100f,
                            hitPointMeters.Y * 100f
                        );

                        float leftWorldX = platform.Position.X;
                        float localX = hitPointPx.X - leftWorldX;

                        System.Console.WriteLine(
                            $"{hitPointMeters} {hitPointPx} {leftWorldX} {localX}"
                        );
                        _postStep.Enqueue(() =>
                        {
                            platform.AddCrater(new MGVector2(localX, 0f), 30f, 5f);

                            // platform.AddCrater(new MGVector2(platform.Width * 0.6f, 0f), 120f, 30f);
                            platformPhysics.Construct(_physicsWorld, platform);

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
                _bodies[i].CheckCollisions(_physicsWorld);
                _bodies[i].Sync(_gameObjects[i]);
                _gameObjects[i].Update(gameTime);
            }
        }
        // Use smaller time steps for accuracy
        float timeStep = Math.Min(deltaTime, 0.016f); // Cap at ~60fps
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
        SpriteBatch.Begin(samplerState: SamplerState.PointClamp);

        foreach (GameObject gameObject in _gameObjects)
        {
            gameObject.Draw(SpriteBatch);
        }

        // Draw physics bounds - toggle with 'D' key
        if (_debugMode)
        {
            AetherBoundsRenderer.DrawWorldAABBs(
                SpriteBatch,
                _physicsWorld,
                Color.Orange,
                Color.Green
            );

            // PlatformPhysics platformPhysics = (PlatformPhysics)_bodies[1];
            // AetherBoundsRenderer.DrawBodyFixtures(
            //     SpriteBatch,
            //     platformPhysics.Body,
            //     Color.Lime,
            //     2f
            // );
        }

        SpriteBatch.End();
        base.Draw(gameTime);
    }
}
