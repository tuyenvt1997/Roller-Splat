using UnityEngine;

namespace RollerSplat
{
    /// <summary>
    /// Per-frame swipe state machine, fed with pointer state by SwipeInput.
    /// Emits at most one direction per press.
    /// </summary>
    public class SwipeTracker
    {
        Vector2 pressStart;
        bool tracking;

        public Vector3Int? Step(bool pressedThisFrame, bool isPressed, bool releasedThisFrame, Vector2 position, float minDistance)
        {
            if (pressedThisFrame)
            {
                pressStart = position;
                tracking = true;
            }

            if (!tracking) return null;
            if (!isPressed && !releasedThisFrame)
            {
                tracking = false;
                return null;
            }

            // Also checked on the release frame so a fast flick is not lost.
            var dir = SwipeInput.DirectionFromDelta(position - pressStart, minDistance);
            if (dir.HasValue || releasedThisFrame) tracking = false;
            return dir;
        }
    }
}
