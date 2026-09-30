using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace RollerSplat
{
    /// <summary>
    /// Slides the ball cell by cell until it hits a wall, painting each cell as it arrives.
    /// </summary>
    public class BallController : MonoBehaviour
    {
        [SerializeField] SwipeInput input;
        [SerializeField] GridManager grid;
        [Tooltip("Tốc độ trượt, tính bằng số ô mỗi giây.")]
        [SerializeField, Min(1f)] float cellsPerSecond = 15f;

        public bool InputLocked { get; set; }
        public bool IsMoving { get; private set; }
        public Vector3Int CurrentCell { get; private set; }

        void Awake()
        {
            if (input == null) Debug.LogError($"{name}: BallController is missing 'input' (SwipeInput).", this);
            if (grid == null) Debug.LogError($"{name}: BallController is missing 'grid' (GridManager).", this);
        }

        void OnEnable()
        {
            if (input != null) input.OnSwipe += HandleSwipe;
        }

        void OnDisable()
        {
            if (input != null) input.OnSwipe -= HandleSwipe;
        }

        /// <summary>Snaps the ball onto a cell (level start) and paints it.</summary>
        public void PlaceAt(Vector3Int cell)
        {
            StopAllCoroutines();
            IsMoving = false;
            CurrentCell = cell;
            transform.position = grid.CellToWorld(cell);
            grid.PaintCell(cell);
        }

        void HandleSwipe(Vector3Int dir)
        {
            if (IsMoving || InputLocked || grid == null) return;

            var path = grid.GetSlidePath(CurrentCell, dir);
            if (path.Count == 0) return;

            StartCoroutine(Slide(path, dir));
        }

        IEnumerator Slide(List<Vector3Int> path, Vector3Int dir)
        {
            IsMoving = true;
            float cellSize = Vector3.Distance(grid.CellToWorld(CurrentCell), grid.CellToWorld(CurrentCell + dir));
            float step = cellsPerSecond * cellSize;

            foreach (var cell in path)
            {
                var target = grid.CellToWorld(cell);
                while (transform.position != target)
                {
                    transform.position = Vector3.MoveTowards(transform.position, target, step * Time.deltaTime);
                    yield return null;
                }
                CurrentCell = cell;
                grid.PaintCell(cell);
            }

            IsMoving = false;
        }
    }
}
