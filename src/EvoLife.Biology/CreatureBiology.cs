using System;

namespace EvoLife.Biology
{
    /// <summary>
    /// Core biological simulation for a single creature. Pure C# with explicit tick updates.
    /// </summary>
    public sealed class CreatureBiology : ICreatureStateView
    {
        private readonly MetabolicRates _baseRates;
        private MetabolicModifiers _modifiers;

        private float _health;
        private float _hunger;
        private float _thirst;
        private float _energy;
        private float _age;
        private bool _isAlive = true;
        private DeathCause? _cause;

        public CreatureBiology(MetabolicRates baseRates, MetabolicModifiers? modifiers = null, float? startingAge = null)
        {
            _baseRates = baseRates ?? throw new ArgumentNullException(nameof(baseRates));
            _modifiers = modifiers ?? MetabolicModifiers.Identity;

            _health = EffectiveMaxHealth;
            _energy = EffectiveMaxEnergy;
            _age = Math.Max(0f, startingAge ?? 0f);
        }

        public event Action<CreatureDiedEventArgs>? Died;
        public event Action<HealthChangedEventArgs>? HealthChanged;
        public event Action<CreatureStateChangedEventArgs>? StateChanged;

        public MetabolicRates BaseRates => _baseRates;
        public MetabolicModifiers Modifiers => _modifiers;
        public bool IsAlive => _isAlive;
        public DeathCause? CauseOfDeath => _cause;

        public CreatureState Snapshot => BuildSnapshot();

        public float EffectiveMaxHealth => Math.Max(0f, _baseRates.MaxHealth * _modifiers.MaxHealthMultiplier);
        public float EffectiveMaxEnergy => Math.Max(0f, _baseRates.MaxEnergy * _modifiers.MaxEnergyMultiplier);
        public float EffectiveMaxAge => Math.Max(0f, _baseRates.MaxAge * _modifiers.MaxAgeMultiplier);

        /// <summary>
        /// Advances metabolism for <paramref name="deltaTime"/> seconds.
        /// Call from a simulation loop, not necessarily Unity's Update().
        /// </summary>
        public void Tick(float deltaTime, ActivityLevel activity = ActivityLevel.Idle)
        {
            if (!_isAlive || deltaTime <= 0f)
            {
                return;
            }

            var previous = Snapshot;

            _age = Math.Min(_age + deltaTime, EffectiveMaxAge);
            ApplyNeedsAccumulation(deltaTime);
            ApplyActivityEnergy(deltaTime, activity);
            ApplyEnvironmentalDamage(deltaTime);

            if (_age >= EffectiveMaxAge && _health > 0f)
            {
                Die(DeathCause.OldAge);
            }

            PublishStateChangeIfNeeded(previous, "Tick");
        }

        public void Eat(float nutritionAmount)
        {
            if (!EnsureAlive(nameof(Eat)) || nutritionAmount <= 0f)
            {
                return;
            }

            var previous = Snapshot;
            _hunger = Clamp(_hunger - nutritionAmount, 0f, _baseRates.HungerCapacity);
            PublishStateChangeIfNeeded(previous, "Eat");
        }

        public void Drink(float amount)
        {
            if (!EnsureAlive(nameof(Drink)) || amount <= 0f)
            {
                return;
            }

            var previous = Snapshot;
            _thirst = Clamp(_thirst - amount, 0f, _baseRates.ThirstCapacity);
            PublishStateChangeIfNeeded(previous, "Drink");
        }

        public void ConsumeEnergy(float amount)
        {
            if (!EnsureAlive(nameof(ConsumeEnergy)) || amount <= 0f)
            {
                return;
            }

            var previous = Snapshot;
            _energy = Clamp(_energy - amount, 0f, EffectiveMaxEnergy);
            PublishStateChangeIfNeeded(previous, "ConsumeEnergy");
        }

        public void TakeDamage(float amount, DeathCause cause = DeathCause.Environmental)
        {
            if (!EnsureAlive(nameof(TakeDamage)) || amount <= 0f)
            {
                return;
            }

            ApplyHealthDelta(-amount);

            if (_health <= 0f)
            {
                Die(cause);
            }
        }

        public void Heal(float amount)
        {
            if (!EnsureAlive(nameof(Heal)) || amount <= 0f)
            {
                return;
            }

            ApplyHealthDelta(amount);
        }

        /// <summary>
        /// Restores energy over time. Typically invoked by <see cref="Tick"/> when activity is resting,
        /// but exposed for explicit rest actions.
        /// </summary>
        public void Rest(float deltaTime)
        {
            if (!EnsureAlive(nameof(Rest)) || deltaTime <= 0f)
            {
                return;
            }

            var previous = Snapshot;
            var recovery = _baseRates.RestingRecovery * _modifiers.RestingRecoveryMultiplier * deltaTime;
            _energy = Clamp(_energy + recovery, 0f, EffectiveMaxEnergy);
            PublishStateChangeIfNeeded(previous, "Rest");
        }

