using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using nkast.Aether.Physics2D.Common.Decomposition;
using nkast.Aether.Physics2D.Dynamics;
using nkast.Aether.Physics2D.Dynamics.Contacts;
using AetherVector2 = nkast.Aether.Physics2D.Common.Vector2;
using Vertices = nkast.Aether.Physics2D.Common.Vertices;

namespace SpaceTanks
{
    public class PlatformPhysics : PhysicsEntity
    {
        public Body Body { get; private set; }
        private const float PlatformMass = 1000f;

        public override List<Body> GetBodies()
        {
            return [Body];
        }

        public void Initialize(World world, Platform platform)
        {
            AetherVector2 physicsPos = new AetherVector2(
                platform.Position.X / 100f,
                platform.Position.Y / 100f
            );

            Body = world.CreateBody(physicsPos, 0, BodyType.Static);
            Body.Tag = "Platform";
        }

        public void Construct(World world, Platform platform)
        {
            AetherVector2 physicsPos = new AetherVector2(
                platform.Position.X / 100f,
                platform.Position.Y / 100f
            );

            if (Body != null)
            {
                world.Remove(Body);
                Body = null;
            }

            Body = world.CreateBody(physicsPos, 0, BodyType.Static);
            Body.Tag = "Platform";
            CreateBodyFromVertices(Body, platform.GetVertices(), 10f);

            Body.OnCollision += ForwardOnCollision;
        }

        private void CreateBodyFromVertices(Body body, List<Vector2> gameVertices, float density)
        {
            if (gameVertices == null || gameVertices.Count < 3)
                return;

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
            float localX = (vertex.X - halfWidth) / 100f;
            float localY = (vertex.Y - halfHeight) / 100f;
            return new AetherVector2(localX, localY);
        }

        public Vector2 GetPosition()
        {
            return new Vector2(Body.Position.X * 100f, Body.Position.Y * 100f);
        }

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

