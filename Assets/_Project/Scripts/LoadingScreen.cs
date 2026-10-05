using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace RollerSplat
{
    /// <summary>
    /// Boot screen: loads the game scene in the background while a ball rolls along the progress bar,
    /// painting it as it goes and flicking little paint drops behind it. The bar never runs ahead of
    /// the real load, and the screen stays up for at least <see cref="minDuration"/> seconds.
    /// </summary>
    public class LoadingScreen : MonoBehaviour
    {
        [Tooltip("Tên scene chơi game (phải có trong Build Settings).")]
        [SerializeField] string sceneToLoad = "Game";
        [Tooltip("Thời gian tối thiểu hiển thị màn loading (giây).")]
        [SerializeField, Min(0f)] float minDuration = 2.5f;

        [Header("UI")]
        [Tooltip("Vùng bên trong thanh loading, bóng lăn từ mép trái tới mép phải vùng này.")]
        [SerializeField] RectTransform track;
        [Tooltip("Image kiểu Filled / Horizontal phủ phần đã chạy.")]
        [SerializeField] Image fill;
        [Tooltip("Dùng thay cho 'fill' khi thanh loading là một Slider (vd. thanh của GUI kit).")]
        [SerializeField] Slider slider;
        [SerializeField] RectTransform ball;
        [SerializeField] TMP_Text label;
        [Tooltip("Hiện phần trăm, vd. \"75%\".")]
        [SerializeField] TMP_Text percentText;
        [Tooltip("Icon quay liên tục trong lúc tải.")]
        [SerializeField] RectTransform spinner;
        [SerializeField] float spinnerSpeed = -240f;
        [Tooltip("Mẫu giọt sơn (đang tắt), được nhân bản khi bóng lăn.")]
        [SerializeField] Image dropTemplate;

        [Header("Hiệu ứng")]
        [SerializeField, Min(0f)] float dropsPerSecond = 14f;
        [SerializeField, Min(0.05f)] float dropLifetime = 0.6f;

        struct Drop
        {
            public Image image;
            public Vector2 velocity;
            public float age;
        }

        readonly List<Drop> drops = new List<Drop>();
        readonly Stack<Image> pool = new Stack<Image>();
        float shown;
        float dropTimer;

        IEnumerator Start()
        {
            if (dropTemplate != null) dropTemplate.gameObject.SetActive(false);
            SetProgress(0f);

            var op = SceneManager.LoadSceneAsync(sceneToLoad);
            if (op == null)
            {
                Debug.LogError($"{name}: scene '{sceneToLoad}' is not in Build Settings.", this);
                yield break;
            }
            op.allowSceneActivation = false;

            float elapsed = 0f;
            while (true)
            {
                // Clamp the frame time: the first frames after a scene load can be very long and would
                // otherwise jump the bar straight to the end.
                float dt = Mathf.Min(Time.unscaledDeltaTime, 1f / 20f);
                elapsed += dt;
                // Async loads report 0..0.9 until activation is allowed.
                float loaded = Mathf.Clamp01(op.progress / 0.9f);
                float timed = minDuration > 0f ? Mathf.Clamp01(elapsed / minDuration) : 1f;
                float target = Mathf.Min(loaded, timed);
                shown = Mathf.MoveTowards(shown, target, dt * 1.5f);
                SetProgress(shown);
                UpdateLabel(elapsed);

                if (shown >= 1f && loaded >= 1f) break;
                yield return null;
            }

            yield return new WaitForSecondsRealtime(0.15f); // let the full bar register before switching
            op.allowSceneActivation = true;
        }

        void Update()
        {
            float dt = Time.unscaledDeltaTime;
            if (spinner != null) spinner.Rotate(0f, 0f, spinnerSpeed * dt);
            for (int i = drops.Count - 1; i >= 0; i--)
            {
                var d = drops[i];
                d.age += dt;
                if (d.age >= dropLifetime)
                {
                    d.image.gameObject.SetActive(false);
                    pool.Push(d.image);
                    drops.RemoveAt(i);
                    continue;
                }
                d.velocity *= Mathf.Exp(-4f * dt); // paint is thick: drops slow down quickly
                var rt = d.image.rectTransform;
                rt.anchoredPosition += d.velocity * dt;
                float t = d.age / dropLifetime;
                rt.localScale = Vector3.one * (1f - t * 0.6f);
                var c = d.image.color;
                c.a = 1f - t * t;
                d.image.color = c;
                drops[i] = d;
            }
        }

        void SetProgress(float p)
        {
            if (percentText != null) percentText.text = $"{Mathf.RoundToInt(p * 100f)}%";
            if (track == null || ball == null)
            {
                SetFill(p);
                return;
            }

            // The ball stays inside the bar; the paint reaches its centre, so it looks painted by the ball.
            var rect = track.rect;
            float width = rect.width;
            float radius = Mathf.Max(1f, ball.rect.width * 0.5f);
            float inset = Mathf.Min(radius * 0.6f, width * 0.5f);
            var previous = ball.anchoredPosition;
            float x = Mathf.Lerp(rect.xMin + inset, rect.xMax - inset, p);
            ball.anchoredPosition = new Vector2(x, previous.y);
            SetFill(width > 0f ? (x - rect.xMin) / width : p);

            // Roll: turn by the distance travelled over the ball's circumference.
            float moved = x - previous.x;
            ball.localRotation *= Quaternion.Euler(0f, 0f, -moved / radius * Mathf.Rad2Deg);

            if (moved > 0.01f && width > 0f) SpawnDrops(Time.unscaledDeltaTime);
        }

        void SetFill(float amount)
        {
            if (fill != null) fill.fillAmount = amount;
            if (slider != null) slider.normalizedValue = amount;
        }

        void SpawnDrops(float dt)
        {
            if (dropTemplate == null || dropsPerSecond <= 0f) return;
            dropTimer += dt * dropsPerSecond;
            while (dropTimer >= 1f)
            {
                dropTimer -= 1f;
                var image = pool.Count > 0 ? pool.Pop() : Instantiate(dropTemplate, dropTemplate.transform.parent);
                image.gameObject.SetActive(true);
                var rt = image.rectTransform;
                // Flick drops off the back of the ball, up and down out of the bar.
                float r = ball.rect.width * 0.5f;
                rt.anchoredPosition = ball.anchoredPosition + new Vector2(-r * 0.6f, Random.Range(-r, r) * 0.6f);
                rt.localScale = Vector3.one;
                rt.sizeDelta = Vector2.one * Random.Range(8f, 16f);
                var velocity = new Vector2(Random.Range(-260f, -60f), Random.Range(-140f, 140f));
                drops.Add(new Drop { image = image, velocity = velocity, age = 0f });
            }
        }

        void UpdateLabel(float elapsed)
        {
            if (label == null) return;
            int dots = 1 + Mathf.FloorToInt(elapsed * 3f) % 3;
            label.text = "loading" + new string('.', dots);
        }
    }
}
