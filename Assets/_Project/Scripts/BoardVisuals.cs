using UnityEngine;
using UnityEngine.Tilemaps;

namespace RollerSplat
{
    /// <summary>
    /// "Carved wooden board" look: a wood-grain board fills the screen, walls are simply the board
    /// surface (the wall tilemap is hidden), and the floor reads as a pit sunk into it. Depth comes
    /// from an overlay tilemap built per level: along every wall edge facing the viewer the wall's
    /// side face is drawn inside the wall cell (so every floor cell keeps its full size) and a soft
    /// shadow falls into the pit; the left edges get a side shadow and the right/bottom edges a thin
    /// crease line. The board rim around the pit gets a light bevel highlight. All textures are
    /// generated at runtime.
    /// </summary>
    public class BoardVisuals : MonoBehaviour
    {
        [SerializeField] LevelLoader loader;
        [SerializeField] GridManager grid;
        [Tooltip("Để trống = Camera.main.")]
        [SerializeField] Camera targetCamera;

        [Header("Ván gỗ")]
        [SerializeField] Color woodColor = new Color32(0xEE, 0x9A, 0x5E, 0xFF);
        [Tooltip("Màu vân gỗ.")]
        [SerializeField] Color grainColor = new Color32(0xD9, 0x80, 0x48, 0xFF);
        [Tooltip("Kích thước một mảng vân gỗ (đơn vị world).")]
        [SerializeField, Min(1f)] float grainTileSize = 6f;

        [Header("Chiều sâu rãnh")]
        [Tooltip("Màu mặt bên của tường (phần dày nhìn thấy phía dưới mép tường).")]
        [SerializeField] Color wallFaceColor = new Color32(0xC9, 0x6E, 0x3C, 0xFF);
        [Tooltip("Độ cao mặt bên tường, tính theo tỉ lệ một ô.")]
        [SerializeField, Range(0f, 0.5f)] float wallFaceHeight = 0.18f;
        [Tooltip("Độ dài bóng đổ xuống rãnh, tính theo tỉ lệ một ô.")]
        [SerializeField, Range(0f, 1f)] float shadowLength = 0.35f;
        [SerializeField, Range(0f, 1f)] float shadowStrength = 0.35f;
        [Tooltip("Màu viền sáng trên mép ván quanh rãnh.")]
        [SerializeField] Color rimColor = new Color32(0xFF, 0xC0, 0x8A, 0xFF);
        [Tooltip("Độ dày viền sáng, tính theo tỉ lệ một ô.")]
        [SerializeField, Range(0f, 0.2f)] float rimWidth = 0.05f;
        [Tooltip("Thứ tự vẽ lớp chiều sâu: trên Floor (0), dưới hiệu ứng bóng.")]
        [SerializeField] int depthSortingOrder = 2;

        const int CellPixels = 64;
        const int North = 1, South = 2, West = 4, East = 8;

        // Seen from a wall cell next to the pit: the pit lies on the opposite side of dir.
        static readonly (Vector3Int dir, int side)[] Neighbours =
        {
            (Vector3Int.up, South), (Vector3Int.down, North), (Vector3Int.left, East), (Vector3Int.right, West),
        };

        readonly Tile[] edgeTiles = new Tile[16];
        readonly Tile[] rimTiles = new Tile[16];
        SpriteRenderer board;
        Camera cam;

        void Awake()
        {
            cam = targetCamera != null ? targetCamera : Camera.main;
            if (cam != null) cam.backgroundColor = woodColor;
            board = CreateBoard();
            for (int mask = 1; mask < 16; mask++)
            {
                edgeTiles[mask] = CreateEdgeTile(mask);
                rimTiles[mask] = CreateRimTile(mask);
            }
        }

        void OnEnable()
        {
            if (loader != null) loader.OnLevelLoaded += HandleLevelLoaded;
        }

        void OnDisable()
        {
            if (loader != null) loader.OnLevelLoaded -= HandleLevelLoaded;
        }

