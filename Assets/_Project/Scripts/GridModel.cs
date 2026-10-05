using System.Collections.Generic;
using UnityEngine;

namespace RollerSplat
{
    /// <summary>
    /// Pure grid logic: which cells are walkable, which are painted, and where a slide stops.
    /// Any cell that is not walkable counts as a wall.
    /// </summary>
    public class GridModel
    {
        readonly HashSet<Vector3Int> walkable;
        readonly HashSet<Vector3Int> painted = new HashSet<Vector3Int>();

        public GridModel(IEnumerable<Vector3Int> walkableCells)
        {
            walkable = new HashSet<Vector3Int>(walkableCells);
        }

        public int TotalCount => walkable.Count;
        public int PaintedCount => painted.Count;
        public bool IsComplete => TotalCount > 0 && PaintedCount == TotalCount;
        public IEnumerable<Vector3Int> Cells => walkable;

        public bool IsWalkable(Vector3Int cell) => walkable.Contains(cell);
        public bool IsPainted(Vector3Int cell) => painted.Contains(cell);

        /// <summary>Paints the cell. Returns false if it is not walkable or already painted.</summary>
        public bool TryPaint(Vector3Int cell) => walkable.Contains(cell) && painted.Add(cell);

        /// <summary>Cells passed through when sliding from start in dir until the next cell is a wall.</summary>
        public List<Vector3Int> GetSlidePath(Vector3Int start, Vector3Int dir)
        {
            var path = new List<Vector3Int>();
            if (dir == Vector3Int.zero) return path;

            var next = start + dir;
            while (walkable.Contains(next))
            {
                path.Add(next);
                next += dir;
            }
            return path;
        }
    }
}
