using NUnit.Framework;
using UnityEngine;

namespace RollerSplat.Tests
{
    public class SwipeInputTests
    {
        [Test]
        public void DirectionFromDelta_BelowThreshold_Null()
        {
            Assert.IsNull(SwipeInput.DirectionFromDelta(new Vector2(3, 2), 10));
        }

        [Test]
        public void DirectionFromDelta_Right()
        {
            Assert.AreEqual(Vector3Int.right, SwipeInput.DirectionFromDelta(new Vector2(20, 5), 10));
        }

        [Test]
        public void DirectionFromDelta_Down()
        {
            Assert.AreEqual(Vector3Int.down, SwipeInput.DirectionFromDelta(new Vector2(-5, -20), 10));
        }

        [Test]
        public void DirectionFromDelta_NearDiagonal_DominantAxis()
        {
            Assert.AreEqual(Vector3Int.up, SwipeInput.DirectionFromDelta(new Vector2(14, 15), 10));
        }

        [Test]
        public void DirectionFromDelta_ExactDiagonal_PrefersHorizontal()
        {
            Assert.AreEqual(Vector3Int.left, SwipeInput.DirectionFromDelta(new Vector2(-15, 15), 10));
        }
    }
}
