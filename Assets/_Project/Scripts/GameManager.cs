using System;
using System.Collections;
using UnityEngine;

namespace RollerSplat
{
    /// <summary>
    /// Watches paint progress and the level clock: declares the win at 100%, the loss when time runs out,
    /// and drives Next / Restart.
    /// </summary>
    public class GameManager : MonoBehaviour
    {
        enum State { Playing, Won, Lost }

        [SerializeField] GridManager grid;
        [SerializeField] LevelLoader loader;
        [SerializeField] BallController ball;
        [SerializeField] UIManager ui;
        [Tooltip("Popup cài đặt (không bắt buộc). Khi mở, bóng bị khoá input.")]
        [SerializeField] SettingsPanel settings;
        [Tooltip("Thời gian chờ (giây) trước khi hiện bảng thắng (để pháo hoa kịp nổ).")]
        [SerializeField, Min(0f)] float winDelay = 1.5f;
        [Tooltip("Thời gian chờ (giây) trước khi hiện bảng thua.")]
        [SerializeField, Min(0f)] float loseDelay = 0.5f;

        /// <summary>Raised the moment the last cell is painted.</summary>
        public event Action OnWon;
        /// <summary>Raised the moment the clock runs out.</summary>
        public event Action OnLost;

        State state = State.Playing;
        Coroutine popupRoutine;
        readonly LevelTimer timer = new LevelTimer();

        void Awake()
        {
            if (grid == null) Debug.LogError($"{name}: GameManager is missing 'grid'.", this);
            if (loader == null) Debug.LogError($"{name}: GameManager is missing 'loader'.", this);
            if (ball == null) Debug.LogError($"{name}: GameManager is missing 'ball'.", this);
            if (ui == null) Debug.LogWarning($"{name}: GameManager has no 'ui'; win/lose panels will not show.", this);
        }

        void OnEnable()
        {
            if (grid != null) grid.OnProgressChanged += HandleProgress;
            if (loader != null) loader.OnLevelLoaded += HandleLevelLoaded;
            if (settings != null)
            {
                settings.Opened += HandleSettingsOpened;
                settings.Closed += HandleSettingsClosed;
                settings.ProgressReset += HandleProgressReset;
            }
        }

        void OnDisable()
        {
            if (grid != null) grid.OnProgressChanged -= HandleProgress;
            if (loader != null) loader.OnLevelLoaded -= HandleLevelLoaded;
            if (settings != null)
            {
                settings.Opened -= HandleSettingsOpened;
                settings.Closed -= HandleSettingsClosed;
                settings.ProgressReset -= HandleProgressReset;
            }
        }

        void Start()
        {
            // Continue from the saved level (it may be past the end if levels were removed).
            if (loader != null) loader.Load(GameProgress.CurrentLevel < loader.LevelCount ? GameProgress.CurrentLevel : 0);
        }

        void Update()
        {
            if (state != State.Playing || !timer.Running) return;
            bool expired = timer.Tick(Time.deltaTime);
            if (ui != null) ui.SetTime(timer.Remaining, timer.HasLimit);
            if (expired) Lose();
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
            if (popupRoutine != null)
            {
                StopCoroutine(popupRoutine);
                popupRoutine = null;
            }
            state = State.Playing;
            ball.InputLocked = settings != null && settings.IsOpen;
            GameProgress.CurrentLevel = index;
            timer.Start(loader.Current != null ? loader.Current.TimeLimit : 0f);
            if (ui != null)
            {
                ui.HideWin();
                ui.HideLose();
                ui.SetLevel(index + 1);
                ui.SetProgress(grid.Model.PaintedCount, grid.Model.TotalCount);
                ui.SetTime(timer.Remaining, timer.HasLimit);
            }
            // Setup + PlaceAt report progress before this event, so a one-cell level is checked here.
            if (grid.Model.IsComplete) Win();
        }

        void Win()
        {
            state = State.Won;
            timer.Stop();
            ball.InputLocked = true;
            // Save the next level now, so quitting from the win popup doesn't replay this one.
            if (loader.LevelCount > 0) GameProgress.CurrentLevel = (loader.CurrentIndex + 1) % loader.LevelCount;
            OnWon?.Invoke();
            int stars = LevelTimer.ComputeStars(timer.Remaining, timer.Limit);
            popupRoutine = StartCoroutine(ShowAfterDelay(winDelay, () => ui.ShowWin(loader.IsLastLevel, stars)));
        }

        void Lose()
        {
            state = State.Lost;
            ball.InputLocked = true;
            OnLost?.Invoke();
            popupRoutine = StartCoroutine(ShowAfterDelay(loseDelay, () => ui.ShowLose()));
        }

        void HandleSettingsOpened() => ball.InputLocked = true;

        void HandleSettingsClosed() => ball.InputLocked = state != State.Playing;

        void HandleProgressReset() => loader.Load(0);

        IEnumerator ShowAfterDelay(float delay, Action show)
        {
            yield return new WaitForSeconds(delay);
            popupRoutine = null;
            if (ui != null) show();
        }
    }
}
