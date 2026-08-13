using System;
using EvoLife.Common;

namespace EvoLife.Creatures
{
    /// <summary>
    /// Core biological simulation for a single creature. Pure C# with explicit tick updates.
    /// </summary>
    public sealed class CreatureBiology : ICreatureStateView
    {
        readonly MetabolicRates baseRates;
        MetabolicModifiers modifiers;

        float health;
        float hunger;
        float thirst;
        float energy;
        float age;
        bool isAlive = true;
        DeathCause? cause;

        public CreatureBiology(MetabolicRates rates, MetabolicModifiers initialModifiers = null, float startingAge = 0f)
        {
            baseRates = rates ?? throw new ArgumentNullException(nameof(rates));
            modifiers = initialModifiers ?? MetabolicModifiers.Identity;

            health = EffectiveMaxHealth;
            energy = EffectiveMaxEnergy;
            age = Math.Max(0f, startingAge);
        }

        public event Action<CreatureDiedEventArgs> Died;
        public event Action<HealthChangedEventArgs> HealthChanged;
        public event Action<CreatureStateChangedEventArgs> StateChanged;

        public MetabolicRates BaseRates => baseRates;
        public MetabolicModifiers Modifiers => modifiers;
        public bool IsAlive => isAlive;
        public DeathCause? CauseOfDeath => cause;

        public CreatureState Snapshot => BuildSnapshot();

        public float EffectiveMaxHealth => Math.Max(0f, baseRates.MaxHealth * modifiers.MaxHealthMultiplier);
        public float EffectiveMaxEnergy => Math.Max(0f, baseRates.MaxEnergy * modifiers.MaxEnergyMultiplier);
        public float EffectiveMaxAge => Math.Max(0f, baseRates.MaxAge * modifiers.MaxAgeMultiplier);

        public void Tick(float deltaTime, ActivityLevel activity = ActivityLevel.Idle)
        {
            if (!isAlive || deltaTime <= 0f)
            {
                return;
            }

            var previous = Snapshot;

            age = Math.Min(age + deltaTime, EffectiveMaxAge);
            ApplyNeedsAccumulation(deltaTime);
            ApplyActivityEnergy(deltaTime, activity);
            ApplyEnvironmentalDamage(deltaTime);

            if (age >= EffectiveMaxAge && health > 0f)
            {
                Die(DeathCause.OldAge);
            }

            PublishStateChangeIfNeeded(previous, "Tick");
        }

        public void Eat(float nutritionAmount)
        {
            if (!isAlive || nutritionAmount <= 0f)
            {
                return;
            }

            var previous = Snapshot;
            hunger = Clamp(hunger - nutritionAmount, 0f, baseRates.HungerCapacity);
            PublishStateChangeIfNeeded(previous, "Eat");
        }

        public void Drink(float amount)
        {
            if (!isAlive || amount <= 0f)
            {
                return;
            }

            var previous = Snapshot;
            thirst = Clamp(thirst - amount, 0f, baseRates.ThirstCapacity);
            PublishStateChangeIfNeeded(previous, "Drink");
        }

        public void ConsumeEnergy(float amount)
        {
            if (!isAlive || amount <= 0f)
            {
                return;
            }

            var previous = Snapshot;
            energy = Clamp(energy - amount, 0f, EffectiveMaxEnergy);
            PublishStateChangeIfNeeded(previous, "ConsumeEnergy");
        }

        public void GainEnergy(float amount)
        {
            if (!isAlive || amount <= 0f)
            {
                return;
            }

            var previous = Snapshot;
            energy = Clamp(energy + amount, 0f, EffectiveMaxEnergy);
            PublishStateChangeIfNeeded(previous, "GainEnergy");
        }

        public void TakeDamage(float amount, DeathCause deathCause = DeathCause.Environmental)
        {
            if (!isAlive || amount <= 0f)
            {
                return;
            }

            ApplyHealthDelta(-amount);

            if (health <= 0f)
            {
                Die(deathCause);
            }
        }

        public void Heal(float amount)
        {
            if (!isAlive || amount <= 0f)
            {
                return;
            }

            ApplyHealthDelta(amount);
        }

        public void Rest(float deltaTime)
        {
            if (!isAlive || deltaTime <= 0f)
            {
                return;
            }

            var previous = Snapshot;
            var recovery = baseRates.RestingRecovery * modifiers.RestingRecoveryMultiplier * deltaTime;
            energy = Clamp(energy + recovery, 0f, EffectiveMaxEnergy);
            PublishStateChangeIfNeeded(previous, "Rest");
        }

        public void Die(DeathCause deathCause = DeathCause.Unknown)
        {
            if (!isAlive)
            {
                return;
            }

            var previous = Snapshot;

            isAlive = false;
            cause = deathCause;
            health = 0f;
            energy = 0f;

            var finalState = Snapshot;
            Died?.Invoke(new CreatureDiedEventArgs(deathCause, finalState));
            StateChanged?.Invoke(new CreatureStateChangedEventArgs(previous, finalState, "Death"));
        }

