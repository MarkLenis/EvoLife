using UnityEngine;

namespace EvoLife.Environment
{
    /// <summary>
    /// Water source. Non-depleting by default for v0 skeleton.
    /// </summary>
    public sealed class WaterSource : MonoBehaviour, IResourceNode
    {
        [SerializeField] float drinkAmountPerRequest = 10f;

        public ResourceKind Kind => ResourceKind.Water;
        public Vector3 Position => transform.position;
        public float AvailableAmount => float.PositiveInfinity;
        public bool IsDepleted => false;

        public float TryConsume(float requestedAmount)
        {
            if (requestedAmount <= 0f)
            {
                return 0f;
            }

            return Mathf.Min(requestedAmount, drinkAmountPerRequest);
        }
    }
}
