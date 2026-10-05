using System.Collections;
using UnityEngine;

namespace RollerSplat
{
    /// <summary>
    /// Celebrates a cleared level: a few rockets climb from the bottom of the screen trailing sparks
    /// and burst into coloured stars that drift down and fade. Built at runtime from two particle
    /// systems (rocket + burst sub-emitter) and scaled to the camera, so it fits every level size.
    /// </summary>
    [RequireComponent(typeof(GameManager))]
    public class WinFireworks : MonoBehaviour
    {
        [SerializeField] LevelLoader loader;
        [Tooltip("Để trống = Camera.main.")]
        [SerializeField] Camera targetCamera;
        [Tooltip("Sprite tròn cho tia lửa.")]
        [SerializeField] Sprite sparkSprite;

        [SerializeField, Range(1, 12)] int rockets = 5;
        [Tooltip("Khoảng thời gian bắn hết số pháo (giây).")]
        [SerializeField, Min(0f)] float launchSpread = 1.2f;
        [SerializeField, Range(10, 200)] int starsPerBurst = 70;
        [SerializeField] Color[] colors =
        {
            new Color(1f, 0.45f, 0.7f), new Color(1f, 0.85f, 0.25f), new Color(0.35f, 0.9f, 1f),
            new Color(0.5f, 1f, 0.45f), new Color(1f, 0.55f, 0.2f), new Color(0.75f, 0.5f, 1f),
        };
        [Tooltip("Thứ tự vẽ (trên mọi thứ trong màn chơi).")]
        [SerializeField] int sortingOrder = 50;

        const float Gravity = 9.81f; // particle gravity uses Physics.gravity

        GameManager game;
        Camera cam;
        ParticleSystem rocketSystem;
        Coroutine show;

        void Awake()
        {
            game = GetComponent<GameManager>();
            cam = targetCamera != null ? targetCamera : Camera.main;
            rocketSystem = CreateRockets();
        }

        void OnEnable()
        {
            game.OnWon += Launch;
            if (loader != null) loader.OnLevelLoaded += HandleLevelLoaded;
        }

        void OnDisable()
        {
            game.OnWon -= Launch;
            if (loader != null) loader.OnLevelLoaded -= HandleLevelLoaded;
        }

        void OnDestroy()
        {
            if (rocketSystem != null) Destroy(rocketSystem.gameObject);
        }

        void HandleLevelLoaded(int index)
        {
            // Next / Restart: don't let the last celebration spill over the new level.
            if (show != null) StopCoroutine(show);
            show = null;
            rocketSystem.Clear(true);
            rocketSystem.Play(false);
        }

        void Launch()
        {
            if (cam == null) return;
            if (show != null) StopCoroutine(show);
            show = StartCoroutine(LaunchRockets());
        }

        IEnumerator LaunchRockets()
        {
            float scale = cam.orthographicSize / 5f; // keep the show the same size on screen for every level
            ApplyScale(scale);

            float halfH = cam.orthographicSize, halfW = halfH * cam.aspect;
            var center = cam.transform.position;
            for (int i = 0; i < rockets; i++)
            {
                // Spread launch spots across the width, with a little jitter.
                float lane = rockets == 1 ? 0.5f : i / (rockets - 1f);
                float x = center.x + Mathf.Lerp(-halfW * 0.7f, halfW * 0.7f, lane) + Random.Range(-0.1f, 0.1f) * halfW;
                float startY = center.y - halfH * 1.05f;
                float apexY = center.y + Random.Range(0.05f, 0.55f) * halfH;

                // Launch speed and flight time so the rocket stops (and bursts) exactly at its apex.
                float height = apexY - startY;
                float speed = Mathf.Sqrt(2f * Gravity * height);
                var emit = new ParticleSystem.EmitParams
                {
                    position = new Vector3(x, startY, 0f),
                    velocity = new Vector3(Random.Range(-0.05f, 0.05f) * speed, speed, 0f),
                    startLifetime = speed / Gravity,
                    startColor = colors.Length > 0 ? colors[Random.Range(0, colors.Length)] : Color.white,
                    startSize = 0.18f * scale,
                    applyShapeToPosition = false,
                };
                rocketSystem.Emit(emit, 1);

                if (i < rockets - 1) yield return new WaitForSeconds(launchSpread / Mathf.Max(1, rockets - 1));
            }
            show = null;
        }

        void ApplyScale(float scale)
        {
            var burst = rocketSystem.subEmitters.GetSubEmitterSystem(1);
            var main = burst.main;
            main.startSpeed = new ParticleSystem.MinMaxCurve(2f * scale, 5f * scale);
            main.startSize = new ParticleSystem.MinMaxCurve(0.08f * scale, 0.2f * scale);

            var trail = rocketSystem.subEmitters.GetSubEmitterSystem(0);
            var trailMain = trail.main;
            trailMain.startSize = new ParticleSystem.MinMaxCurve(0.05f * scale, 0.1f * scale);
        }