            platform.Position = new Vector2(Body.Position.X * 100f, Body.Position.Y * 100f);
            platform.Rotation = Body.Rotation;
        }
    }

    public class Platform : GameObject
    {
        public bool NeedsRetile { get; private set; } = false;

        public event Action<Platform> RetileRequested;
        private GraphicsDevice _gd;

        private RenderTarget2D _fillRT;
        private bool _fillDirty = true;

        private readonly List<Vector2> _surface = new();
        private const float SurfaceStep = 10f;

        private readonly List<Vector2> _poly = new();

        private RenderTarget2D _maskRT;
        private bool _maskDirty = true;

        private BasicEffect _maskEffect2D;

        private Effect _clipEffect;

        private float _shakeTimeLeft = 0f;
        private float _shakeDuration = 0f;
        private float _shakeMagnitude = 0f;
        private Vector2 _shakeOffset = Vector2.Zero;
        private readonly Random _rng = new Random();

        private TileMap _ruleTiles;
        private int[,] _tileIds;

        public void Shake(float magnitudePx, float durationSec)
        {
            _shakeMagnitude = Math.Max(_shakeMagnitude, magnitudePx);
            _shakeDuration = Math.Max(_shakeDuration, durationSec);
            _shakeTimeLeft = _shakeDuration;
        }

        public void ApplyTileIds(int[,] tileIds)
        {
            _tileIds = tileIds ?? throw new ArgumentNullException(nameof(tileIds));
            NeedsRetile = false;
            _fillDirty = true;
        }

        public Platform(PlatformDefinition def, TileMap ruleTiles)
        {
            Width = def.Width;
            Height = def.Height;
            Origin = new Vector2(Width, Height) * 0.5f;

            _ruleTiles = ruleTiles;
            _tileIds = def.TileIds;

            _poly.Clear();
            _poly.AddRange(def.Polygon);

            _surface.Clear();
            _surface.AddRange(def.Surface);

            _fillDirty = true;
            _maskDirty = true;
        }

        public Platform(int width, int height)
        {
            Width = width;
            Height = height;
            Origin = new Vector2(Width, Height) * 0.5f;

            _surface.Clear();
            for (float x = 0; x <= Width; x += SurfaceStep)
                _surface.Add(new Vector2(x, -Height));

            RebuildPolygonFromSurface();
        }

        public void LoadContent(ContentManager content, GraphicsDevice graphicsDevice)
        {
            _gd = graphicsDevice;

            _ruleTiles = TileMap.FromFile(content, "tile-rules.xml");

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

            _maskRT = new RenderTarget2D(
                _gd,
                Width,
                Height,
                false,
                SurfaceFormat.Color,
                DepthFormat.None
            );

            _maskEffect2D = new BasicEffect(_gd)
            {
                VertexColorEnabled = true,
                TextureEnabled = false,
                World = Matrix.Identity,
                View = Matrix.Identity,
                Projection = Matrix.CreateOrthographicOffCenter(0, Width, Height, 0, 0, 1),
            };

            _clipEffect = content.Load<Effect>("MaskedTile");

            PrepareRenderTargets();
        }

        public void AddCrater(Vector2 craterCenterLocal, float craterTopWidth, float craterDepth)
        {
            if (craterTopWidth <= 0f || craterDepth <= 0f)
                return;

            float cx = craterCenterLocal.X;
            float halfW = craterTopWidth * 0.5f;

            float left = MathF.Max(0f, cx - halfW);
            float right = MathF.Min(Width, cx + halfW);

            for (int i = 0; i < _surface.Count; i++)
            {
                float x = _surface[i].X;
                if (x < left || x > right)
                    continue;

                float t = (x - cx) / halfW;
                float w = 1f - (t * t);
                float delta = craterDepth * w;

                float newY = _surface[i].Y + delta;

                newY = MathF.Min(newY, -1f);

                _surface[i] = new Vector2(x, newY);
            }

            RebuildPolygonFromSurface();
            _maskDirty = true;

            NeedsRetile = true;

            RetileRequested?.Invoke(this);
        }

        private static bool PointInPolygon(IReadOnlyList<Vector2> poly, Vector2 p)
        {
            bool inside = false;
            for (int i = 0, j = poly.Count - 1; i < poly.Count; j = i++)
            {
                Vector2 a = poly[i];
                Vector2 b = poly[j];

                bool intersect =
                    ((a.Y > p.Y) != (b.Y > p.Y))
                    && (p.X < (b.X - a.X) * (p.Y - a.Y) / ((b.Y - a.Y) + 1e-6f) + a.X);

                if (intersect)
                    inside = !inside;
            }
            return inside;
        }

        private void RebuildFillRT()
        {
            if (_gd == null || _fillRT == null || _ruleTiles == null || _tileIds == null)
                return;

            int tileW = _ruleTiles.TileWidth;
            int tileH = _ruleTiles.TileHeight;

            int rows = _tileIds.GetLength(0);
            int cols = _tileIds.GetLength(1);

            _gd.SetRenderTarget(_fillRT);
            _gd.Clear(Color.Transparent);

            using (var sb = new SpriteBatch(_gd))
            {
                sb.Begin(
                    SpriteSortMode.Deferred,
                    BlendState.AlphaBlend,
                    SamplerState.PointClamp,
                    DepthStencilState.None,
                    RasterizerState.CullNone
                );

                for (int r = 0; r < rows; r++)
                {
                    for (int c = 0; c < cols; c++)
                    {
                        int id = _tileIds[r, c];
                        if (id < 0)
                            continue;

                        TextureRegion tr = _ruleTiles.GetRegionById(id);

                        int x = c * tileW;
                        int y = r * tileH;

                        int w = Math.Min(tr.SourceRectangle.Width, Width - x);
                        int h = Math.Min(tr.SourceRectangle.Height, Height - y);
                        if (w <= 0 || h <= 0)
                            continue;

                        sb.Draw(
                            tr.Texture,
                            new Rectangle(x, y, w, h),
                            new Rectangle(tr.SourceRectangle.X, tr.SourceRectangle.Y, w, h),
                            Color.White
                        );
                    }
                }

                sb.End();
            }

            _gd.SetRenderTarget(null);
            _fillDirty = false;
        }

        public void PrepareRenderTargets()
        {
            if (_maskDirty)
                RebuildMaskRT();

            if (!NeedsRetile && _fillDirty)
                RebuildFillRT();
        }

        private void RebuildPolygonFromSurface()
        {
            _poly.Clear();

            _poly.Add(new Vector2(0, 0));
            _poly.Add(new Vector2(Width, 0));

            for (int i = _surface.Count - 1; i >= 0; i--)
                _poly.Add(_surface[i]);

            if (_surface.Count > 0 && _surface[0].X != 0f)
                _poly.Add(new Vector2(0f, _surface[0].Y));
        }

        public List<Vector2> GetVertices() => _poly;

        private void RebuildMaskRT()
        {
            if (_gd == null || _maskRT == null || _maskEffect2D == null)
                return;

            _gd.SetRenderTarget(_maskRT);
            _gd.Clear(Color.Transparent);

            var tris = EarClipper.Triangulate(_poly);

            if (tris == null || tris.Count == 0)
            {
                _gd.SetRenderTarget(null);
                _maskDirty = false;
                return;
            }

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

                float t = _shakeTimeLeft / Math.Max(_shakeDuration, 0.0001f);
                float amp = _shakeMagnitude * t;

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
