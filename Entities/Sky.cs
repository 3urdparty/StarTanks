using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace SpaceTanks
{
    
    
    
    
    
    
    
    
    
    
    public sealed class Sky
    {
        private readonly int _tileSize; 
        private readonly int _viewWidth;
        private readonly int _viewHeight;

        private int _gridW;
        private int _gridH;

        private TextureRegion[] _tiles; 
        private int[] _tileIndices; 
        private readonly Random _rng;

        
        
        private float[] _weights = new float[]
        {
            0.90f, 
            0.02f, 
            0.03f, 
            0.02f, 
            0.01f, 
            0.015f, 
            0.005f, 
        };

        
        private float[] _cdf;

        public Sky(int viewWidth, int viewHeight, int tileSize = 16, int? seed = null)
        {
            _viewWidth = viewWidth;
            _viewHeight = viewHeight;
            _tileSize = tileSize;
            _rng = seed.HasValue ? new Random(seed.Value) : new Random();

            RecomputeGrid();
        }

        
        
        
        
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
            Regenerate(); 
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

        
        
        
        public void Resize(int newViewWidth, int newViewHeight)
        {
            
            
            throw new NotSupportedException(
                "Recreate Sky with the new viewport dimensions in this implementation."
            );
        }

        
        
        
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

        
        
        
        
        
        public void Draw(SpriteBatch spriteBatch, Vector2 cameraWorldTopLeft)
        {
            if (_tiles == null || _tileIndices == null)
                return;

            
            int offsetX = Mod((int)cameraWorldTopLeft.X, _tileSize);
            int offsetY = Mod((int)cameraWorldTopLeft.Y, _tileSize);

            
            int startX = -offsetX - _tileSize;
            int startY = -offsetY - _tileSize;

            int tilesX = (_viewWidth / _tileSize) + 3;
            int tilesY = (_viewHeight / _tileSize) + 3;

            for (int gy = 0; gy < tilesY; gy++)
            {
                for (int gx = 0; gx < tilesX; gx++)
                {
                    
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
            
            
            _gridW = Math.Max(8, (_viewWidth / _tileSize) + 12);
            _gridH = Math.Max(8, (_viewHeight / _tileSize) + 12);
        }

        private void BuildCdf()
        {
            _cdf = new float[7];
            float running = 0f;

            
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

            
            _cdf[6] = 1f;
        }

        private int SampleWeightedIndex()
        {
            float r = (float)_rng.NextDouble(); 
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
