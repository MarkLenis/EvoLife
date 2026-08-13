using System.Collections.Generic;
using UnityEngine;
using EvoLife.Common;

namespace EvoLife.Environment
{
    /// <summary>
    /// Optional bundled environment settings. Events stay on <see cref="EnvironmentalEventConfig"/>.
    /// </summary>
    [CreateAssetMenu(fileName = "EnvironmentConfig", menuName = "EvoLife/Environment/Config")]
    public sealed class EnvironmentConfig : ScriptableObject
    {
        [SerializeField] PlantSpawnSettings plants = new PlantSpawnSettings();
        [SerializeField] float dayDurationSeconds = 120f;
        [SerializeField] float nightStartNormalized = 0.5f;
        [SerializeField] int waterSourceCount = 2;
        [SerializeField] List<BiomeZone> zones = new List<BiomeZone>();

        public PlantSpawnSettings Plants => plants ?? (plants = new PlantSpawnSettings());

        public float DayDurationSeconds
        {
            get => dayDurationSeconds;
            set => dayDurationSeconds = Mathf.Max(0.0001f, value);
        }

        public float NightStartNormalized
        {
            get => nightStartNormalized;
            set => nightStartNormalized = Mathf.Clamp01(value);
        }

        public int WaterSourceCount
        {
            get => waterSourceCount;
            set => waterSourceCount = Mathf.Max(0, value);
        }

        public IReadOnlyList<BiomeZone> Zones => zones ?? (zones = new List<BiomeZone>());
    }
}
