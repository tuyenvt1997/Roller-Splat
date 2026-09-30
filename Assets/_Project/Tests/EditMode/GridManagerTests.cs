using System.Collections.Generic;
using System.Text.RegularExpressions;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.Tilemaps;

namespace RollerSplat.Tests
{
    public class GridManagerTests
    {
        readonly List<Object> created = new List<Object>();
        Tilemap floor, wall;
        Level level;
        GridManager grid;
        Tile tile;

        static readonly Vector3Int A = new Vector3Int(0, 0, 0);
        static readonly Vector3Int B = new Vector3Int(1, 0, 0);
        static readonly Vector3Int C = new Vector3Int(2, 0, 0);

        [SetUp]
        public void SetUp()
        {
            var levelGo = Track(new GameObject("TestLevel"));
            var gridGo = new GameObject("Grid", typeof(Grid));
            gridGo.transform.SetParent(levelGo.transform);
            floor = new GameObject("Floor", typeof(Tilemap)).GetComponent<Tilemap>();
            floor.transform.SetParent(gridGo.transform);
            wall = new GameObject("Walls", typeof(Tilemap)).GetComponent<Tilemap>();
            wall.transform.SetParent(gridGo.transform);
            var start = new GameObject("StartPoint").transform;
            start.SetParent(levelGo.transform);

            level = levelGo.AddComponent<Level>();
            level.Init(floor, wall, start);

            grid = Track(new GameObject("GridManager")).AddComponent<GridManager>();
            tile = Track(ScriptableObject.CreateInstance<Tile>());
        }

        [TearDown]
        public void TearDown()
        {
            foreach (var o in created) if (o != null) Object.DestroyImmediate(o);
            created.Clear();
        }

        T Track<T>(T o) where T : Object { created.Add(o); return o; }

        void PaintFloor(params Vector3Int[] cells)
        {
            foreach (var c in cells) floor.SetTile(c, tile);
        }

        [Test]
        public void Setup_CountsFloorCells()
        {
            PaintFloor(A, B, C);
            Assert.IsTrue(grid.Setup(level));
            Assert.AreEqual(3, grid.Model.TotalCount);
        }

        [Test]
        public void Setup_ExcludesCellsCoveredByWall()
        {
            PaintFloor(A, B, C);
            wall.SetTile(B, tile);
            grid.Setup(level);
            Assert.AreEqual(2, grid.Model.TotalCount);
            Assert.IsFalse(grid.IsWalkable(B));
        }

        [Test]
        public void Setup_IgnoresEmptyCellsInsideBounds()
        {
            PaintFloor(A, B, C);
            floor.SetTile(B, null);
            grid.Setup(level);
            Assert.AreEqual(2, grid.Model.TotalCount);
        }

        [Test]
        public void Setup_NoWalkableCells_ReturnsFalse()
        {
            LogAssert.Expect(LogType.Error, new Regex("no walkable"));
            Assert.IsFalse(grid.Setup(level));
        }

        [Test]
        public void Setup_SetsFloorColor()
        {
            PaintFloor(A, B, C);
            grid.Setup(level);
            Assert.AreEqual(level.FloorColor, floor.GetColor(A));
        }

        [Test]
        public void PaintCell_ChangesColorAndRaisesProgress()
        {
            PaintFloor(A, B, C);
            grid.Setup(level);
            int painted = -1, total = -1;
            grid.OnProgressChanged += (p, t) => { painted = p; total = t; };

            Assert.IsTrue(grid.PaintCell(A));
            Assert.AreEqual(level.PaintColor, floor.GetColor(A));
            Assert.AreEqual(1, painted);
            Assert.AreEqual(3, total);
        }

        [Test]
        public void PaintCell_AlreadyPainted_NoEvent()
        {
            PaintFloor(A, B, C);
            grid.Setup(level);
            grid.PaintCell(A);
            int calls = 0;
            grid.OnProgressChanged += (p, t) => calls++;

            Assert.IsFalse(grid.PaintCell(A));
            Assert.AreEqual(0, calls);
        }
    }
}
