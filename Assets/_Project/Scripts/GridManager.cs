using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace RollerSplat
{
    /// <summary>
    /// Builds a GridModel from a Level's tilemaps, paints floor tiles and reports progress.
    /// </summary>
    public class GridManager : MonoBehaviour
    {
        /// <summary>(paintedCount, totalCount)</summary>
        public event Action<int, int> OnProgressChanged;

        /// <summary>Raised right after a cell switches to the paint colour (drives the paint pop effect).</summary>
        public event Action<Vector3Int> OnCellPainted;

        /// <summary>Floor tilemap of the current level (null before Setup).</summary>
        public Tilemap Floor => floor;

        /// <summary>Level currently set up (null before Setup).</summary>
        public Level CurrentLevel => level;

        public GridModel Model { get; private set; } = new GridModel(Array.Empty<Vector3Int>());

        public Color PaintColor => level != null ? level.PaintColor : Color.white;
        public float CellSize => floor != null ? floor.layoutGrid.cellSize.x : 1f;

        Level level;
        Tilemap floor;
        Bounds floorWorldBounds;

        public bool Setup(Level newLevel)
        {
            level = newLevel;
            floor = null;
            Model = new GridModel(Array.Empty<Vector3Int>());

            if (newLevel == null)
            {
                Debug.LogError("GridManager.Setup: level is null.", this);
                return false;
            }
            if (newLevel.FloorTilemap == null)
            {
                Debug.LogError($"GridManager.Setup: level '{newLevel.name}' has no floorTilemap assigned.", newLevel);
                return false;
            }

            floor = newLevel.FloorTilemap;
            var wall = newLevel.WallTilemap;
            var cells = new List<Vector3Int>();
            foreach (var cell in floor.cellBounds.allPositionsWithin)
            {
                if (!floor.HasTile(cell)) continue;
                if (wall != null && wall.HasTile(cell)) continue;
                cells.Add(cell);
            }

            if (cells.Count == 0)
            {
                Debug.LogError($"GridManager.Setup: level '{newLevel.name}' has no walkable cells.", newLevel);
                return false;
            }

            Model = new GridModel(cells);
            floorWorldBounds = ComputeWorldBounds(cells);
            foreach (var cell in cells) SetCellColor(cell, newLevel.FloorColor);

            OnProgressChanged?.Invoke(Model.PaintedCount, Model.TotalCount);
            return true;
        }

        public List<Vector3Int> GetSlidePath(Vector3Int start, Vector3Int dir) => Model.GetSlidePath(start, dir);

        public bool IsWalkable(Vector3Int cell) => Model.IsWalkable(cell);

        /// <param name="playEffects">False paints the cell instantly without raising OnCellPainted
        /// (used for the start cell, so the level opens without a flash under the ball).</param>
        public bool PaintCell(Vector3Int cell, bool playEffects = true)
        {
            if (!Model.TryPaint(cell)) return false;
            SetCellColor(cell, level.PaintColor);
            if (playEffects) OnCellPainted?.Invoke(cell);
            OnProgressChanged?.Invoke(Model.PaintedCount, Model.TotalCount);
            return true;
        }

        public Vector3 CellToWorld(Vector3Int cell) => floor != null ? floor.GetCellCenterWorld(cell) : Vector3.zero;

        public Vector3Int WorldToCell(Vector3 world) => floor != null ? floor.WorldToCell(world) : Vector3Int.zero;

        /// <summary>World-space bounds of all walkable cells (used to fit the camera).</summary>
        public Bounds GetFloorWorldBounds() => floorWorldBounds;

        void SetCellColor(Vector3Int cell, Color color)
        {
            floor.SetTileFlags(cell, TileFlags.None);
            floor.SetColor(cell, color);
        }

        Bounds ComputeWorldBounds(List<Vector3Int> cells)
        {
            var min = cells[0];
            var max = cells[0];
            foreach (var c in cells)
            {
                min = Vector3Int.Min(min, c);
                max = Vector3Int.Max(max, c);
            }
            var b = new Bounds(floor.CellToWorld(min), Vector3.zero);
            b.Encapsulate(floor.CellToWorld(max + new Vector3Int(1, 1, 0)));
            return b;
        }
    }
}
