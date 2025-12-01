using System;
using System.Collections.Generic;

namespace MonoGameLibrary.Graphics
{
    public class Animation
    {
        public List<TextureRegion> Frames { get; set; }
        public TimeSpan Delay { get; set; }

        /// <summary>
        /// If true, animation loops. If false, it plays once and stops.
        /// </summary>
        public bool Loop { get; set; } = true;

        /// <summary>
        /// True when a non-looping animation has finished.
        /// </summary>
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

        /// <summary>
        /// Advances animation frames based on deltaTime (in seconds).
        /// </summary>
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
                        _currentFrame = Frames.Count - 1; // hold last frame
                        HasFinished = true;               // mark finished
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
        /// <summary>
        /// Returns the currently active frame.
        /// </summary>
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
