using System;
using UnityEngine;
using UnityEngine.InputSystem;

namespace RollerSplat
{
    /// <summary>
    /// Turns arrow keys / WASD and mouse/touch swipes into one of four grid directions.
    /// </summary>
    public class SwipeInput : MonoBehaviour
    {
        public event Action<Vector3Int> OnSwipe;

        [Tooltip("Khoảng kéo tối thiểu, tính theo tỉ lệ chiều cao màn hình.")]
        [SerializeField, Range(0.01f, 0.3f)] float minSwipeFraction = 0.05f;

        readonly SwipeTracker tracker = new SwipeTracker();

        void Update()
        {
            ReadKeyboard();
            ReadPointer();
        }

        void ReadKeyboard()
        {
            var kb = Keyboard.current;
            if (kb == null) return;

            if (kb.upArrowKey.wasPressedThisFrame || kb.wKey.wasPressedThisFrame) Emit(Vector3Int.up);
            else if (kb.downArrowKey.wasPressedThisFrame || kb.sKey.wasPressedThisFrame) Emit(Vector3Int.down);
            else if (kb.leftArrowKey.wasPressedThisFrame || kb.aKey.wasPressedThisFrame) Emit(Vector3Int.left);
            else if (kb.rightArrowKey.wasPressedThisFrame || kb.dKey.wasPressedThisFrame) Emit(Vector3Int.right);
        }

        void ReadPointer()
        {
            var pointer = Pointer.current;
            if (pointer == null) return;

            var dir = tracker.Step(
                pointer.press.wasPressedThisFrame,
                pointer.press.isPressed,
                pointer.press.wasReleasedThisFrame,
                pointer.position.ReadValue(),
                minSwipeFraction * Screen.height);
            if (dir.HasValue) Emit(dir.Value);
        }

        void Emit(Vector3Int dir) => OnSwipe?.Invoke(dir);

        /// <summary>
        /// Dominant-axis direction of a drag, or null if shorter than minDistance.
        /// Ties go to the horizontal axis.
        /// </summary>
        public static Vector3Int? DirectionFromDelta(Vector2 delta, float minDistance)
        {
            if (delta.magnitude < minDistance) return null;
            if (Mathf.Abs(delta.x) >= Mathf.Abs(delta.y))
                return delta.x > 0 ? Vector3Int.right : Vector3Int.left;
            return delta.y > 0 ? Vector3Int.up : Vector3Int.down;
        }
    }
}
