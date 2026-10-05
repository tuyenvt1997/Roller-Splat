using NUnit.Framework;
using UnityEngine;

namespace RollerSplat.Tests
{
    public class SwipeTrackerTests
    {
        [Test]
        public void Step_FlickReleasedNextFrame_EmitsDirection()
        {
            var t = new SwipeTracker();
            Assert.IsNull(t.Step(true, true, false, Vector2.zero, 10));
            // Movement and release land in the same frame.
            Assert.AreEqual(Vector3Int.right, t.Step(false, false, true, new Vector2(150, 0), 10));
        }

        [Test]
        public void Step_OneSwipePerPress()
        {
            var t = new SwipeTracker();
            t.Step(true, true, false, Vector2.zero, 10);
            Assert.AreEqual(Vector3Int.up, t.Step(false, true, false, new Vector2(0, 20), 10));
            Assert.IsNull(t.Step(false, true, false, new Vector2(0, 40), 10));
            Assert.IsNull(t.Step(false, false, true, new Vector2(0, 60), 10));
        }

        [Test]
        public void Step_TapWithoutMove_Null()
        {
            var t = new SwipeTracker();
            Assert.IsNull(t.Step(true, true, false, Vector2.zero, 10));
            Assert.IsNull(t.Step(false, false, true, new Vector2(2, 1), 10));
        }
    }
}
