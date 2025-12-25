using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using MonoGameLibrary;
using MonoGameLibrary.Graphics;
using nkast.Aether.Physics2D.Common.Decomposition;
using nkast.Aether.Physics2D.Dynamics;
using nkast.Aether.Physics2D.Dynamics.Contacts;
using AetherVector2 = nkast.Aether.Physics2D.Common.Vector2;
using Vertices = nkast.Aether.Physics2D.Common.Vertices;

namespace SpaceTanks
{
    public class PlatformPhysics : PhysicsEntity
    {
        public event OnCollisionEventHandler OnCollision;
        public Body Body { get; private set; }
        private const float PlatformMass = 1000f;

        public override List<Body> GetBodies()
        {
            return [Body];
        }

        private bool ForwardOnCollision(Fixture fixtureA, Fixture fixtureB, Contact contact)
        {
            // Invoke all subscribed handlers
            if (OnCollision != null)
                return OnCollision.Invoke(fixtureA, fixtureB, contact);

            return true;
        }

        public void Initialize(World world, Platform platform)
        {
            AetherVector2 physicsPos = new AetherVector2(
                platform.Position.X / 100f,
                platform.Position.Y / 100f
            );

            Body = world.CreateBody(physicsPos, 0, BodyType.Static);
            Body.Tag = "Platform";

            // This now uses the exact platform outline
        }

        public void Construct(World world, Platform platform)
        {
            AetherVector2 physicsPos = new AetherVector2(
                platform.Position.X / 100f,
                platform.Position.Y / 100f
            );

            // IMPORTANT: remove old body first
            if (Body != null)
            {
                world.Remove(Body);
                Body = null;
            }

            Body = world.CreateBody(physicsPos, 0, BodyType.Static);
            Body.Tag = "Platform";
            CreateBodyFromVertices(Body, platform.GetVertices(), 10f);

            // Reattach preserved collision handler
            Body.OnCollision += ForwardOnCollision;
        }

        private void CreateBodyFromVertices(Body body, List<Vector2> gameVertices, float density)
        {
            if (gameVertices == null || gameVertices.Count < 3)
                return;

            // Triangulate concave polygon -> triangles (convex)
            var triangles = EarClipper.Triangulate(gameVertices);

            var parts = new List<nkast.Aether.Physics2D.Common.Vertices>(triangles.Count);

            foreach (var tri in triangles)
            {
                var v = new nkast.Aether.Physics2D.Common.Vertices(3);
                v.Add(new AetherVector2(tri.A.X / 100f, tri.A.Y / 100f));
                v.Add(new AetherVector2(tri.B.X / 100f, tri.B.Y / 100f));
                v.Add(new AetherVector2(tri.C.X / 100f, tri.C.Y / 100f));
                parts.Add(v);
            }

            var fixtures = Body.CreateCompoundPolygon(parts, density);
            foreach (var f in fixtures)
            {
                f.Friction = 0.8f;
                f.Restitution = 0.2f;
            }
        }

        private AetherVector2 ConvertToPhysics(Vector2 vertex, float halfWidth, float halfHeight)
        {
            // Convert from top-left origin to center origin, then to physics units
            float localX = (vertex.X - halfWidth) / 100f;
            float localY = (vertex.Y - halfHeight) / 100f;
            return new AetherVector2(localX, localY);
        }

        public Vector2 GetPosition()
        {
            return new Vector2(Body.Position.X * 100f, Body.Position.Y * 100f);
        }

        /// <summary>
        /// Sync the Platform's shape from the physics body
        /// </summary>
        public void Sync(Platform platform)
        {
            if (Body == null)
                return;
            Update(platform);
        }

        public void Update(Platform platform)
        {
            if (Body == null)
                return;
            platform.Shake(6f, 0.18f);

            // Sync render transform from physics
            platform.Position = new Vector2(Body.Position.X * 100f, Body.Position.Y * 100f);
            platform.Rotation = Body.Rotation;
        }
    }

    public class Platform : GameObject
    {
        private GraphicsDevice _gd;

        private RenderTarget2D _fillRT;
        private bool _fillDirty = true;

        // Heightfield surface (local space): x in [0..Width], y negative up to 0
        private readonly List<Vector2> _surface = new();
        private const float SurfaceStep = 10f;

        // Polygon used for physics + mask triangulation (local space, concave allowed)
        private readonly List<Vector2> _poly = new();

        // Mask render target (local-space 0..Width, 0..Height)
        private RenderTarget2D _maskRT;
        private bool _maskDirty = true;

