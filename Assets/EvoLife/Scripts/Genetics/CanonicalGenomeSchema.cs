using System;

namespace EvoLife.Genetics
{
    /// <summary>
    /// Versioned Unity runtime genome schema (v1).
    /// Trait names, bounds, founder ranges, and mutation magnitudes match the Python
    /// reference package under evolife/genetics/. Unity C# is the canonical simulator.
    /// </summary>
    public static class CanonicalGenomeSchema
    {
        public const int Version = 1;
        public const int TraitCount = 9;

        static readonly TraitDefinition[] traits =
        {
            new TraitDefinition(
                TraitId.BaseMovementSpeed,
                "base_movement_speed",
                hardMin: 0.5f,
                hardMax: 5f,
                generationMin: 1f,
                generationMax: 3f,
                defaultValue: 2f,
                mutationMagnitude: 0.2f,
                description: "Baseline locomotion speed (units/sec)"),
            new TraitDefinition(
                TraitId.SprintSpeed,
                "sprint_speed",
                hardMin: 1f,
                hardMax: 10f,
                generationMin: 2f,
                generationMax: 6f,
                defaultValue: 4f,
                mutationMagnitude: 0.3f,
                description: "Maximum burst locomotion speed (units/sec)"),
            new TraitDefinition(
                TraitId.VisionRange,
                "vision_range",
                hardMin: 1f,
                hardMax: 50f,
                generationMin: 5f,
                generationMax: 25f,
                defaultValue: 12f,
                mutationMagnitude: 1.5f,
                description: "Sensory detection radius (units)"),
            new TraitDefinition(
                TraitId.MaximumEnergy,
                "maximum_energy",
                hardMin: 10f,
                hardMax: 500f,
                generationMin: 50f,
                generationMax: 200f,
                defaultValue: 100f,
                mutationMagnitude: 10f,
                description: "Energy capacity ceiling"),
            new TraitDefinition(
                TraitId.MetabolismRate,
                "metabolism_rate",
                hardMin: 0.01f,
                hardMax: 5f,
                generationMin: 0.1f,
                generationMax: 1.5f,
                defaultValue: 0.5f,
                mutationMagnitude: 0.05f,
                description: "Energy consumed per tick (lower = more efficient)"),
            new TraitDefinition(
                TraitId.BodySize,
                "body_size",
                hardMin: 0.1f,
                hardMax: 10f,
                generationMin: 0.5f,
                generationMax: 3f,
                defaultValue: 1f,
                mutationMagnitude: 0.1f,
                description: "Physical scale affecting collision and energy cost"),
            new TraitDefinition(
                TraitId.Aggression,
                "aggression",
                hardMin: 0f,
                hardMax: 1f,
                generationMin: 0f,
                generationMax: 1f,
                defaultValue: 0.3f,
                mutationMagnitude: 0.05f,
                description: "Tendency toward aggressive behavior (0=passive, 1=aggressive)"),
            new TraitDefinition(
                TraitId.ReproductionThreshold,
                "reproduction_threshold",
                hardMin: 0.1f,
                hardMax: 1f,
                generationMin: 0.3f,
                generationMax: 0.9f,
                defaultValue: 0.6f,
                mutationMagnitude: 0.03f,
                description: "Fraction of max energy required to reproduce"),
            new TraitDefinition(
                TraitId.MaximumAge,
                "maximum_age",
                hardMin: 10f,
                hardMax: 10000f,
                generationMin: 100f,
                generationMax: 2000f,
                defaultValue: 500f,
                mutationMagnitude: 50f,
                description: "Maximum lifespan in simulation ticks")
        };

        public static int Count => traits.Length;

        static CanonicalGenomeSchema()
        {
            if (traits.Length != TraitCount)
            {
                throw new InvalidOperationException(
                    "CanonicalGenomeSchema trait table does not match TraitCount.");
            }
        }

        public static TraitDefinition Get(TraitId id) => traits[(int)id];

        public static TraitDefinition Get(int index)
        {
            if (index < 0 || index >= traits.Length)
            {
                throw new ArgumentOutOfRangeException(nameof(index));
            }

            return traits[index];
        }

        public static TraitDefinition Get(string canonicalName)
        {
            for (var i = 0; i < traits.Length; i++)
            {
                if (traits[i].CanonicalName == canonicalName)
                {
                    return traits[i];
                }
            }

            throw new ArgumentException($"Unknown trait: {canonicalName}", nameof(canonicalName));
        }

        public static TraitDefinition[] All()
        {
            var copy = new TraitDefinition[traits.Length];
            Array.Copy(traits, copy, traits.Length);
            return copy;
        }

        public static string[] CanonicalNames()
        {
            var names = new string[traits.Length];
            for (var i = 0; i < traits.Length; i++)
            {
                names[i] = traits[i].CanonicalName;
            }

            return names;
        }

        public static float[] DefaultValues()
        {
            var values = new float[traits.Length];
            for (var i = 0; i < traits.Length; i++)
            {
                values[i] = traits[i].Default;
            }

            return values;
        }
    }
}
