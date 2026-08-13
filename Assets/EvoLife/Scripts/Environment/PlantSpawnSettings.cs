using System;
using UnityEngine;
using EvoLife.Common;

namespace EvoLife.Environment
{
    /// <summary>
    /// Seeded plant/water placement settings. Placement runs once, not every frame.
    /// </summary>
    [Serializable]
    public sealed class PlantSpawnSettings
    {
        [SerializeField] int seed = 42;
        [SerializeField] float worldRadius = 20f;
        [SerializeField] float defaultDensity = 0.04f;
        [SerializeField] float minSeparation = 1.5f;
        [SerializeField] float defaultCapacity = 20f;
        [SerializeField] float defaultRemaining = 20f;
        [SerializeField] float defaultRegenPerSecond = 0.5f;
        [SerializeField] float defaultRegenDelaySeconds = 2f;
        [SerializeField] int maxPlacementAttempts = 24;

        public int Seed
        {
            get => seed;
            set => seed = value;
        }

        public float WorldRadius
        {
            get => worldRadius;
            set => worldRadius = Mathf.Max(0f, value);
        }

        public float DefaultDensity
        {
            get => defaultDensity;
            set => defaultDensity = Mathf.Max(0f, value);
        }

        public float MinSeparation
        {
            get => minSeparation;
            set => minSeparation = Mathf.Max(0f, value);
        }

        public float DefaultCapacity
        {
            get => defaultCapacity;
            set => defaultCapacity = Mathf.Max(0f, value);
        }

        public float DefaultRemaining
        {
            get => defaultRemaining;
            set => defaultRemaining = Mathf.Max(0f, value);
        }

        public float DefaultRegenPerSecond
        {
            get => defaultRegenPerSecond;
            set => defaultRegenPerSecond = Mathf.Max(0f, value);
        }

        public float DefaultRegenDelaySeconds
        {
            get => defaultRegenDelaySeconds;
            set => defaultRegenDelaySeconds = Mathf.Max(0f, value);
        }

        public int MaxPlacementAttempts
        {
            get => maxPlacementAttempts;
            set => maxPlacementAttempts = Mathf.Max(1, value);
        }

        public float WorldArea => Mathf.PI * WorldRadius * WorldRadius;
    }
}