        public void ApplyModifiers(MetabolicModifiers updatedModifiers)
        {
            if (updatedModifiers == null)
            {
                throw new ArgumentNullException(nameof(updatedModifiers));
            }

            var previous = Snapshot;
            modifiers = updatedModifiers;

            health = Clamp(health, 0f, EffectiveMaxHealth);
            energy = Clamp(energy, 0f, EffectiveMaxEnergy);
            age = Math.Min(age, EffectiveMaxAge);

            PublishStateChangeIfNeeded(previous, "ApplyModifiers");
        }

        void ApplyNeedsAccumulation(float deltaTime)
        {
            hunger = Clamp(
                hunger + baseRates.HungerIncreaseRate * modifiers.HungerRateMultiplier * deltaTime,
                0f,
                baseRates.HungerCapacity);

            thirst = Clamp(
                thirst + baseRates.ThirstIncreaseRate * modifiers.ThirstRateMultiplier * deltaTime,
                0f,
                baseRates.ThirstCapacity);
        }

        void ApplyActivityEnergy(float deltaTime, ActivityLevel activity)
        {
            if (activity == ActivityLevel.Resting)
            {
                var recovery = baseRates.RestingRecovery * modifiers.RestingRecoveryMultiplier * deltaTime;
                energy = Clamp(energy + recovery, 0f, EffectiveMaxEnergy);
                return;
            }

            var consumption = baseRates.PassiveEnergyConsumption * deltaTime;

            switch (activity)
            {
                case ActivityLevel.Walking:
                    consumption += baseRates.WalkingEnergyConsumption * deltaTime;
                    break;
                case ActivityLevel.Sprinting:
                    consumption += baseRates.SprintingEnergyConsumption * deltaTime;
                    break;
                case ActivityLevel.Attacking:
                    consumption += baseRates.AttackEnergyConsumption * deltaTime;
                    break;
            }

            consumption *= modifiers.EnergyConsumptionMultiplier;
            energy = Clamp(energy - consumption, 0f, EffectiveMaxEnergy);
        }

        void ApplyEnvironmentalDamage(float deltaTime)
        {
            if (hunger >= baseRates.StarvationThreshold)
            {
                ApplyHealthDelta(-baseRates.StarvationDamage * modifiers.StarvationDamageMultiplier * deltaTime);
            }

            if (thirst >= baseRates.DehydrationThreshold)
            {
                ApplyHealthDelta(-baseRates.DehydrationDamage * modifiers.DehydrationDamageMultiplier * deltaTime);
            }

            if (health <= 0f && isAlive)
            {
                var deathCause = hunger >= baseRates.StarvationThreshold
                    ? DeathCause.Starvation
                    : DeathCause.Dehydration;

                Die(deathCause);
            }
        }

        void ApplyHealthDelta(float delta)
        {
            if (delta == 0f)
            {
                return;
            }

            var previousHealth = health;
            health = Clamp(health + delta, 0f, EffectiveMaxHealth);

            if (Math.Abs(health - previousHealth) > float.Epsilon)
            {
                HealthChanged?.Invoke(new HealthChangedEventArgs(previousHealth, health, EffectiveMaxHealth));
            }
        }

        CreatureState BuildSnapshot() =>
            new CreatureState(
                health,
                EffectiveMaxHealth,
                hunger,
                thirst,
                energy,
                EffectiveMaxEnergy,
                age,
                EffectiveMaxAge,
                isAlive,
                cause);

        void PublishStateChangeIfNeeded(CreatureState previous, string changeKind)
        {
            var current = Snapshot;
            if (!StatesEqual(previous, current))
            {
                StateChanged?.Invoke(new CreatureStateChangedEventArgs(previous, current, changeKind));
            }
        }

        static bool StatesEqual(CreatureState a, CreatureState b) =>
            Math.Abs(a.Health - b.Health) < float.Epsilon
            && Math.Abs(a.MaxHealth - b.MaxHealth) < float.Epsilon
            && Math.Abs(a.Hunger - b.Hunger) < float.Epsilon
            && Math.Abs(a.Thirst - b.Thirst) < float.Epsilon
            && Math.Abs(a.Energy - b.Energy) < float.Epsilon
            && Math.Abs(a.MaxEnergy - b.MaxEnergy) < float.Epsilon
            && Math.Abs(a.Age - b.Age) < float.Epsilon
            && Math.Abs(a.MaxAge - b.MaxAge) < float.Epsilon
            && a.IsAlive == b.IsAlive
            && a.DeathCause == b.DeathCause;

        static float Clamp(float value, float min, float max) =>
            value < min ? min : value > max ? max : value;
    }
}
