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

        public GridModel Model { get; private set; } = new GridModel(Array.Empty<Vector3Int>());

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

        public bool PaintCell(Vector3Int cell)
        {
            if (!Model.TryPaint(cell)) return false;
            SetCellColor(cell, level.PaintColor);
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
