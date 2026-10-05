using UnityEngine;
using UnityEngine.Tilemaps;

namespace RollerSplat
{
    /// <summary>Data holder on the root of each level prefab.</summary>
    public class Level : MonoBehaviour
    {
        [Tooltip("Tilemap chứa các ô đường đi.")]
        [SerializeField] Tilemap floorTilemap;
        [Tooltip("Tilemap chứa tường (có thể để trống).")]
        [SerializeField] Tilemap wallTilemap;
        [Tooltip("Đặt vào giữa ô xuất phát của bóng.")]
        [SerializeField] Transform startPoint;
        [SerializeField] Color floorColor = new Color(0.42f, 0.18f, 0.08f);
        [SerializeField] Color paintColor = new Color(1f, 0.45f, 0.7f);
        [Tooltip("Thời gian giới hạn (giây). 0 = không giới hạn.")]
        [SerializeField, Min(0f)] float timeLimit = 60f;

        public Tilemap FloorTilemap => floorTilemap;
        public Tilemap WallTilemap => wallTilemap;
        public Transform StartPoint => startPoint;
        public Color FloorColor => floorColor;
        public Color PaintColor => paintColor;
        public float TimeLimit => timeLimit;

        /// <summary>Wires references from code (used by tests).</summary>
        public void Init(Tilemap floor, Tilemap wall, Transform start)
        {
            floorTilemap = floor;
            wallTilemap = wall;
            startPoint = start;
        }
    }
}
