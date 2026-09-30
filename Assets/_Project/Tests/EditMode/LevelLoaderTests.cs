using NUnit.Framework;
using UnityEngine;

namespace RollerSplat.Tests
{
    public class LevelLoaderTests
    {
        [Test]
        public void ComputeOrthoSize_Landscape_FitsHeight()
        {
            Assert.AreEqual(6f, LevelLoader.ComputeOrthoSize(new Vector2(10, 10), 16f / 9f, 1f), 1e-4);
        }

        [Test]
        public void ComputeOrthoSize_Portrait_FitsWidth()
        {
            Assert.AreEqual(10f / (9f / 16f) / 2f + 1f,
                LevelLoader.ComputeOrthoSize(new Vector2(10, 6), 9f / 16f, 1f), 1e-4);
        }
    }
}
