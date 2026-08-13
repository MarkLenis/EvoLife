using UnityEngine;
using EvoLife.Common;

namespace EvoLife.Environment
{
    /// <summary>
    /// Water source. Non-depleting by default. Optional finite capacity/recharge is for experiments.
    /// </summary>
    public sealed class WaterSource : MonoBehaviour, IResourceNode, ISimulationTickable
    {
        [SerializeField] bool infiniteSource = true;
        [SerializeField] float maxAmount = 100f;
        [SerializeField] float currentAmount = 100f;
        [SerializeField] float drinkAmountPerRequest = 10f;
        [SerializeField] float rechargePerSecond;
        [SerializeField] ResourceRegistry registry;

        readonly WaterStock stock = new WaterStock();
        bool stockSynced;

        public ResourceKind Kind => ResourceKind.Water;
        public Vector3 Position => transform.position;
        public float AvailableAmount => Stock.Remaining;
        public float Capacity => Stock.Capacity;
        public bool IsDepleted => Stock.IsDepleted;
        public bool IsInfinite => Stock.IsInfinite;
        public WaterStock Stock
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
            bool infinite,
            float capacity,
            float remaining,
            float maxPerRequest,
            float rechargeRate = 0f,
            ResourceRegistry resourceRegistry = null)
        {
            infiniteSource = infinite;
            maxAmount = capacity;
            currentAmount = remaining;
            drinkAmountPerRequest = maxPerRequest;
            rechargePerSecond = rechargeRate;
            stock.Configure(infinite, capacity, remaining, maxPerRequest, rechargeRate);
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

        public void SetRechargeMultiplier(float multiplier) => Stock.SetRechargeMultiplier(multiplier);

        public float TryConsume(float requestedAmount) => Stock.TryConsume(requestedAmount);

        public void Tick(float deltaTimeSeconds) => Stock.Tick(deltaTimeSeconds);

        void EnsureStock()
        {
            if (stockSynced)
            {
                return;
            }

            stock.Configure(infiniteSource, maxAmount, currentAmount, drinkAmountPerRequest, rechargePerSecond);
            stockSynced = true;
        }
    }
}
