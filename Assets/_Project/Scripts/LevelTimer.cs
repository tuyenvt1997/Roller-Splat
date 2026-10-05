using UnityEngine;

namespace RollerSplat
{
    /// <summary>Per-level countdown. A limit of 0 (or less) means the level is untimed.</summary>
    public class LevelTimer
    {
        public float Limit { get; private set; }
        public float Remaining { get; private set; }
        public bool HasLimit => Limit > 0f;
        public bool Running { get; private set; }

        public void Start(float limit)
        {
            Limit = Mathf.Max(0f, limit);
            Remaining = Limit;
            Running = HasLimit;
        }

        public void Stop() => Running = false;

        /// <summary>Advances the countdown; returns true only on the tick it runs out.</summary>
        public bool Tick(float dt)
        {
            if (!Running) return false;
            Remaining = Mathf.Max(0f, Remaining - dt);
            if (Remaining > 0f) return false;
            Running = false;
            return true;
        }

        /// <summary>3 stars with at least 2/3 of the time left, 2 with 1/3, otherwise 1. Untimed levels get 3.</summary>
        public static int ComputeStars(float remaining, float limit)
        {
            if (limit <= 0f) return 3;
            float left = remaining / limit;
            if (left >= 2f / 3f - 1e-4f) return 3;
            if (left >= 1f / 3f - 1e-4f) return 2;
            return 1;
        }
    }
}
