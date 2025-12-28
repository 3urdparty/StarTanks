using System;
using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using MonoGameLibrary;
using MonoGameLibrary.Graphics;
using SpaceTanks.Extensions;

namespace SpaceTanks
{
    /// <summary>
    /// Procedurally tiles a night-sky background using small atlas regions.
    /// Heavy weight is given to stars-1 (blank) by default.
    ///
    /// Usage:
    ///   var sky = new Sky(1920, 1080, seed: 123);
    ///   sky.Initialize(content, "atlas.xml");
    ///   // in Draw:
    ///   sky.Draw(spriteBatch, cameraWorldTopLeft: Vector2.Zero); // or your camera position
    /// </summary>
    public sealed class Sky
    {
        private readonly int _tileSize; // in pixels (regions are 16x16)
        private readonly int _viewWidth;
        private readonly int _viewHeight;

        private int _gridW;
        private int _gridH;

        private TextureRegion[] _tiles; // stars-1..stars-7
        private int[] _tileIndices; // chosen index per cell (0..6)
        private readonly Random _rng;

        // Weighted distribution (must be length 7)
        // Higher value => more likely
        private float[] _weights = new float[]
        {
            0.80f, // stars-1 (blank) heavy weight
            0.06f, // stars-2
            0.05f, // stars-3
            0.03f, // stars-4
            0.03f, // stars-5
            0.02f, // stars-6
            0.01f, // stars-7
        };

        // Precomputed CDF for weighted sampling
        private float[] _cdf;

        public Sky(int viewWidth, int viewHeight, int tileSize = 16, int? seed = null)
        {
            _viewWidth = viewWidth;
            _viewHeight = viewHeight;
            _tileSize = tileSize;
            _rng = seed.HasValue ? new Random(seed.Value) : new Random();

            RecomputeGrid();
        }

        /// <summary>
        /// Optionally override weight distribution. Must be length 7 and sum > 0.
        /// Example: SetWeights(0.90f, 0.04f, 0.03f, 0.01f, 0.01f, 0.005f, 0.005f)
        /// </summary>
        public void SetWeights(params float[] weights)
        {
            if (weights == null || weights.Length != 7)
                throw new ArgumentException("weights must be length 7 (stars-1..stars-7).");

            float sum = 0f;
            for (int i = 0; i < weights.Length; i++)
                sum += Math.Max(0f, weights[i]);

            if (sum <= 0f)
                throw new ArgumentException("weights sum must be > 0.");

            _weights = new float[7];
            for (int i = 0; i < 7; i++)
                _weights[i] = Math.Max(0f, weights[i]) / sum;

            BuildCdf();
            Regenerate(); // apply new distribution
        }

        public void Initialize(
            Microsoft.Xna.Framework.Content.ContentManager content,
            string atlasPath
        )
        {
            TextureAtlas atlas = TextureAtlas.FromFile(content, atlasPath);

            _tiles = new TextureRegion[7];
            _tiles[0] = atlas.GetRegion("stars-1");
            _tiles[1] = atlas.GetRegion("stars-2");
            _tiles[2] = atlas.GetRegion("stars-3");
            _tiles[3] = atlas.GetRegion("stars-4");
            _tiles[4] = atlas.GetRegion("stars-5");
            _tiles[5] = atlas.GetRegion("stars-6");
            _tiles[6] = atlas.GetRegion("stars-7");

            BuildCdf();
            Regenerate();
        }

        /// <summary>
        /// Call if viewport size changes (e.g., window resize).
        /// </summary>
        public void Resize(int newViewWidth, int newViewHeight)
        {
            // note: fields are readonly in this version; if you need resizing,
            // change them to non-readonly or recreate Sky.
            throw new NotSupportedException(
                "Recreate Sky with the new viewport dimensions in this implementation."
            );
        }

        /// <summary>
        /// Re-roll the entire sky pattern (same weights, new random layout).
        /// </summary>
        public void Regenerate()
        {
            if (_tiles == null)
                return;

            _tileIndices = new int[_gridW * _gridH];

            for (int y = 0; y < _gridH; y++)
            {
                for (int x = 0; x < _gridW; x++)
                {
                    _tileIndices[y * _gridW + x] = SampleWeightedIndex();
                }
            }
        }

        /// <summary>
        /// Draws a tiled sky that follows the camera smoothly.
        /// Provide the camera's world-space top-left to get stable scrolling.
        /// If you have a camera transform matrix, use its translation inverse.
        /// </summary>
        public void Draw(SpriteBatch spriteBatch, Vector2 cameraWorldTopLeft)
        {
            if (_tiles == null || _tileIndices == null)
                return;

            // Offset to align tiles with camera (wrap within tile size)
            int offsetX = Mod((int)cameraWorldTopLeft.X, _tileSize);
            int offsetY = Mod((int)cameraWorldTopLeft.Y, _tileSize);

            // Draw slightly expanded area to cover edges during scrolling
            int startX = -offsetX - _tileSize;
            int startY = -offsetY - _tileSize;

            int tilesX = (_viewWidth / _tileSize) + 3;
            int tilesY = (_viewHeight / _tileSize) + 3;

            for (int gy = 0; gy < tilesY; gy++)
            {
                for (int gx = 0; gx < tilesX; gx++)
                {
                    // Wrap into our pre-generated grid
                    int cellX = Mod(gx, _gridW);
                    int cellY = Mod(gy, _gridH);
                    int idx = _tileIndices[cellY * _gridW + cellX];

                    TextureRegion region = _tiles[idx];

                    Vector2 pos = new Vector2(startX + gx * _tileSize, startY + gy * _tileSize);

                    region.Draw(
                        spriteBatch,
                        pos,
                        Color.White,
                        0f,
                        Vector2.Zero,
                        Vector2.One,
                        SpriteEffects.None,
                        0f
                    );
                }
            }
        }

        private void RecomputeGrid()
        {
            // Pre-generate a grid bigger than the view for variety; keep it modest for memory.
            // You can increase these multipliers if you want less repetition.
            _gridW = Math.Max(8, (_viewWidth / _tileSize) + 12);
            _gridH = Math.Max(8, (_viewHeight / _tileSize) + 12);
        }

        private void BuildCdf()
        {
            _cdf = new float[7];
            float running = 0f;

            // Normalize defensively (in case weights were not already normalized)
            float sum = 0f;
            for (int i = 0; i < 7; i++)
                sum += Math.Max(0f, _weights[i]);
            if (sum <= 0f)
                sum = 1f;

            for (int i = 0; i < 7; i++)
            {
                running += Math.Max(0f, _weights[i]) / sum;
                _cdf[i] = running;
            }

            // Ensure last is exactly 1 for numerical safety
            _cdf[6] = 1f;
        }

        private int SampleWeightedIndex()
        {
            float r = (float)_rng.NextDouble(); // [0,1)
            for (int i = 0; i < _cdf.Length; i++)
            {
                if (r <= _cdf[i])
                    return i;
            }
            return 6;
        }

        private static int Mod(int x, int m)
        {
            if (m <= 0)
                return 0;
            int r = x % m;
            return r < 0 ? r + m : r;
        }
    }
}
