using System;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace RollerSplat
{
    /// <summary>
    /// Settings popup shared by the menu and the game: volume sliders, reset progress (with confirmation)
    /// and, in game, a Home button. Hook the buttons' OnClick to the public methods.
    /// </summary>
    public class SettingsPanel : MonoBehaviour
    {
        [Tooltip("Gốc của popup, được bật/tắt khi mở/đóng.")]
        [SerializeField] GameObject panel;
        [SerializeField] Slider musicSlider;
        [SerializeField] Slider sfxSlider;
        [Tooltip("Popup hỏi lại trước khi xoá tiến trình.")]
        [SerializeField] GameObject confirmPopup;
        [Tooltip("Dừng game (Time.timeScale = 0) khi popup đang mở.")]
        [SerializeField] bool pauseGame;
        [Tooltip("Scene mà nút Home quay về.")]
        [SerializeField] string menuScene = "Menu";

        public event Action Opened;
        public event Action Closed;
        /// <summary>Raised after the saved progress has been wiped.</summary>
        public event Action ProgressReset;

        public bool IsOpen => panel != null && panel.activeSelf;

        bool paused;

        void Awake()
        {
            if (musicSlider != null) musicSlider.onValueChanged.AddListener(v => GameSettings.MusicVolume = v);
            if (sfxSlider != null) sfxSlider.onValueChanged.AddListener(v => GameSettings.SfxVolume = v);
            if (panel != null) panel.SetActive(false);
            if (confirmPopup != null) confirmPopup.SetActive(false);
        }

        void OnDestroy() => Unpause();

        public void Open()
        {
            if (musicSlider != null) musicSlider.SetValueWithoutNotify(GameSettings.MusicVolume);
            if (sfxSlider != null) sfxSlider.SetValueWithoutNotify(GameSettings.SfxVolume);
            if (confirmPopup != null) confirmPopup.SetActive(false);
            if (panel != null) panel.SetActive(true);
            if (pauseGame && !paused)
            {
                paused = true;
                Time.timeScale = 0f;
            }
            Opened?.Invoke();
        }

        public void Close()
        {
            if (confirmPopup != null) confirmPopup.SetActive(false);
            if (panel != null) panel.SetActive(false);
            Unpause();
            Closed?.Invoke();
        }

        public void OnResetPressed()
        {
            if (confirmPopup != null) confirmPopup.SetActive(true);
            else ConfirmReset();
        }

        public void ConfirmReset()
        {
            GameProgress.Reset();
            ProgressReset?.Invoke();
            Close();
        }

        public void CancelReset()
        {
            if (confirmPopup != null) confirmPopup.SetActive(false);
        }

        public void OnHomePressed()
        {
            Unpause();
            SceneManager.LoadScene(menuScene);
        }

        void Unpause()
        {
            if (!paused) return;
            paused = false;
            Time.timeScale = 1f;
        }
    }
}
