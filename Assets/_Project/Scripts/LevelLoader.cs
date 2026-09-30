using System;
using System.Collections.Generic;
using UnityEngine;

namespace RollerSplat
{
    /// <summary>
    /// Spawns level prefabs, hands them to GridManager, places the ball and fits the camera.
    /// </summary>
    public class LevelLoader : MonoBehaviour
    {
        /// <summary>Raised after a level is fully set up. Argument: level index (0-based).</summary>
        public event Action<int> OnLevelLoaded;

        [Tooltip("Các prefab level theo thứ tự chơi.")]
        [SerializeField] List<Level> levels = new List<Level>();
        [Tooltip("Cha của level được tạo ra. Để trống = chính object này.")]
        [SerializeField] Transform levelRoot;
        [SerializeField] GridManager grid;
        [SerializeField] BallController ball;
        [Tooltip("Để trống = Camera.main.")]
        [SerializeField] Camera targetCamera;
        [SerializeField] bool fitCamera = true;
        [SerializeField, Min(0f)] float cameraPadding = 1f;

        Level current;

        public int CurrentIndex { get; private set; } = -1;
        public int LevelCount => levels.Count;
        public bool IsLastLevel => CurrentIndex == levels.Count - 1;

        public void Load(int index)
        {
            if (levels.Count == 0)
            {
                Debug.LogError($"{name}: LevelLoader has no levels in its list.", this);
                return;
            }
            if (index < 0 || index >= levels.Count)
            {
                Debug.LogError($"{name}: level index {index} is out of range (0..{levels.Count - 1}).", this);
                return;
            }
            if (levels[index] == null)
            {
                Debug.LogError($"{name}: level slot {index} is empty.", this);
                return;
            }
            if (grid == null || ball == null)
            {
                Debug.LogError($"{name}: LevelLoader is missing 'grid' or 'ball' reference.", this);
                return;
            }

            if (current != null)
            {
                current.gameObject.SetActive(false);
                Destroy(current.gameObject);
            }

            current = Instantiate(levels[index], levelRoot != null ? levelRoot : transform);
            current.name = levels[index].name;
            CurrentIndex = index;

            if (!grid.Setup(current)) return;

            if (current.StartPoint == null)
            {
                Debug.LogError($"{name}: level '{current.name}' has no startPoint assigned.", current);
                return;
            }
            var startCell = grid.WorldToCell(current.StartPoint.position);
            if (!grid.IsWalkable(startCell))
            {
                Debug.LogError($"{name}: startPoint of level '{current.name}' is not on a floor cell ({startCell}).", current);
                return;
            }

            ball.PlaceAt(startCell);
            if (fitCamera) FitCamera();
            OnLevelLoaded?.Invoke(index);
        }

        /// <summary>Loads the next level, wrapping back to the first after the last one.</summary>
        public void LoadNext() => Load(levels.Count == 0 ? 0 : (CurrentIndex + 1) % levels.Count);

        public void Reload() => Load(Mathf.Max(CurrentIndex, 0));

        void FitCamera()
        {
            var cam = targetCamera != null ? targetCamera : Camera.main;
            if (cam == null || !cam.orthographic) return;

            var bounds = grid.GetFloorWorldBounds();
            var pos = cam.transform.position;
            cam.transform.position = new Vector3(bounds.center.x, bounds.center.y, pos.z);
            cam.orthographicSize = ComputeOrthoSize(bounds.size, cam.aspect, cameraPadding);
        }

        /// <summary>Orthographic size that fits a world-space box of boundsSize, plus padding.</summary>
        public static float ComputeOrthoSize(Vector2 boundsSize, float aspect, float padding)
        {
            return Mathf.Max(boundsSize.y / 2f, boundsSize.x / 2f / aspect) + padding;
        }
    }
}
