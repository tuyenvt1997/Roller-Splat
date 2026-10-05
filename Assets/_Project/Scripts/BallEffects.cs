using UnityEngine;

namespace RollerSplat
{
    /// <summary>
    /// Juice for the ball: stretches along its motion while sliding, squashes and wobbles back
    /// when it hits a wall, sprays a burst of paint drops away from the wall, and leaves a fading
    /// sparkle trail along the path while it slides.
    /// </summary>
    [RequireComponent(typeof(BallController))]
    public class BallEffects : MonoBehaviour
    {
        [Header("Kéo giãn khi lăn")]
        [Tooltip("Độ giãn theo hướng lăn ở tốc độ trung bình (0.2 = dài thêm 20%).")]
        [SerializeField, Range(0f, 0.6f)] float stretch = 0.2f;

        [Header("Nảy khi chạm tường")]
        [Tooltip("Độ dẹt lúc vừa chạm tường.")]
        [SerializeField, Range(0f, 0.6f)] float impactSquash = 0.35f;
        [Tooltip("Thời gian nảy về hình tròn (giây).")]
        [SerializeField, Min(0.05f)] float wobbleTime = 0.35f;
        [Tooltip("Số lần rung qua lại trong lúc nảy.")]
        [SerializeField, Min(0.5f)] float wobbleCycles = 2f;

        [Header("Bắn sơn khi chạm tường")]
        [Tooltip("Sprite tròn cho giọt sơn. Để trống = dùng sprite của bóng.")]
        [SerializeField] Sprite splashSprite;
        [SerializeField, Range(0, 64)] int splashCount = 14;
        [Tooltip("Pha trắng vào màu sơn để giọt nổi bật trên ô vừa tô (0 = đúng màu sơn).")]
        [SerializeField, Range(0f, 1f)] float splashLighten = 0.5f;
        [Tooltip("Thứ tự vẽ của giọt sơn (trên bóng, để không bị bóng che).")]
        [SerializeField] int splashSortingOrder = 11;

        [Header("Vệt lấp lánh trên đường đi")]
        [Tooltip("Số hạt rơi lại mỗi đơn vị quãng đường (0 = tắt).")]
        [SerializeField, Min(0f)] float trailRate = 14f;
        [Tooltip("Pha trắng vào màu sơn cho hạt lấp lánh.")]
        [SerializeField, Range(0f, 1f)] float trailLighten = 0.7f;
        [Tooltip("Thứ tự vẽ của vệt (trên Floor, dưới bóng).")]
        [SerializeField] int trailSortingOrder = 8;

        BallController ball;
        SpriteRenderer sprite;
        ParticleSystem splash;
        ParticleSystem trail;
        Vector3 baseScale;
        Vector3 lastPosition;
        Vector2 impactAxis = Vector2.right;
        float impactTime = -1f;

        void Awake()
        {
            ball = GetComponent<BallController>();
            sprite = GetComponent<SpriteRenderer>();
            baseScale = transform.localScale;
            lastPosition = transform.position;
            splash = CreateSplash();
            trail = CreateTrail();
        }

        void OnEnable() => ball.OnSlideEnded += HandleSlideEnded;

        void OnDisable() => ball.OnSlideEnded -= HandleSlideEnded;

        void OnDestroy()
        {
            if (splash != null) Destroy(splash.gameObject);
            if (trail != null) Destroy(trail.gameObject);
        }

        void LateUpdate()
        {
            var position = transform.position;
            var velocity = Time.deltaTime > 0f ? (Vector2)(position - lastPosition) / Time.deltaTime : Vector2.zero;
            lastPosition = position;
            UpdateTrail(position);

            if (ball.IsMoving && velocity.sqrMagnitude > 1e-6f)
            {
                // Stretch follows speed, so the ball rounds off as the ease-out slows it into the wall.
                float amount = stretch * Mathf.Clamp(velocity.magnitude / ball.SlideSpeed, 0f, 1.5f);
                ApplyDeform(velocity.normalized, amount);
                impactTime = -1f;
            }
            else if (impactTime >= 0f)
            {
                float t = (Time.time - impactTime) / wobbleTime;
                if (t >= 1f)
                {
                    impactTime = -1f;
                    transform.localScale = baseScale;
                    return;
                }
                // Damped spring: squashed on impact, overshoots a little, settles back to round.
                float wobble = Mathf.Cos(t * wobbleCycles * Mathf.PI * 2f) * (1f - t) * (1f - t);
                ApplyDeform(impactAxis, -impactSquash * wobble);
            }
            else
            {
                transform.localScale = baseScale;
            }
        }

        void UpdateTrail(Vector3 position)
        {
            if (trail == null) return;
            // The emitter follows the ball every frame (also on level load), but only emits while sliding,
            // so teleporting to a new start cell never leaves a streak behind.
            trail.transform.position = position;
            var emission = trail.emission;
            bool sliding = ball.IsMoving && trailRate > 0f;
            if (sliding && !emission.enabled)
            {
                var main = trail.main;
                main.startColor = Color.Lerp(ball.PaintColor, Color.white, trailLighten);
            }
            emission.enabled = sliding;
        }

