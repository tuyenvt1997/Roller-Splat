using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace RollerSplat
{
    /// <summary>
    /// Slides the ball until it hits a wall, painting each cell as it passes.
    /// </summary>
    public class BallController : MonoBehaviour
    {
        [SerializeField] SwipeInput input;
        [SerializeField] GridManager grid;
        [Tooltip("Tốc độ trượt trung bình, tính bằng số ô mỗi giây.")]
        [SerializeField, Min(1f)] float cellsPerSecond = 25f;

        /// <summary>Raised when a slide stops against a wall. Argument: slide direction.</summary>
        public event Action<Vector3Int> OnSlideEnded;

        public bool InputLocked { get; set; }
        public bool IsMoving { get; private set; }
        public Vector3Int CurrentCell { get; private set; }
        public Color PaintColor => grid != null ? grid.PaintColor : Color.white;
        /// <summary>Average slide speed in world units per second.</summary>
        public float SlideSpeed => cellsPerSecond * (grid != null ? grid.CellSize : 1f);

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

        /// <summary>Snaps the ball onto a cell (level start) and paints it instantly, without the pop effect.</summary>
        public void PlaceAt(Vector3Int cell)
        {
            StopAllCoroutines();
            IsMoving = false;
            CurrentCell = cell;
            transform.position = grid.CellToWorld(cell);
            grid.PaintCell(cell, playEffects: false);
        }

        void HandleSwipe(Vector3Int dir)
        {
            if (IsMoving || InputLocked || grid == null) return;

            var path = grid.GetSlidePath(CurrentCell, dir);
            if (path.Count == 0) return;

            StartCoroutine(Slide(path, dir));
        }

        /// <summary>
        /// Glides over the whole path in one continuous motion (no per-cell stops) and eases out
        /// into the wall. Each cell is painted as soon as the ball's centre crosses into it.
        /// </summary>
        IEnumerator Slide(List<Vector3Int> path, Vector3Int dir)
        {
            IsMoving = true;
            var from = transform.position;
            var to = grid.CellToWorld(path[path.Count - 1]);
            float duration = path.Count / cellsPerSecond;
            float t = 0f;
            int painted = 0;

            while (true)
            {
                t = Mathf.Min(1f, t + Time.deltaTime / duration);
                float eased = 1f - (1f - t) * (1f - t); // ease-out quad: fast start, soft stop
                transform.position = Vector3.LerpUnclamped(from, to, eased);

                // Cell i is entered once the ball is past the edge halfway between cell i-1 and cell i.
                int reached = t >= 1f ? path.Count : Mathf.Clamp(Mathf.FloorToInt(eased * path.Count + 0.5f), 0, path.Count);
                for (; painted < reached; painted++)
                {
                    CurrentCell = path[painted];
                    grid.PaintCell(path[painted]);
                }

                if (t >= 1f) break;
                yield return null;
            }

            IsMoving = false;
            OnSlideEnded?.Invoke(dir);
        }
    }
}
