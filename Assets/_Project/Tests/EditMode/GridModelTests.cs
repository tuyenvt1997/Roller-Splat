using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

namespace RollerSplat.Tests
{
    public class GridModelTests
    {
        static IEnumerable<Vector3Int> Row(int n)
        {
            for (int x = 0; x < n; x++) yield return new Vector3Int(x, 0, 0);
        }

        // (0,0),(1,0),(2,0),(2,1),(2,2)
        static GridModel LShapeModel() => new GridModel(new[]
        {
            new Vector3Int(0, 0, 0), new Vector3Int(1, 0, 0), new Vector3Int(2, 0, 0),
            new Vector3Int(2, 1, 0), new Vector3Int(2, 2, 0),
        });

        [Test]
        public void GetSlidePath_StraightRow_StopsBeforeWall()
        {
            var m = new GridModel(Row(4));
            var path = m.GetSlidePath(new Vector3Int(0, 0, 0), Vector3Int.right);
            CollectionAssert.AreEqual(
                new[] { new Vector3Int(1, 0, 0), new Vector3Int(2, 0, 0), new Vector3Int(3, 0, 0) }, path);
        }

        [Test]
        public void GetSlidePath_BlockedImmediately_ReturnsEmpty()
        {
            Assert.IsEmpty(new GridModel(Row(4)).GetSlidePath(Vector3Int.zero, Vector3Int.left));
        }

        [Test]
        public void GetSlidePath_LShape_StopsAtCorner()
        {
            var path = LShapeModel().GetSlidePath(Vector3Int.zero, Vector3Int.right);
            Assert.AreEqual(2, path.Count);
            Assert.AreEqual(new Vector3Int(2, 0, 0), path[^1]);
        }

        [Test]
        public void TryPaint_SameCellTwice_CountsOnce()
        {
            var m = new GridModel(Row(3));
            Assert.IsTrue(m.TryPaint(Vector3Int.zero));
            Assert.IsFalse(m.TryPaint(Vector3Int.zero));
            Assert.AreEqual(1, m.PaintedCount);
        }

        [Test]
        public void TryPaint_NonWalkable_ReturnsFalse()
        {
            var m = new GridModel(Row(3));
            Assert.IsFalse(m.TryPaint(new Vector3Int(5, 5, 0)));
            Assert.AreEqual(0, m.PaintedCount);
        }

        [Test]
        public void IsComplete_AllPainted_True()
        {
            var m = new GridModel(Row(2));
            m.TryPaint(new Vector3Int(0, 0, 0));
            Assert.IsFalse(m.IsComplete);
            m.TryPaint(new Vector3Int(1, 0, 0));
            Assert.IsTrue(m.IsComplete);
        }

        [Test]
        public void IsComplete_EmptyGrid_False()
        {
            Assert.IsFalse(new GridModel(new Vector3Int[0]).IsComplete);
        }
    }
}