        /// <summary>Scales along an axis (positive = longer) while keeping roughly the same area.</summary>
        void ApplyDeform(Vector2 axis, float amount)
        {
            float along = 1f + amount;
            float across = 1f / Mathf.Sqrt(Mathf.Max(0.1f, along));
            // Axis-aligned slides only, so mapping the deformation onto x/y is enough.
            bool horizontal = Mathf.Abs(axis.x) >= Mathf.Abs(axis.y);
            transform.localScale = horizontal
                ? new Vector3(baseScale.x * along, baseScale.y * across, baseScale.z)
                : new Vector3(baseScale.x * across, baseScale.y * along, baseScale.z);
        }

        void HandleSlideEnded(Vector3Int dir)
        {
            impactAxis = new Vector2(dir.x, dir.y);
            impactTime = Time.time;
            EmitSplash(dir);
        }

        void EmitSplash(Vector3Int dir)
        {
            if (splash == null || splashCount <= 0) return;

            // Spray back from the wall: centre the emitter's arc on the direction opposite to the slide.
            var away = new Vector3(-dir.x, -dir.y, 0f);
            float radius = sprite != null ? sprite.bounds.extents.x : 0.3f;
            float awayAngle = Mathf.Atan2(away.y, away.x) * Mathf.Rad2Deg;
            splash.transform.SetPositionAndRotation(
                transform.position - away * radius * 0.8f,
                Quaternion.Euler(0f, 0f, awayAngle - SplashArc / 2f));

            var main = splash.main;
            main.startColor = Color.Lerp(ball.PaintColor, Color.white, splashLighten);
            splash.Emit(splashCount);
        }

        const float SplashArc = 170f;

        ParticleSystem CreateSplash()
        {
            var ps = CreateParticles("Splash", splashSortingOrder);

            var main = ps.main;
            main.loop = false;
            main.startLifetime = new ParticleSystem.MinMaxCurve(0.3f, 0.55f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(2.5f, 6f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.1f, 0.24f);

            var emission = ps.emission;
            emission.enabled = false; // bursts come from Emit() only

            var shape = ps.shape;
            // Flat half-circle in the XY plane, so every drop moves on screen (no wasted z velocity).
            shape.shapeType = ParticleSystemShapeType.Circle;
            shape.arc = SplashArc;
            shape.radius = 0.05f;

            var limit = ps.limitVelocityOverLifetime;
            limit.enabled = true;
            limit.limit = 1f;
            limit.dampen = 0.1f; // drops fly out fast, then slow down like thick paint
            return ps;
        }

        ParticleSystem CreateTrail()
        {
            var ps = CreateParticles("Trail", trailSortingOrder);

            var main = ps.main;
            main.loop = true;
            main.startLifetime = new ParticleSystem.MinMaxCurve(0.3f, 0.6f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(0f, 0.3f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.06f, 0.16f);

            var emission = ps.emission;
            emission.rateOverTime = 0f;
            emission.rateOverDistance = trailRate;
            emission.enabled = false; // switched on while the ball slides

            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Circle;
            shape.radius = (sprite != null ? sprite.bounds.extents.x : 0.3f) * 0.6f;

            var color = ps.colorOverLifetime;
            color.enabled = true;
            var fade = new Gradient();
            fade.SetKeys(
                new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
                new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(0f, 1f) });
            color.color = fade;

            ps.Play();
            return ps;
        }

        /// <summary>
        /// Shared setup: a world-space, round-sprite particle system that shrinks its particles away.
        /// Not parented to the ball, so squash/stretch and the ball's scale don't affect it.
        /// </summary>
        ParticleSystem CreateParticles(string suffix, int sortingOrder)
        {
            var go = new GameObject($"{name}_{suffix}");
            var ps = go.AddComponent<ParticleSystem>();
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

            var main = ps.main;
            main.playOnAwake = false;
            main.duration = 1f;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.gravityModifier = 0f;
            main.maxParticles = 128;

            var size = ps.sizeOverLifetime;
            size.enabled = true;
            size.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.EaseInOut(0f, 1f, 1f, 0f));

            var sheet = ps.textureSheetAnimation;
            var dropSprite = splashSprite != null ? splashSprite : sprite != null ? sprite.sprite : null;
            if (dropSprite != null)
            {
                sheet.enabled = true;
                sheet.mode = ParticleSystemAnimationMode.Sprites;
                sheet.AddSprite(dropSprite);
            }

            var renderer = go.GetComponent<ParticleSystemRenderer>();
            renderer.renderMode = ParticleSystemRenderMode.Billboard;
            renderer.sortingOrder = sortingOrder;
            if (sprite != null)
            {
                // Reuse the ball's sprite material so particles render with the 2D pipeline (also in builds).
                renderer.sharedMaterial = sprite.sharedMaterial;
                renderer.sortingLayerID = sprite.sortingLayerID;
            }
            return ps;
        }
    }
}