        void LateUpdate()
        {
            // Keep the board covering the whole view; the camera re-fits on every level load.
            if (cam == null || board == null) return;
            float h = cam.orthographicSize * 2f + 2f;
            float w = h * cam.aspect + 2f;
            var p = cam.transform.position;
            board.transform.position = new Vector3(p.x, p.y, 1f);
            board.size = new Vector2(w, h);
        }

        void HandleLevelLoaded(int index)
        {
            var level = grid != null ? grid.CurrentLevel : null;
            var floor = grid != null ? grid.Floor : null;
            if (level == null || floor == null) return;

            var walls = level.WallTilemap;
            if (walls != null)
            {
                var wallRenderer = walls.GetComponent<TilemapRenderer>();
                if (wallRenderer != null) wallRenderer.enabled = false; // walls are the board itself
            }

            // The overlay lives under the level's Grid, so it is destroyed with the level.
            var go = new GameObject("Depth", typeof(Tilemap), typeof(TilemapRenderer));
            go.transform.SetParent(floor.transform.parent, false);
            go.GetComponent<TilemapRenderer>().sortingOrder = depthSortingOrder;
            var depth = go.GetComponent<Tilemap>();

            foreach (var cell in grid.Model.Cells)
            {
                int mask = 0;
                if (!grid.IsWalkable(cell + Vector3Int.up)) mask |= North;
                if (!grid.IsWalkable(cell + Vector3Int.down)) mask |= South;
                if (!grid.IsWalkable(cell + Vector3Int.left)) mask |= West;
                if (!grid.IsWalkable(cell + Vector3Int.right)) mask |= East;
                if (mask != 0) depth.SetTile(cell, edgeTiles[mask]);

                // Bevel highlight on the board cells around the pit; bits mark which side the pit is on.
                foreach (var (dir, side) in Neighbours)
                {
                    var wall = cell + dir;
                    if (grid.IsWalkable(wall)) continue;
                    var tile = depth.GetTile(wall) as Tile;
                    int rim = side;
                    for (int m = 1; m < 16; m++)
                        if (tile == rimTiles[m]) rim |= m;
                    depth.SetTile(wall, rimTiles[rim]);
                }
            }
        }

        // ---------- Generated art ----------

        SpriteRenderer CreateBoard()
        {
            const int size = 256;
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Repeat };
            // Grain lines: (x position, sway amplitude, phase). Each sways on its own, so they drift
            // apart and together like real grain instead of running in parallel.
            var lines = new[] { (20f, 9f, 0.3f), (74f, 6f, 2.1f), (118f, 11f, 4.0f), (170f, 7f, 1.2f), (215f, 10f, 5.1f) };
            for (int y = 0; y < size; y++)
            {
                // Only whole periods over the texture height, so the pattern tiles seamlessly.
                float v = y / (float)size * Mathf.PI * 2f;
                for (int x = 0; x < size; x++)
                {
                    float d = float.MaxValue;
                    foreach (var (lx, amp, phase) in lines)
                    {
                        float cx = lx + amp * Mathf.Sin(v + phase) + amp * 0.3f * Mathf.Sin(v * 2f + phase * 1.7f);
                        float dl = Mathf.Abs(Mathf.Repeat(x - cx, size));
                        d = Mathf.Min(d, Mathf.Min(dl, size - dl));
                    }
                    float grain = 1f - Edge(1f, 3f, d);
                    // Broad, soft bands between the grain lines so the board doesn't look flat.
                    float band = 0.5f + 0.5f * Mathf.Sin(x / (float)size * Mathf.PI * 2f * 2f + 0.5f * Mathf.Sin(v));
                    var baseColor = Color.Lerp(woodColor, grainColor, band * 0.2f);
                    tex.SetPixel(x, y, Color.Lerp(baseColor, grainColor, grain * 0.6f));
                }
            }
            tex.Apply();

