using System.Collections;
using UnityEngine;

namespace RollerSplat
{
    /// <summary>
    /// Watches paint progress, declares the win at 100% and drives Next / Restart.
    /// </summary>
    public class GameManager : MonoBehaviour
    {
        enum State { Playing, Won }

        [SerializeField] GridManager grid;
        [SerializeField] LevelLoader loader;
        [SerializeField] BallController ball;
        [SerializeField] UIManager ui;
        [Tooltip("Thời gian chờ (giây) trước khi hiện bảng thắng.")]
        [SerializeField, Min(0f)] float winDelay = 0.5f;

        State state = State.Playing;
        Coroutine winRoutine;

        void Awake()
        {
            if (grid == null) Debug.LogError($"{name}: GameManager is missing 'grid'.", this);
            if (loader == null) Debug.LogError($"{name}: GameManager is missing 'loader'.", this);
            if (ball == null) Debug.LogError($"{name}: GameManager is missing 'ball'.", this);
            if (ui == null) Debug.LogWarning($"{name}: GameManager has no 'ui'; win panel will not show.", this);
        }

        void OnEnable()
        {
            if (grid != null) grid.OnProgressChanged += HandleProgress;
            if (loader != null) loader.OnLevelLoaded += HandleLevelLoaded;
        }

        void OnDisable()
        {
            if (grid != null) grid.OnProgressChanged -= HandleProgress;
            if (loader != null) loader.OnLevelLoaded -= HandleLevelLoaded;
        }

        void Start()
        {
            if (loader != null) loader.Load(0);
        }

        // Hook these to the UI buttons' OnClick.
        public void OnNextPressed() => loader.LoadNext();
        public void OnRestartPressed() => loader.Reload();

        void HandleProgress(int painted, int total)
        {
            if (ui != null) ui.SetProgress(painted, total);
            if (state == State.Playing && total > 0 && painted == total) Win();
        }

        void HandleLevelLoaded(int index)
        {
            if (winRoutine != null)
            {
                StopCoroutine(winRoutine);
                winRoutine = null;
            }
            state = State.Playing;
            ball.InputLocked = false;
            if (ui != null)
            {
                ui.HideWin();
                ui.SetLevel(index + 1);
                ui.SetProgress(grid.Model.PaintedCount, grid.Model.TotalCount);
            }
            // Setup + PlaceAt report progress before this event, so a one-cell level is checked here.
            if (grid.Model.IsComplete) Win();
        }

        void Win()
        {
            state = State.Won;
            ball.InputLocked = true;
            winRoutine = StartCoroutine(ShowWinAfterDelay());
        }

        IEnumerator ShowWinAfterDelay()
        {
            yield return new WaitForSeconds(winDelay);
            winRoutine = null;
            if (ui != null) ui.ShowWin(loader.IsLastLevel);
        }
    }
}
