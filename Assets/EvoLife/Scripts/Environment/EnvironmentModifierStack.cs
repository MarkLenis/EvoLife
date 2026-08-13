using System.Collections.Generic;
using EvoLife.Common;

namespace EvoLife.Environment
{
    /// <summary>
    /// Stacked ecological modifiers keyed by event id. Product of regen multipliers; sum of temperature.
    /// </summary>
    public sealed class EnvironmentModifierStack
    {
        readonly Dictionary<int, Modifier> modifiers = new Dictionary<int, Modifier>();

        public int Count => modifiers.Count;

        public float RegenMultiplier
        {
            get
            {
                var product = 1f;
                foreach (var entry in modifiers.Values)
                {
                    product *= entry.RegenMultiplier;
                }

                return product;
            }
        }

        public float TemperatureDelta
        {
            get
            {
                var sum = 0f;
                foreach (var entry in modifiers.Values)
                {
                    sum += entry.TemperatureDelta;
                }

                return sum;
            }
        }

        public float WaterRechargeMultiplier
        {
            get
            {
                var product = 1f;
                foreach (var entry in modifiers.Values)
                {
                    product *= entry.WaterRechargeMultiplier;
                }

                return product;
            }
        }

        public float RegenMultiplierForBiome(BiomeKind kind)
        {
            var product = 1f;
            foreach (var entry in modifiers.Values)
            {
                if (AppliesTo(entry, kind))
                {
                    product *= entry.RegenMultiplier;
                }
            }

            return product;
        }

        public void Set(int eventId, float regenMultiplier, float temperatureDelta, float waterRechargeMultiplier, BiomeKind[] biomes)
        {
            modifiers[eventId] = new Modifier(
                regenMultiplier < 0f ? 0f : regenMultiplier,
                temperatureDelta,
                waterRechargeMultiplier < 0f ? 0f : waterRechargeMultiplier,
                biomes);
        }

        public void Remove(int eventId) => modifiers.Remove(eventId);

        public void Clear() => modifiers.Clear();

        static bool AppliesTo(Modifier modifier, BiomeKind kind)
        {
            if (modifier.Biomes == null || modifier.Biomes.Length == 0)
            {
                return true;
            }

            for (var i = 0; i < modifier.Biomes.Length; i++)
            {
                if (modifier.Biomes[i] == kind)
                {
                    return true;
                }
            }

            return false;
        }

        readonly struct Modifier
        {
            public Modifier(float regenMultiplier, float temperatureDelta, float waterRechargeMultiplier, BiomeKind[] biomes)
            {
                RegenMultiplier = regenMultiplier;
                TemperatureDelta = temperatureDelta;
                WaterRechargeMultiplier = waterRechargeMultiplier;
                Biomes = biomes;
            }

            public float RegenMultiplier { get; }
            public float TemperatureDelta { get; }
            public float WaterRechargeMultiplier { get; }
            public BiomeKind[] Biomes { get; }
        }
    }
}
