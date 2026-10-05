using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace RollerSplat
{
    /// <summary>Level label, progress, clock, and the win / lose popups. Every reference is optional.</summary>
    public class UIManager : MonoBehaviour
    {
        [SerializeField] TMP_Text levelText;
        [SerializeField] TMP_Text progressText;
        [Tooltip("Image kiểu Filled (Horizontal).")]
        [SerializeField] Image progressFill;

        [Header("Đồng hồ")]
        [SerializeField] TMP_Text timeText;
        [Tooltip("Dưới số giây này đồng hồ chuyển sang màu cảnh báo.")]
        [SerializeField, Min(0f)] float warningTime = 10f;
        [SerializeField] Color timeColor = Color.white;
        [SerializeField] Color warningColor = new Color(1f, 0.3f, 0.3f);

        [Header("Bảng thắng")]
        [SerializeField] GameObject winPanel;
        [SerializeField] TMP_Text winText;
        [Tooltip("Các ngôi sao theo thứ tự hiện (trái → giữa → phải).")]
        [SerializeField] Image[] stars = new Image[0];
        [SerializeField] Sprite starOn;
        [SerializeField] Sprite starOff;
        [SerializeField, Min(0f)] float starInterval = 0.3f;

        [Header("Bảng thua")]
        [SerializeField] GameObject losePanel;

        Coroutine starRoutine;

        public void SetLevel(int levelNumber)
        {
            if (levelText != null) levelText.text = $"Level {levelNumber}";
            if (winText != null) winText.text = $"LEVEL {levelNumber}";
        }

        public void SetProgress(int painted, int total)
        {
            float ratio = total > 0 ? (float)painted / total : 0f;
            if (progressText != null) progressText.text = $"{(total > 0 ? painted * 100 / total : 0)}%";
            if (progressFill != null) progressFill.fillAmount = ratio;
        }

        public void SetTime(float remaining, bool hasLimit)
        {
            if (timeText == null) return;
            timeText.gameObject.SetActive(hasLimit);
            if (!hasLimit) return;
            int seconds = Mathf.CeilToInt(remaining);
            timeText.text = $"{seconds / 60}:{seconds % 60:00}";
            timeText.color = remaining <= warningTime ? warningColor : timeColor;
        }

        public void ShowWin(bool isLastLevel, int starCount)
        {
            if (winText != null && isLastLevel) winText.text = "ALL CLEAR!";
            if (winPanel != null) winPanel.SetActive(true);
            if (starRoutine != null) StopCoroutine(starRoutine);
            starRoutine = StartCoroutine(RevealStars(starCount));
        }

        public void HideWin()
        {
            if (starRoutine != null)
            {
                StopCoroutine(starRoutine);
                starRoutine = null;
            }
            if (winPanel != null) winPanel.SetActive(false);
        }

        public void ShowLose()
        {
            if (losePanel != null) losePanel.SetActive(true);
        }

        public void HideLose()
        {
            if (losePanel != null) losePanel.SetActive(false);
        }

        IEnumerator RevealStars(int count)
        {
            foreach (var star in stars)
            {
                if (star == null) continue;
                if (starOff != null) star.sprite = starOff;
                star.rectTransform.localScale = Vector3.one;
            }

            for (int i = 0; i < stars.Length && i < count; i++)
            {
                yield return new WaitForSecondsRealtime(starInterval);
                var star = stars[i];
                if (star == null) continue;
                if (starOn != null) star.sprite = starOn;
                yield return Pop(star.rectTransform);
            }
            starRoutine = null;
        }

        static IEnumerator Pop(RectTransform rt)
        {
            const float duration = 0.25f;
            for (float t = 0f; t < duration; t += Time.unscaledDeltaTime)
            {
                // 0 → overshoot 1.25 → settle at 1.
                float k = t / duration;
                float s = k < 0.6f ? Mathf.Lerp(0f, 1.25f, k / 0.6f) : Mathf.Lerp(1.25f, 1f, (k - 0.6f) / 0.4f);
                rt.localScale = Vector3.one * s;
                yield return null;
            }
            rt.localScale = Vector3.one;
        }
    }
}
