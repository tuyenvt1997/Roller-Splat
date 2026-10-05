using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace RollerSplat
{
    /// <summary>Title menu: shows the level the player will continue from and starts the game.</summary>
    public class MainMenu : MonoBehaviour
    {
        [Tooltip("Tên scene chơi game (phải có trong Build Settings).")]
        [SerializeField] string gameScene = "Game";
        [SerializeField] TMP_Text levelText;
        [SerializeField] SettingsPanel settings;

        void OnEnable()
        {
            if (settings != null) settings.ProgressReset += Refresh;
            Refresh();
        }

        void OnDisable()
        {
            if (settings != null) settings.ProgressReset -= Refresh;
        }

        // Hook to the PLAY button's OnClick.
        public void OnPlayPressed() => SceneManager.LoadScene(gameScene);

        void Refresh()
        {
            if (levelText != null) levelText.text = $"LEVEL {GameProgress.CurrentLevel + 1}";
        }
    }
}