        // Tile pattern (atlas region)
        private TextureRegion _tileRegion;
        private bool _usePointSampling = true;

        // For drawing filled triangles into _maskRT
        private BasicEffect _maskEffect2D;

        // Effect that multiplies Tile * MaskAlpha
        // You need to load it in your Content project (MGCB).
        // It should sample Texture0 (tile) and Texture1 (mask).
        private Effect _clipEffect;

        private float _shakeTimeLeft = 0f;
        private float _shakeDuration = 0f;
        private float _shakeMagnitude = 0f;
        private Vector2 _shakeOffset = Vector2.Zero;
        private readonly Random _rng = new Random();

        public void Shake(float magnitudePx, float durationSec)
        {
            _shakeMagnitude = Math.Max(_shakeMagnitude, magnitudePx);
            _shakeDuration = Math.Max(_shakeDuration, durationSec);
            _shakeTimeLeft = _shakeDuration;
        }

        public Platform(int width, int height)
        {
            Width = width;
            Height = height;
            Origin = new Vector2(Width, Height) * 0.5f;

            // Initialize surface at flat top y = -Height
            _surface.Clear();
            for (float x = 0; x <= Width; x += SurfaceStep)
                _surface.Add(new Vector2(x, -Height));

            RebuildPolygonFromSurface();
        }

        public void LoadContent(ContentManager content, GraphicsDevice graphicsDevice)
        {
            _gd = graphicsDevice;

            // Load atlas region
            TextureAtlas atlas = TextureAtlas.FromFile(content, "tilemap-atlas.xml");
            _tileRegion = atlas.GetRegion("11"); // this is a TextureRegion

            _maskRT = new RenderTarget2D(
                _gd,
                Width,
                Height,
                false,
                SurfaceFormat.Color,
                DepthFormat.None
            );
            _fillRT = new RenderTarget2D(
                _gd,
                Width,
                Height,
                false,
                SurfaceFormat.Color,
                DepthFormat.None
            );

            _fillDirty = true;
            _maskDirty = true;
            // Create mask render target (local-space size)
            _maskRT = new RenderTarget2D(
                _gd,
                Width,
                Height,
                false,
                SurfaceFormat.Color,
                DepthFormat.None
            );

            // BasicEffect for drawing the mask triangles (in RT space)
            _maskEffect2D = new BasicEffect(_gd)
            {
                VertexColorEnabled = true,
                TextureEnabled = false,
                World = Matrix.Identity,
                View = Matrix.Identity,
                Projection = Matrix.CreateOrthographicOffCenter(0, Width, Height, 0, 0, 1),
            };

            // Load your clipping shader (must exist in Content)
            // Name it e.g. "MaskedTile"
            _clipEffect = content.Load<Effect>("MaskedTile");

            PrepareRenderTargets();
        }

        // -----------------------------
        // Terrain deformation (stacking)
        // -----------------------------
        public void AddCrater(Vector2 craterCenterLocal, float craterTopWidth, float craterDepth)
        {
            if (craterTopWidth <= 0f || craterDepth <= 0f)
                return;

            float cx = craterCenterLocal.X;
            float halfW = craterTopWidth * 0.5f;

            float left = MathF.Max(0f, cx - halfW);
            float right = MathF.Min(Width, cx + halfW);

            // Parabolic falloff: 1 at center, 0 at edges
            for (int i = 0; i < _surface.Count; i++)
            {
                float x = _surface[i].X;
                if (x < left || x > right)
                    continue;

                float t = (x - cx) / halfW; // [-1..1]
                float w = 1f - (t * t); // [0..1]
                float delta = craterDepth * w;

                // Lowering surface means y increases toward 0 (since top is negative)
                float newY = _surface[i].Y + delta;

                // Clamp so we never reach or cross the bottom (0)
                newY = MathF.Min(newY, -1f);

                _surface[i] = new Vector2(x, newY);
            }

            RebuildPolygonFromSurface();
            _maskDirty = true;
        }