        // ---------- Particle setup ----------

        ParticleSystem CreateRockets()
        {
            var rocket = CreateSystem("WinFireworks", null);
            var main = rocket.main;
            main.gravityModifier = 1f;
            main.maxParticles = 32;
            main.loop = true; // keep the system alive; rockets themselves are emitted one by one from code
            var emission = rocket.emission;
            emission.enabled = false;
            // The rocket head stays solid until it bursts.
            var rocketSize = rocket.sizeOverLifetime;
            rocketSize.enabled = false;
            var rocketColor = rocket.colorOverLifetime;
            rocketColor.enabled = false;

            // Sparks falling off the rocket while it climbs.
            var trail = CreateSystem("RocketTrail", rocket.transform);
            var trailMain = trail.main;
            trailMain.startLifetime = new ParticleSystem.MinMaxCurve(0.2f, 0.45f);
            trailMain.startSpeed = new ParticleSystem.MinMaxCurve(0f, 0.4f);
            trailMain.gravityModifier = 0.2f;
            trailMain.maxParticles = 500;
            var trailEmission = trail.emission;
            trailEmission.rateOverTime = 60f;

            // The burst: stars flung out in a circle, slowed by drag, pulled gently down, fading out.
            var burst = CreateSystem("Burst", rocket.transform);
            var burstMain = burst.main;
            burstMain.startLifetime = new ParticleSystem.MinMaxCurve(0.8f, 1.4f);
            burstMain.gravityModifier = 0.25f;
            burstMain.maxParticles = 2000;
            var burstEmission = burst.emission;
            burstEmission.rateOverTime = 0f;
            burstEmission.SetBursts(new[] { new ParticleSystem.Burst(0f, (short)starsPerBurst) });
            var shape = burst.shape;
            shape.enabled = true;
            // A sphere seen from the front gives a full round burst; a circle would be turned edge-on
            // because sub-emitter shapes follow the parent particle's direction.
            shape.shapeType = ParticleSystemShapeType.Sphere;
            shape.radius = 0.01f;
            var drag = burst.limitVelocityOverLifetime;
            drag.enabled = true;
            drag.limit = 0.3f;
            drag.dampen = 0.06f;

            var subs = rocket.subEmitters;
            subs.enabled = true;
            subs.AddSubEmitter(trail, ParticleSystemSubEmitterType.Birth, ParticleSystemSubEmitterProperties.InheritColor);
            subs.AddSubEmitter(burst, ParticleSystemSubEmitterType.Death, ParticleSystemSubEmitterProperties.InheritColor);
            rocket.Play(false); // sub-emitters are driven by the rocket particles, never on their own
            return rocket;
        }

        ParticleSystem CreateSystem(string systemName, Transform parent)
        {
            var go = new GameObject(systemName);
            if (parent != null) go.transform.SetParent(parent, false);
            var ps = go.AddComponent<ParticleSystem>();
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

            var main = ps.main;
            main.playOnAwake = false;
            main.loop = parent != null; // sub-emitters must loop to keep emitting while their parent lives
            main.duration = 1f;
            main.simulationSpace = ParticleSystemSimulationSpace.World;

            var shape = ps.shape;
            shape.enabled = false;

            var size = ps.sizeOverLifetime;
            size.enabled = true;
            size.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.EaseInOut(0f, 1f, 1f, 0.2f));

            var color = ps.colorOverLifetime;
            color.enabled = true;
            var fade = new Gradient();
            fade.SetKeys(
                new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
                new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(1f, 0.6f), new GradientAlphaKey(0f, 1f) });
            color.color = fade;

            if (sparkSprite != null)
            {
                var sheet = ps.textureSheetAnimation;
                sheet.enabled = true;
                sheet.mode = ParticleSystemAnimationMode.Sprites;
                sheet.AddSprite(sparkSprite);
            }

            var renderer = go.GetComponent<ParticleSystemRenderer>();
            renderer.renderMode = ParticleSystemRenderMode.Billboard;
            renderer.sortingOrder = sortingOrder;
            // Same sprite material as the ball, so it renders with the 2D pipeline (also in builds).
            var ball = FindFirstObjectByType<BallController>();
            var ballSprite = ball != null ? ball.GetComponent<SpriteRenderer>() : null;
            if (ballSprite != null) renderer.sharedMaterial = ballSprite.sharedMaterial;
            return ps;
        }
    }
}
