using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace RollerSplat
{
    /// <summary>Level label, progress display and win panel. Every reference is optional.</summary>
    public class UIManager : MonoBehaviour
    {
        [SerializeField] TMP_Text levelText;
        [SerializeField] TMP_Text progressText;
        [Tooltip("Image kiểu Filled (Horizontal).")]
        [SerializeField] Image progressFill;
        [SerializeField] GameObject winPanel;
        [SerializeField] TMP_Text winText;

        public void SetLevel(int levelNumber)
        {
            if (levelText != null) levelText.text = $"Level {levelNumber}";
        }

        public void SetProgress(int painted, int total)
        {
            float ratio = total > 0 ? (float)painted / total : 0f;
            if (progressText != null) progressText.text = $"{(total > 0 ? painted * 100 / total : 0)}%";
            if (progressFill != null) progressFill.fillAmount = ratio;
        }

        public void ShowWin(bool isLastLevel)
        {
            if (winText != null) winText.text = isLastLevel ? "Bạn đã phá đảo!" : "Hoàn thành!";
            if (winPanel != null) winPanel.SetActive(true);
        }

        public void HideWin()
        {
            if (winPanel != null) winPanel.SetActive(false);
        }
    }
}
