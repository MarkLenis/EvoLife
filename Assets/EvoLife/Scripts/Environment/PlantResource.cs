using UnityEngine;
using EvoLife.Common;

namespace EvoLife.Environment
{
    /// <summary>
    /// Renewable plant food node. Regeneration is owned by Environment, not Creatures.
    /// Plants persist and refill in place; they are not respawned every tick.
    /// </summary>
    public sealed class PlantResource : MonoBehaviour, IResourceNode, ISimulationTickable
    {
        [SerializeField] float maxAmount = 20f;
        [SerializeField] float currentAmount = 20f;
        [SerializeField] float regenPerSecond = 0.5f;
        [SerializeField] float regenDelaySeconds;
        [SerializeField] ResourceRegistry registry;

        readonly PlantStock stock = new PlantStock();
        bool stockSynced;

        public ResourceKind Kind => ResourceKind.Plant;
        public Vector3 Position => transform.position;
        public float AvailableAmount => Stock.Remaining;
        public float Capacity => Stock.Capacity;
        public bool IsDepleted => Stock.IsDepleted;
        public float RegenPerSecond => Stock.RegenPerSecond;
        public float RegenDelaySeconds => Stock.RegenDelaySeconds;
        public float EffectiveRegenPerSecond => Stock.EffectiveRegenPerSecond;
        public PlantStock Stock
        {
            get
            {
                EnsureStock();
                return stock;
            }
        }

        void Awake() => EnsureStock();

        void OnEnable()
        {
            EnsureStock();
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

        public void Configure(
            float capacity,
            float remaining,
            float regenRate,
            float regenDelay = 0f,
            ResourceRegistry resourceRegistry = null)
        {
            maxAmount = capacity;
            currentAmount = remaining;
            regenPerSecond = regenRate;
            regenDelaySeconds = regenDelay;
            stock.Configure(capacity, remaining, regenRate, regenDelay);
            stockSynced = true;
            if (resourceRegistry != null)
            {
                BindRegistry(resourceRegistry);
            }
        }

        public void BindRegistry(ResourceRegistry resourceRegistry)
        {
            if (registry == resourceRegistry)
            {
                return;
            }

            registry?.Unregister(this);
            registry = resourceRegistry;
            if (isActiveAndEnabled)
            {
                registry?.Register(this);
            }
        }

        public void SetBiomeRegenMultiplier(float multiplier) => Stock.SetBiomeRegenMultiplier(multiplier);

        public void SetEventRegenMultiplier(float multiplier) => Stock.SetEventRegenMultiplier(multiplier);

        public void AddAvailable(float amount) => Stock.AddAvailable(amount);

        public void DepleteByFraction(float fraction) => Stock.DepleteByFraction(fraction);

        public float TryConsume(float requestedAmount) => Stock.TryConsume(requestedAmount);

        public void Tick(float deltaTimeSeconds) => Stock.Tick(deltaTimeSeconds);

        void EnsureStock()
        {
            if (stockSynced)
            {
                return;
            }

            stock.Configure(maxAmount, currentAmount, regenPerSecond, regenDelaySeconds);
            stockSynced = true;
        }
    }
}
