using System;
using UnityEngine;
using EvoLife.Common;

namespace EvoLife.Creatures
{
    /// <summary>
    /// Unity-facing owner of biological vitals. Delegates simulation to <see cref="CreatureBiology"/>.
    /// </summary>
    public sealed class CreatureVitals : MonoBehaviour, IReadOnlyVitalState, ISimulationTickable, ICreatureDeathObservable
    {
        [SerializeField] SpeciesVitalsDefinition definition;

        CreatureBiology biology;
        ActivityLevel currentActivity = ActivityLevel.Idle;
        event Action<CreatureDeathNotice> deathObserved;

        public event Action<CreatureDiedEventArgs> Died;
        public event Action<HealthChangedEventArgs> HealthChanged;
        public event Action<CreatureStateChangedEventArgs> StateChanged;

        event Action<CreatureDeathNotice> ICreatureDeathObservable.DeathObserved
        {
            add => deathObserved += value;
            remove => deathObserved -= value;
        }

        public ActivityLevel CurrentActivity
        {
            get => currentActivity;
            set => currentActivity = value;
        }

        public CreatureBiology Biology => biology;

        public float Health => biology?.Snapshot.Health ?? 0f;
        public float MaxHealth => biology?.Snapshot.MaxHealth ?? 0f;
        public float Hunger => biology?.Snapshot.Hunger ?? 0f;
        public float MaxHunger => biology?.Snapshot.MaxHunger ?? 0f;
        public float Thirst => biology?.Snapshot.Thirst ?? 0f;
        public float MaxThirst => biology?.Snapshot.MaxThirst ?? 0f;
        public float Energy => biology?.Snapshot.Energy ?? 0f;
        public float MaxEnergy => biology?.Snapshot.MaxEnergy ?? 0f;
        public float Age => biology?.Snapshot.Age ?? 0f;
        public float MaxAge => biology?.Snapshot.MaxAge ?? 0f;
        public bool IsAlive => biology?.IsAlive ?? false;
        public DeathCause? CauseOfDeath => biology?.CauseOfDeath;

        public void Initialize(SpeciesVitalsDefinition vitalsDefinition, float startingAge = 0f)
        {
            UnwireEvents(biology);
            definition = vitalsDefinition;
            if (definition == null)
            {
                biology = null;
                return;
            }

            biology = new CreatureBiology(definition.ToMetabolicRates(), startingAge: Mathf.Max(0f, startingAge));
            WireEvents(biology);
        }

        /// <summary>
        /// Rebuilds biology from the serialized species definition. Used for local episode
        /// reset during training; does not reset the rest of the ecosystem.
        /// </summary>
        public void Reinitialize()
        {
            Initialize(definition);
        }

        void OnDestroy()
        {
            UnwireEvents(biology);
        }

        /// <summary>
        /// Applies phenotype-derived metabolic multipliers. Creatures never read genomes.
        /// Builds a new modifiers object so other creatures cannot share this mutation.
        /// </summary>
        public void ApplyPhenotypeModifiers(IReadOnlyPhenotype phenotype)
        {
            if (biology == null || phenotype == null)
            {
                return;
            }

            var metabolism = Mathf.Max(0.01f, phenotype.MetabolismMultiplier);
            biology.ApplyModifiers(
                biology.Modifiers.With(
                    maxEnergyMultiplier: Mathf.Max(0.01f, phenotype.MaxEnergyMultiplier),
                    maxAgeMultiplier: Mathf.Max(0.01f, phenotype.MaxAgeMultiplier),
                    hungerRateMultiplier: metabolism,
                    thirstRateMultiplier: metabolism,
                    energyConsumptionMultiplier: metabolism));
        }

        /// <summary>
        /// Applied by Genetics after phenotype resolution. Creatures never read genomes directly.
        /// </summary>
        public void ApplyMetabolismMultiplier(float multiplier)
        {
            if (biology == null)
            {
                return;
            }

            var clamped = Mathf.Max(0.01f, multiplier);
            biology.ApplyModifiers(
                biology.Modifiers.With(
                    hungerRateMultiplier: clamped,
                    thirstRateMultiplier: clamped,
                    energyConsumptionMultiplier: clamped));
        }

        public void Tick(float deltaTimeSeconds)
        {
            biology?.Tick(deltaTimeSeconds, currentActivity);
        }

        public void ApplyDamage(float amount, DeathCause cause = DeathCause.Environmental)
        {
            biology?.TakeDamage(amount, cause);
        }

        public void RestoreHealth(float amount)
        {
            biology?.Heal(amount);
        }

        public void ConsumeFood(float hungerRelief, float energyGain)
        {
            if (biology == null)
            {
                return;
            }

            biology.Eat(Mathf.Max(0f, hungerRelief));
            if (energyGain > 0f)
            {
                biology.GainEnergy(energyGain);
            }
        }

        public void Drink(float thirstRelief)
        {
            biology?.Drink(Mathf.Max(0f, thirstRelief));
        }

        public void ConsumeEnergy(float amount)
        {
            biology?.ConsumeEnergy(Mathf.Max(0f, amount));
        }

        public void Rest(float deltaTimeSeconds)
        {
            biology?.Rest(Mathf.Max(0f, deltaTimeSeconds));
        }

        public void Die(DeathCause cause = DeathCause.Unknown)
        {
            biology?.Die(cause);
        }

        void WireEvents(CreatureBiology target)
        {
            if (target == null)
            {
                return;
            }

            target.Died += ForwardDied;
            target.HealthChanged += ForwardHealthChanged;
            target.StateChanged += ForwardStateChanged;
        }

        void UnwireEvents(CreatureBiology target)
        {
            if (target == null)
            {
                return;
            }

            target.Died -= ForwardDied;
            target.HealthChanged -= ForwardHealthChanged;
            target.StateChanged -= ForwardStateChanged;
        }

        void ForwardDied(CreatureDiedEventArgs args)
        {
            Died?.Invoke(args);
            var identity = GetComponent<CreatureIdentity>();
            var id = identity != null ? identity.Id : default;
            deathObserved?.Invoke(new CreatureDeathNotice(
                id,
                args.Cause,
                args.FinalState.Age,
                args.FinalState.MaxAge));
        }
        void ForwardHealthChanged(HealthChangedEventArgs args) => HealthChanged?.Invoke(args);
        void ForwardStateChanged(CreatureStateChangedEventArgs args) => StateChanged?.Invoke(args);
    }
}
