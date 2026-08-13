using UnityEngine;
using EvoLife.Creatures;
using EvoLife.Environment;
using EvoLife.Genetics;

namespace EvoLife.AI
{
    /// <summary>
    /// Builds the canonical creature observation source from existing module components.
    /// </summary>
    public static class CreatureObservationFactory
    {
        public static CompositeObservationSource Create(
            CreatureVitals vitals,
            CreatureIdentity identity,
            CreatureGenome genome,
            CreatureCapabilityMotor motor,
            Transform transform,
            ResourceRegistry resources,
            float baseSenseRange = CreatureObservationSchema.DefaultSenseRange)
        {
            float SenseRange() => ResolveSenseRange(motor, baseSenseRange);

            IResourceProximitySensor resourceSensor = transform != null
                ? new ResourceRegistryProximitySensor(resources, transform, SenseRange)
                : null;

            ICreatureProximitySensor creatureSensor = transform != null
                ? new PhysicsCreatureProximitySensor(transform, SenseRange, identity)
                : null;

            return new CompositeObservationSource(
                vitals,
                identity,
                genome != null ? genome.Genome : null,
                resourceSensor,
                creatureSensor);
        }

        /// <summary>
        /// Sense radius shared by PPO observations and the scripted baseline interactor.
        /// Phenotype sensory-range multipliers apply here.
        /// </summary>
        public static float ResolveSenseRange(
            CreatureCapabilityMotor motor,
            float baseSenseRange = CreatureObservationSchema.DefaultSenseRange)
        {
            var multiplier = motor != null ? motor.SensoryRangeMultiplier : 1f;
            return Mathf.Max(0.01f, baseSenseRange * multiplier);
        }
    }
}