        public void Die(DeathCause cause = DeathCause.Unknown)
        {
            if (!_isAlive)
            {
                return;
            }

            var previous = Snapshot;

            _isAlive = false;
            _cause = cause;
            _health = 0f;
            _energy = 0f;

            var finalState = Snapshot;
            Died?.Invoke(new CreatureDiedEventArgs(cause, finalState));
            StateChanged?.Invoke(new CreatureStateChangedEventArgs(previous, finalState, "Death"));
        }

        public void ApplyModifiers(MetabolicModifiers modifiers)
        {
            if (modifiers == null)
            {
                throw new ArgumentNullException(nameof(modifiers));
            }

            var previous = Snapshot;
            _modifiers = modifiers;

            _health = Clamp(_health, 0f, EffectiveMaxHealth);
            _energy = Clamp(_energy, 0f, EffectiveMaxEnergy);
            _age = Math.Min(_age, EffectiveMaxAge);

            PublishStateChangeIfNeeded(previous, "ApplyModifiers");
        }

        private void ApplyNeedsAccumulation(float deltaTime)
        {
            _hunger = Clamp(
                _hunger + _baseRates.HungerIncreaseRate * _modifiers.HungerRateMultiplier * deltaTime,
                0f,
                _baseRates.HungerCapacity);

            _thirst = Clamp(
                _thirst + _baseRates.ThirstIncreaseRate * _modifiers.ThirstRateMultiplier * deltaTime,
                0f,
                _baseRates.ThirstCapacity);
        }

        private void ApplyActivityEnergy(float deltaTime, ActivityLevel activity)
        {
            if (activity == ActivityLevel.Resting)
            {
                var recovery = _baseRates.RestingRecovery * _modifiers.RestingRecoveryMultiplier * deltaTime;
                _energy = Clamp(_energy + recovery, 0f, EffectiveMaxEnergy);
                return;
            }

            var consumption = _baseRates.PassiveEnergyConsumption * deltaTime;

            switch (activity)
            {
                case ActivityLevel.Walking:
                    consumption += _baseRates.WalkingEnergyConsumption * deltaTime;
                    break;
                case ActivityLevel.Sprinting:
                    consumption += _baseRates.SprintingEnergyConsumption * deltaTime;
                    break;
                case ActivityLevel.Attacking:
                    consumption += _baseRates.AttackEnergyConsumption * deltaTime;
                    break;
            }

            consumption *= _modifiers.EnergyConsumptionMultiplier;
            _energy = Clamp(_energy - consumption, 0f, EffectiveMaxEnergy);
        }

        private void ApplyEnvironmentalDamage(float deltaTime)
        {
            if (_hunger >= _baseRates.StarvationThreshold)
            {
                ApplyHealthDelta(-_baseRates.StarvationDamage * _modifiers.StarvationDamageMultiplier * deltaTime);
            }

            if (_thirst >= _baseRates.DehydrationThreshold)
            {
                ApplyHealthDelta(-_baseRates.DehydrationDamage * _modifiers.DehydrationDamageMultiplier * deltaTime);
            }

            if (_health <= 0f && _isAlive)
            {
                var cause = _hunger >= _baseRates.StarvationThreshold && _thirst >= _baseRates.DehydrationThreshold
                    ? DeathCause.Starvation
                    : _hunger >= _baseRates.StarvationThreshold
                        ? DeathCause.Starvation
                        : DeathCause.Dehydration;

                Die(cause);
            }
        }

        private void ApplyHealthDelta(float delta)
        {
            if (delta == 0f)
            {
                return;
            }

            var previousHealth = _health;
            _health = Clamp(_health + delta, 0f, EffectiveMaxHealth);

            if (Math.Abs(_health - previousHealth) > float.Epsilon)
            {
                HealthChanged?.Invoke(new HealthChangedEventArgs(previousHealth, _health, EffectiveMaxHealth));
            }
        }

        private CreatureState BuildSnapshot() =>
            new CreatureState(
                _health,
                EffectiveMaxHealth,
                _hunger,
                _thirst,
                _energy,
                EffectiveMaxEnergy,
                _age,
                EffectiveMaxAge,
                _isAlive,
                _cause);

        private bool EnsureAlive(string operation)
        {
            if (_isAlive)
            {
                return true;
            }

            return false;
        }

        private void PublishStateChangeIfNeeded(CreatureState previous, string changeKind)
        {
            var current = Snapshot;
            if (!StatesEqual(previous, current))
            {
                StateChanged?.Invoke(new CreatureStateChangedEventArgs(previous, current, changeKind));
            }
        }

        private static bool StatesEqual(CreatureState a, CreatureState b) =>
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

        private static float Clamp(float value, float min, float max) =>
            value < min ? min : value > max ? max : value;
    }
}
