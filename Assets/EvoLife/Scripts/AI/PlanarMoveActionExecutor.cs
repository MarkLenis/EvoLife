using UnityEngine;

namespace EvoLife.AI
{
    /// <summary>
    /// Applies continuous actions as a planar move intent. Physics/animation come later.
    /// Action layout (v0): [0]=moveX, [1]=moveZ
    /// </summary>
    public sealed class PlanarMoveActionExecutor : MonoBehaviour, IActionExecutor
    {
        [SerializeField] float moveSpeed = 3.5f;

        Vector3 pendingVelocity;

        public int ActionSize => 2;

        public void ApplyActions(float[] actions)
        {
            if (actions == null || actions.Length < ActionSize)
            {
                pendingVelocity = Vector3.zero;
                return;
            }

            var x = Mathf.Clamp(actions[0], -1f, 1f);
            var z = Mathf.Clamp(actions[1], -1f, 1f);
            pendingVelocity = new Vector3(x, 0f, z) * moveSpeed;
        }

        void Update()
        {
            if (pendingVelocity.sqrMagnitude > 0f)
            {
                transform.position += pendingVelocity * Time.deltaTime;
            }
        }
    }
}