            var sprite = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f),
                size / grainTileSize, 0, SpriteMeshType.FullRect);
            var go = new GameObject("Board");
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = sprite;
            sr.drawMode = SpriteDrawMode.Tiled;
            sr.sortingOrder = -100;
            return sr;
        }

        Tile CreateEdgeTile(int mask)
        {
            const int n = CellPixels;
            var tex = new Texture2D(n, n, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp };
            float shadow = Mathf.Max(1f, shadowLength * n);
            var crease = new Color(0f, 0f, 0f, 0.25f);

            for (int y = 0; y < n; y++)
            for (int x = 0; x < n; x++)
            {
                int fromTop = n - 1 - y;
                var c = Color.clear;

                if ((mask & West) != 0) // side shadow cast by the left wall
                    c = Over(c, new Color(0f, 0f, 0f, shadowStrength * 0.6f * Falloff(x, shadow * 0.4f)));
                if ((mask & East) != 0 && x >= n - 2) c = Over(c, crease);
                if ((mask & South) != 0 && y <= 1) c = Over(c, crease);

                if ((mask & North) != 0) // shadow cast into the pit by the wall above (its face is in the wall cell)
                    c = Over(c, new Color(0f, 0f, 0f, shadowStrength * Falloff(fromTop, shadow)));
                tex.SetPixel(x, y, c);
            }
            tex.Apply();

            var tile = ScriptableObject.CreateInstance<Tile>();
            tile.sprite = Sprite.Create(tex, new Rect(0, 0, n, n), new Vector2(0.5f, 0.5f), n);
            tile.colliderType = Tile.ColliderType.None;
            return tile;
        }

        /// <summary>
        /// Board cell next to the pit (mask = pit sides): a light bevel line along those edges and,
        /// when the pit lies below, the wall's side face in the bottom of the cell.
        /// </summary>
        Tile CreateRimTile(int mask)
        {
            const int n = CellPixels;
            var tex = new Texture2D(n, n, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp };
            float width = Mathf.Max(1f, rimWidth * n);
            int face = (mask & South) != 0 ? Mathf.RoundToInt(wallFaceHeight * n) : 0;
            for (int y = 0; y < n; y++)
            for (int x = 0; x < n; x++)
            {
                if (y < face)
                {
                    // Wall side face: lighter at its top edge, darker where it meets the pit floor.
                    var faceColor = Color.Lerp(wallFaceColor * 0.85f, wallFaceColor * 1.08f, y / (float)face);
                    faceColor.a = 1f;
                    tex.SetPixel(x, y, faceColor);
                    continue;
                }

                float d = float.MaxValue;
                if ((mask & South) != 0) d = Mathf.Min(d, y - face); // bevel sits on top of the face
                if ((mask & North) != 0) d = Mathf.Min(d, n - 1 - y);
                if ((mask & West) != 0) d = Mathf.Min(d, x);
                if ((mask & East) != 0) d = Mathf.Min(d, n - 1 - x);
                var c = rimColor;
                c.a = 1f - Edge(width * 0.5f, width, d);
                tex.SetPixel(x, y, c);
            }
            tex.Apply();

            var tile = ScriptableObject.CreateInstance<Tile>();
            tile.sprite = Sprite.Create(tex, new Rect(0, 0, n, n), new Vector2(0.5f, 0.5f), n);
            tile.colliderType = Tile.ColliderType.None;
            return tile;
        }

        /// <summary>GLSL-style smoothstep: 0 below <paramref name="a"/>, 1 above <paramref name="b"/>.</summary>
        static float Edge(float a, float b, float x) => Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(a, b, x));

        /// <summary>1 at the edge, fading smoothly to 0 at <paramref name="length"/> pixels away.</summary>
        static float Falloff(float distance, float length)
        {
            float t = Mathf.Clamp01(distance / length);
            return (1f - t) * (1f - t);
        }

        /// <summary>Alpha-composites <paramref name="top"/> over <paramref name="bottom"/>.</summary>
        static Color Over(Color bottom, Color top)
        {
            float a = top.a + bottom.a * (1f - top.a);
            if (a <= 0f) return Color.clear;
            var rgb = ((Vector4)top * top.a + (Vector4)bottom * bottom.a * (1f - top.a)) / a;
            return new Color(rgb.x, rgb.y, rgb.z, a);
        }
    }
}
