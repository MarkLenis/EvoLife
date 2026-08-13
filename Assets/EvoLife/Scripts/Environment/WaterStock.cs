namespace EvoLife.Environment
{
    /// <summary>
    /// Pure water stock. Infinite by default so drinking does not remove the source.
    /// Finite capacity and recharge are opt-in for experiments.
    /// </summary>
    public sealed class WaterStock
    {
        bool infinite = true;
        float capacity;
        float remaining;
        float drinkAmountPerRequest = 10f;
        float rechargePerSecond;
        float rechargeMultiplier = 1f;

        public WaterStock(
            bool infiniteSource = true,
            float capacity = 100f,
            float remaining = 100f,
            float drinkAmountPerRequest = 10f,
            float rechargePerSecond = 0f)
        {
            Configure(infiniteSource, capacity, remaining, drinkAmountPerRequest, rechargePerSecond);
        }

        public bool IsInfinite => infinite;
        public float Capacity => infinite ? float.PositiveInfinity : capacity;
        public float Remaining => infinite ? float.PositiveInfinity : remaining;
        public float DrinkAmountPerRequest => drinkAmountPerRequest;
        public bool IsDepleted => !infinite && remaining <= 0f;

        public void Configure(
            bool infiniteSource,
            float maxAmount,
            float currentAmount,
            float maxPerRequest,
            float rechargeRate)
        {
            infinite = infiniteSource;
            capacity = maxAmount < 0f ? 0f : maxAmount;
            remaining = Clamp(currentAmount, 0f, capacity);
            drinkAmountPerRequest = maxPerRequest < 0f ? 0f : maxPerRequest;
            rechargePerSecond = rechargeRate < 0f ? 0f : rechargeRate;
        }

        public void SetRechargeMultiplier(float multiplier) =>
            rechargeMultiplier = multiplier < 0f ? 0f : multiplier;

        public float TryConsume(float requestedAmount)
        {
            if (requestedAmount <= 0f)
            {
                return 0f;
            }

            var capped = drinkAmountPerRequest <= 0f
                ? requestedAmount
                : (requestedAmount < drinkAmountPerRequest ? requestedAmount : drinkAmountPerRequest);

            if (infinite)
            {
                return capped;
            }

            if (remaining <= 0f)
            {
                return 0f;
            }

            var taken = capped < remaining ? capped : remaining;
            remaining -= taken;
            return taken;
        }

        public void Tick(float deltaTimeSeconds)
        {
            if (infinite || deltaTimeSeconds <= 0f || remaining >= capacity)
            {
                return;
            }

            var rate = rechargePerSecond * rechargeMultiplier;
            if (rate <= 0f)
            {
                return;
            }

            remaining = Clamp(remaining + rate * deltaTimeSeconds, 0f, capacity);
        }

        static float Clamp(float value, float min, float max) =>
            value < min ? min : value > max ? max : value;
    }
}
