using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace RollerSplat
{
    /// <summary>
    /// Makes freshly painted floor cells pop: each one shrinks, springs back slightly larger than
    /// a cell, and flashes from white into the paint colour. Cells painted along a slide pop one
    /// after another, so the path ripples behind the ball.
    /// </summary>
    [RequireComponent(typeof(GridManager))]
    public class PaintCellEffects : MonoBehaviour
    {
        [Tooltip("Thời gian ô nảy (giây).")]
        [SerializeField, Min(0.05f)] float popTime = 0.25f;
        [Tooltip("Kích thước ô lúc vừa được tô (1 = bằng ô).")]
        [SerializeField, Range(0.3f, 1f)] float popStartScale = 0.85f;
        [Tooltip("Độ phồng quá cỡ trước khi về đúng kích thước.")]
        [SerializeField, Range(0f, 0.3f)] float popOvershoot = 0.08f;
        [Tooltip("Độ loé trắng lúc đầu (0 = không loé).")]
        [SerializeField, Range(0f, 1f)] float flash = 0.6f;

        struct Pop
        {
            public Tilemap tilemap;
            public Vector3Int cell;
            public Color color;
            public float startTime;
        }

        readonly List<Pop> pops = new List<Pop>();
        GridManager grid;

        void Awake() => grid = GetComponent<GridManager>();

        void OnEnable() => grid.OnCellPainted += HandleCellPainted;

        void OnDisable() => grid.OnCellPainted -= HandleCellPainted;

        void HandleCellPainted(Vector3Int cell)
        {
            var floor = grid.Floor;
            if (floor == null) return;
            pops.Add(new Pop { tilemap = floor, cell = cell, color = floor.GetColor(cell), startTime = Time.time });
            Apply(pops[pops.Count - 1], 0f);
        }

        void Update()
        {
            for (int i = pops.Count - 1; i >= 0; i--)
            {
                var pop = pops[i];
                if (pop.tilemap == null) // level was unloaded mid-animation
                {
                    pops.RemoveAt(i);
                    continue;
                }

                float t = (Time.time - pop.startTime) / popTime;
                Apply(pop, Mathf.Min(t, 1f));
                if (t >= 1f) pops.RemoveAt(i);
            }
        }

        void Apply(Pop pop, float t)
        {
            // Grow from popStartScale to 1 + popOvershoot over the first 60%, then settle back to 1.
            const float peakAt = 0.6f;
            float peak = 1f + popOvershoot;
            float scale = t < peakAt
                ? Mathf.Lerp(popStartScale, peak, 1f - Mathf.Pow(1f - t / peakAt, 2f))
                : Mathf.Lerp(peak, 1f, Mathf.SmoothStep(0f, 1f, (t - peakAt) / (1f - peakAt)));

            pop.tilemap.SetTransformMatrix(pop.cell, Matrix4x4.Scale(new Vector3(scale, scale, 1f)));
            pop.tilemap.SetColor(pop.cell, Color.Lerp(Color.Lerp(pop.color, Color.white, flash), pop.color, t));
        }
    }
}
