using System;
using System.Collections.Generic;

namespace SpaceTanks
{
    public class Animation
    {
        public List<TextureRegion> Frames { get; set; }
        public TimeSpan Delay { get; set; }

        public bool Loop { get; set; } = true;

        public bool HasFinished { get; private set; } = false;

        private int _currentFrame = 0;
        private TimeSpan _elapsed = TimeSpan.Zero;

        public Animation()
        {
            Frames = new List<TextureRegion>();
            Delay = TimeSpan.FromMilliseconds(100);
        }

        public Animation(List<TextureRegion> frames, TimeSpan delay, bool loop = true)
        {
            Frames = frames;
            Delay = delay;
            Loop = loop;
        }

        public void Update(float deltaTime)
        {
            if (Frames == null || Frames.Count == 0 || HasFinished)
                return;

            _elapsed += TimeSpan.FromSeconds(deltaTime);

            if (_elapsed >= Delay)
            {
                _elapsed -= Delay;
                _currentFrame++;

                if (_currentFrame >= Frames.Count)
                {
                    if (Loop)
                    {
                        _currentFrame = 0;
                    }
                    else
                    {
                        _currentFrame = Frames.Count - 1;
                        HasFinished = true;
                    }
                }
            }
        }

        public void Reset()
        {
            _currentFrame = 0;
            _elapsed = TimeSpan.Zero;
            HasFinished = false;
        }

        public TextureRegion CurrentFrame
        {
            get
            {
                if (Frames == null || Frames.Count == 0)
                    return null;

                return Frames[_currentFrame];
            }
        }
    }
}
