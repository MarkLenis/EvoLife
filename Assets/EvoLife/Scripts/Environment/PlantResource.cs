using UnityEngine;
using EvoLife.Common;

namespace EvoLife.Environment
{
    /// <summary>
    /// Renewable plant food node. Regeneration is owned by Environment, not Creatures.
    /// </summary>
    public sealed class PlantResource : MonoBehaviour, IResourceNode, ISimulationTickable
    {
        [SerializeField] float maxAmount = 20f;
        [SerializeField] float currentAmount = 20f;
        [SerializeField] float regenPerSecond = 0.5f;
        [SerializeField] ResourceRegistry registry;

        public ResourceKind Kind => ResourceKind.Plant;
        public Vector3 Position => transform.position;
        public float AvailableAmount => currentAmount;
        public bool IsDepleted => currentAmount <= 0f;

        void OnEnable()
        {
            if (registry == null)
            {
                registry = FindObjectOfType<ResourceRegistry>();
            }

            registry?.Register(this);
        }

        void OnDisable()
        {
            registry?.Unregister(this);
        }

        public float TryConsume(float requestedAmount)
        {
            if (requestedAmount <= 0f || currentAmount <= 0f)
            {
                return 0f;
            }

            var taken = Mathf.Min(requestedAmount, currentAmount);
            currentAmount -= taken;
            return taken;
        }

        public void Tick(float deltaTimeSeconds)
        {
            if (deltaTimeSeconds <= 0f || currentAmount >= maxAmount)
            {
                return;
            }

            currentAmount = Mathf.Min(maxAmount, currentAmount + regenPerSecond * deltaTimeSeconds);
        }
    }
}
