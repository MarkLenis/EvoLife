namespace EvoLife.Environment
{
    /// <summary>
    /// Pure plant food stock. Regeneration is in-place; this type never respawns a node.
    /// </summary>
    public sealed class PlantStock
    {
        float capacity;
        float remaining;
        float regenPerSecond;
        float regenDelaySeconds;
        float biomeRegenMultiplier = 1f;
        float eventRegenMultiplier = 1f;
        float elapsedSeconds;
        float depletedAtSeconds = -1f;

        public PlantStock(
            float capacity = 20f,
            float remaining = 20f,
            float regenPerSecond = 0.5f,
            float regenDelaySeconds = 0f)
        {
            Configure(capacity, remaining, regenPerSecond, regenDelaySeconds);
        }

        public float Capacity => capacity;
        public float Remaining => remaining;
        public float RegenPerSecond => regenPerSecond;
        public float RegenDelaySeconds => regenDelaySeconds;
        public float EffectiveRegenPerSecond => regenPerSecond * biomeRegenMultiplier * eventRegenMultiplier;
        public bool IsDepleted => remaining <= 0f;

        public void Configure(float maxAmount, float currentAmount, float regenRate, float regenDelay)
        {
            capacity = maxAmount < 0f ? 0f : maxAmount;
            remaining = Clamp(currentAmount, 0f, capacity);
            regenPerSecond = regenRate < 0f ? 0f : regenRate;
            regenDelaySeconds = regenDelay < 0f ? 0f : regenDelay;
            if (remaining <= 0f && capacity > 0f)
            {
                depletedAtSeconds = elapsedSeconds;
            }
        }

        public void SetBiomeRegenMultiplier(float multiplier) =>
            biomeRegenMultiplier = multiplier < 0f ? 0f : multiplier;

        public void SetEventRegenMultiplier(float multiplier) =>
            eventRegenMultiplier = multiplier < 0f ? 0f : multiplier;

        public float TryConsume(float requestedAmount)
        {
            if (requestedAmount <= 0f || remaining <= 0f)
            {
                return 0f;
            }

            var taken = requestedAmount < remaining ? requestedAmount : remaining;
            remaining -= taken;
            if (remaining <= 0f)
            {
                remaining = 0f;
                depletedAtSeconds = elapsedSeconds;
            }

            return taken;
        }

        public void AddAvailable(float amount)
        {
            if (amount <= 0f || capacity <= 0f)
            {
                return;
            }

            remaining = Clamp(remaining + amount, 0f, capacity);
            if (remaining > 0f)
            {
                depletedAtSeconds = -1f;
            }
        }

        public void DepleteByFraction(float fraction)
        {
            if (fraction <= 0f || remaining <= 0f)
            {
                return;
            }

            var clamped = fraction > 1f ? 1f : fraction;
            remaining *= 1f - clamped;
            if (remaining <= 0.0001f)
            {
                remaining = 0f;
                depletedAtSeconds = elapsedSeconds;
            }
        }

        public void Tick(float deltaTimeSeconds)
        {
            if (deltaTimeSeconds <= 0f)
            {
                return;
            }

            elapsedSeconds += deltaTimeSeconds;
            if (remaining >= capacity || EffectiveRegenPerSecond <= 0f || capacity <= 0f)
            {
                return;
            }

            if (remaining <= 0f
                && depletedAtSeconds >= 0f
                && elapsedSeconds - depletedAtSeconds < regenDelaySeconds)
            {
                return;
            }

            remaining = Clamp(remaining + EffectiveRegenPerSecond * deltaTimeSeconds, 0f, capacity);
            if (remaining > 0f)
            {
                depletedAtSeconds = -1f;
            }
        }

        static float Clamp(float value, float min, float max) =>
            value < min ? min : value > max ? max : value;
    }
}