        private void RebuildFillRT()
        {
            if (_gd == null || _fillRT == null || _tileRegion == null)
                return;

            _gd.SetRenderTarget(_fillRT);
            _gd.Clear(Color.Transparent);

            using (var sb = new SpriteBatch(_gd))
            {
                sb.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointClamp);

                // This property name is correct in your code:
                Rectangle src = _tileRegion.SourceRectangle;

                int tileW = src.Width;
                int tileH = src.Height;

                // Tile across the entire local RT
                for (int y = 0; y < Height; y += tileH)
                {
                    for (int x = 0; x < Width; x += tileW)
                    {
                        int w = Math.Min(tileW, Width - x);
                        int h = Math.Min(tileH, Height - y);

                        // Crop source at edges if needed
                        var srcCropped = new Rectangle(src.X, src.Y, w, h);
                        var dst = new Rectangle(x, y, w, h);

                        sb.Draw(_tileRegion.Texture, dst, srcCropped, Color.White);
                    }
                }

                sb.End();
            }

            _gd.SetRenderTarget(null);
            _fillDirty = false;
        }

        // in Platform
        public void PrepareRenderTargets()
        {
            if (_fillDirty)
                RebuildFillRT();
            if (_maskDirty)
                RebuildMaskRT();
        }

        private void RebuildPolygonFromSurface()
        {
            _poly.Clear();

            // Bottom edge (y = 0)
            _poly.Add(new Vector2(0, 0));
            _poly.Add(new Vector2(Width, 0));

            // Top surface from right -> left (keeps a consistent winding for triangulation)
            for (int i = _surface.Count - 1; i >= 0; i--)
                _poly.Add(_surface[i]);

            // Ensure exact left end exists (optional safety)
            if (_surface.Count > 0 && _surface[0].X != 0f)
                _poly.Add(new Vector2(0f, _surface[0].Y));
        }

        public List<Vector2> GetVertices() => _poly;

        // -----------------------------
        // Mask rendering (RT)
        // -----------------------------
        private void RebuildMaskRT()
        {
            if (_gd == null || _maskRT == null || _maskEffect2D == null)
                return;

            _gd.SetRenderTarget(_maskRT);
            _gd.Clear(Color.Transparent);

            // Triangulate polygon in local space
            var tris = EarClipper.Triangulate(_poly);

            if (tris == null || tris.Count == 0)
            {
                _gd.SetRenderTarget(null);
                _maskDirty = false;
                return;
            }

            // Build triangle list vertices (convert local y to RT y)
            var vtx = new VertexPositionColor[tris.Count * 3];

            for (int i = 0; i < tris.Count; i++)
            {
                var t = tris[i];

                vtx[i * 3 + 0] = new VertexPositionColor(new Vector3(ToRT(t.A), 0f), Color.White);
                vtx[i * 3 + 1] = new VertexPositionColor(new Vector3(ToRT(t.B), 0f), Color.White);
                vtx[i * 3 + 2] = new VertexPositionColor(new Vector3(ToRT(t.C), 0f), Color.White);
            }

            foreach (var pass in _maskEffect2D.CurrentTechnique.Passes)
            {
                pass.Apply();
                _gd.DrawUserPrimitives(PrimitiveType.TriangleList, vtx, 0, tris.Count);
            }

            _gd.SetRenderTarget(null);
            _maskDirty = false;
        }

        // Local poly: y in [-Height..0]
        // RT: y in [0..Height] downward
        private Vector2 ToRT(Vector2 local) => new Vector2(local.X, local.Y + Height);

        public override void Draw(SpriteBatch spriteBatch)
        {
            _clipEffect.Parameters["TileTexture"].SetValue(_fillRT);
            _clipEffect.Parameters["MaskTexture"].SetValue(_maskRT);

            var topLeft = (Position + _shakeOffset) - new Vector2(Width, Height) * 0.5f;

            var destRect = new Rectangle(
                (int)MathF.Round(topLeft.X + Origin.X),
                (int)MathF.Round(topLeft.Y - Origin.Y),
                Width,
                Height
            );

            spriteBatch.Draw(_fillRT, destRect, Color.White);
        }

        public override void Update(GameTime gameTime)
        {
            float dt = (float)gameTime.ElapsedGameTime.TotalSeconds;

            if (_shakeTimeLeft > 0f)
            {
                _shakeTimeLeft -= dt;

                // decay 1..0
                float t = _shakeTimeLeft / Math.Max(_shakeDuration, 0.0001f);
                float amp = _shakeMagnitude * t;

                // random offset each frame
                float ox = ((float)_rng.NextDouble() * 2f - 1f) * amp;
                float oy = ((float)_rng.NextDouble() * 2f - 1f) * amp;
                _shakeOffset = new Vector2(ox, oy);

                if (_shakeTimeLeft <= 0f)
                {
                    _shakeOffset = Vector2.Zero;
                    _shakeMagnitude = 0f;
                    _shakeDuration = 0f;
                }
            }
        }
    }
}
