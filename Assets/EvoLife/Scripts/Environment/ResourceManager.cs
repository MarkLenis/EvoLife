using System;
using System.Collections.Generic;
using UnityEngine;
using EvoLife.Common;

namespace EvoLife.Environment
{
    /// <summary>
    /// Optional environment owner for seeded resource placement, biome modifiers, and census.
    /// Places plants once; regeneration is in-place and is not a per-frame respawn.
    /// </summary>
    public sealed class ResourceManager : MonoBehaviour, ISimulationTickable, IReadOnlyResourceCensus, IEnvironmentEffectHost
    {
        [SerializeField] ResourceRegistry registry;
        [SerializeField] PlantSpawnSettings plantSpawn = new PlantSpawnSettings();
        [SerializeField] List<BiomeZone> zones = new List<BiomeZone>();
        [SerializeField] int waterSourceCount = 2;
        [SerializeField] bool placeOnStart = true;
        [SerializeField] GameObject plantPrefab;
        [SerializeField] GameObject waterPrefab;

        readonly BiomeMap biomeMap = new BiomeMap();
        readonly EnvironmentModifierStack modifiers = new EnvironmentModifierStack();
        readonly List<PlantResource> plants = new List<PlantResource>();
        readonly List<WaterSource> waters = new List<WaterSource>();
        readonly List<GameObject> spawned = new List<GameObject>();
        bool placed;
        bool modifiersDirty = true;
        ResourceCensus cachedCensus;
        int cachedCensusFrame = int.MinValue;
        bool censusCached;

        public ResourceRegistry Registry => registry;
        public BiomeMap Biomes => biomeMap;
        public PlantSpawnSettings SpawnSettings => plantSpawn ?? (plantSpawn = new PlantSpawnSettings());
        public IReadOnlyList<PlantResource> Plants => plants;
        public IReadOnlyList<WaterSource> Waters => waters;
        public bool HasPlaced => placed;
        public int PlaceResourcesCallCount { get; private set; }

        public bool PlaceOnStart
        {
            get => placeOnStart;
            set => placeOnStart = value;
        }

        /// <summary>
        /// Frame-cached census fields. Prefer <see cref="CaptureCensus"/> for a live snapshot.
        /// Cache lifetime: the current Unity frame (<c>Time.frameCount</c>), invalidated on
        /// tick, placement, tracking, and event resource mutations.
        /// </summary>
        public int PlantCount => CachedCensus.PlantCount;
        public int WaterSourceCount => CachedCensus.WaterSourceCount;
        public float TotalPlantFoodRemaining => CachedCensus.TotalPlantFoodRemaining;
        public float TotalPlantCapacity => CachedCensus.TotalPlantCapacity;
        public float PlantAbundance => CachedCensus.PlantAbundance;
        public float PlantDensity => CachedCensus.PlantDensity;
        public float WorldArea => SpawnSettings.WorldArea;
        public float TemperatureNormalized => ComputeTemperature();

        ResourceCensus CachedCensus
        {
            get
            {
                if (!censusCached || cachedCensusFrame != Time.frameCount)
                {
                    cachedCensus = CaptureCensusUncached();
                    censusCached = true;
                    cachedCensusFrame = Time.frameCount;
                }

                return cachedCensus;
            }
        }

        public void Configure(
            ResourceRegistry resourceRegistry,
            PlantSpawnSettings settings,
            IEnumerable<BiomeZone> biomeZones = null,
            int waterCount = 0)
        {
            registry = resourceRegistry;
            if (settings != null)
            {
                plantSpawn = settings;
            }

            waterSourceCount = Mathf.Max(0, waterCount);
            biomeMap.ReplaceZones(biomeZones);
            if (zones != null)
            {
                zones.Clear();
                if (biomeZones != null)
                {
                    foreach (var zone in biomeZones)
                    {
                        if (zone != null)
                        {
                            zones.Add(zone);
                        }
                    }
                }
            }

            biomeMap.ConfigureDefaults(
                BiomeKind.Grassland,
                BiomeMap.DefaultRegenFor(BiomeKind.Grassland),
                BiomeMap.DefaultTemperatureFor(BiomeKind.Grassland),
                SpawnSettings.DefaultDensity);
            modifiersDirty = true;
            InvalidateCensus();
        }

        public void SetPresentationPrefabs(GameObject plant, GameObject water)
        {
            plantPrefab = plant;
            waterPrefab = water;
        }

        void Awake()
        {
            if (registry == null)
            {
                registry = GetComponent<ResourceRegistry>() ?? FindObjectOfType<ResourceRegistry>();
            }

            SyncSerializedZones();
        }

        void Start()
        {
            if (placeOnStart)
            {
                EnsurePlaced();
            }
        }

        public void EnsurePlaced()
        {
            if (placed)
            {
                return;
            }

            PlaceResources();
        }

        public void PlaceResources()
        {
            PlaceResourcesCallCount++;
            if (registry == null)
            {
                registry = GetComponent<ResourceRegistry>() ?? gameObject.AddComponent<ResourceRegistry>();
            }

            SyncSerializedZones();
            ClearSpawned();

            var settings = SpawnSettings;
            var rng = new System.Random(settings.Seed);
            var origin = transform.position;
            PlacePlants(origin, settings, rng);
            PlaceWater(origin, settings, rng);
            placed = true;
            modifiersDirty = true;
            RefreshModifiers();
            InvalidateCensus();
        }

        public void Tick(float deltaTimeSeconds)
        {
            EnsurePlaced();
            RefreshModifiers();
            InvalidateCensus();

            if (deltaTimeSeconds <= 0f)
            {
                return;
            }

            for (var i = 0; i < plants.Count; i++)
            {
                plants[i]?.Tick(deltaTimeSeconds);
            }

            for (var i = 0; i < waters.Count; i++)
            {
                waters[i]?.Tick(deltaTimeSeconds);
            }
        }

        public void TrackPlant(PlantResource plant)
        {
            if (plant == null || plants.Contains(plant))
            {
                return;
            }

            plants.Add(plant);
            if (registry != null)
            {
                plant.BindRegistry(registry);
            }

            modifiersDirty = true;
            RefreshModifiers();
            InvalidateCensus();
        }

        public void TrackWater(WaterSource water)
        {
            if (water == null || waters.Contains(water))
            {
                return;
            }

            waters.Add(water);
            if (registry != null)
            {
                water.BindRegistry(registry);
            }

            modifiersDirty = true;
            RefreshModifiers();
            InvalidateCensus();
        }

        public ResourceCensus CaptureCensus()
        {
            cachedCensus = CaptureCensusUncached();
            censusCached = true;
            cachedCensusFrame = Time.frameCount;
            return cachedCensus;
        }

        ResourceCensus CaptureCensusUncached()
        {
            if (registry != null)
            {
                return registry.CaptureCensus(WorldArea);
            }

            var remaining = 0f;
            var capacity = 0f;
            for (var i = 0; i < plants.Count; i++)
            {
                if (plants[i] == null)
                {
                    continue;
                }

                remaining += plants[i].AvailableAmount;
                capacity += plants[i].Capacity;
            }

            return new ResourceCensus(plants.Count, waters.Count, remaining, capacity, WorldArea);
        }

        public void PushEventModifiers(
            int eventId,
            float regenMultiplier,
            float temperatureDelta,
            float waterRechargeMultiplier,
            BiomeKind[] biomes)
        {
            modifiers.Set(eventId, regenMultiplier, temperatureDelta, waterRechargeMultiplier, biomes);
            modifiersDirty = true;
            RefreshModifiers();
        }

        public void RemoveEventModifiers(int eventId)
        {
            modifiers.Remove(eventId);
            modifiersDirty = true;
            RefreshModifiers();
        }

        public void BoostPlantAvailability(float amount, BiomeKind[] biomes)
        {
            ForMatchingPlants(biomes, plant => plant.AddAvailable(amount));
            InvalidateCensus();
        }

        public void DepletePlants(float fraction, BiomeKind[] biomes)
        {
            ForMatchingPlants(biomes, plant => plant.DepleteByFraction(fraction));
            InvalidateCensus();
        }

        public EnvironmentStateSnapshot CaptureState(
            IReadOnlyDayNightState dayNight,
            IReadOnlyList<IReadOnlyEnvironmentalEvent> activeEvents)
        {
            return new EnvironmentStateSnapshot(dayNight, CaptureCensus(), activeEvents, TemperatureNormalized);
        }

        void PlacePlants(Vector3 origin, PlantSpawnSettings settings, System.Random rng)
        {
            if (biomeMap.Zones.Count == 0)
            {
                var count = Mathf.RoundToInt(settings.DefaultDensity * settings.WorldArea);
                PlacePlantBatch(origin, settings.WorldRadius, count, settings, rng, null);
                return;
            }

            for (var i = 0; i < biomeMap.Zones.Count; i++)
            {
                var zone = biomeMap.Zones[i];
                if (zone == null)
                {
                    continue;
                }

                var count = Mathf.RoundToInt(zone.PlantSpawnDensity * zone.Area);
                PlacePlantBatch(zone.Center, zone.Radius, count, settings, rng, zone);
            }
        }

        void PlacePlantBatch(
            Vector3 center,
            float radius,
            int count,
            PlantSpawnSettings settings,
            System.Random rng,
            BiomeZone zone)
        {
            var placedPositions = new List<Vector3>(plants.Count + Mathf.Max(0, count));
            for (var i = 0; i < plants.Count; i++)
            {
                if (plants[i] != null)
                {
                    placedPositions.Add(plants[i].Position);
                }
            }

            for (var n = 0; n < count; n++)
            {
                if (!TrySamplePosition(center, radius, settings, rng, placedPositions, out var position))
                {
                    continue;
                }

                var plant = CreatePlant(position, settings, zone);
                if (plant == null)
                {
                    continue;
                }

                plants.Add(plant);
                placedPositions.Add(position);
            }
        }

        PlantResource CreatePlant(Vector3 position, PlantSpawnSettings settings, BiomeZone zone)
        {
            var go = plantPrefab != null
                ? Instantiate(plantPrefab, position, Quaternion.identity, transform)
                : new GameObject("Plant");
            if (plantPrefab == null)
            {
                go.transform.SetParent(transform, false);
                go.transform.position = position;
            }

            spawned.Add(go);
            var plant = go.GetComponent<PlantResource>() ?? go.AddComponent<PlantResource>();
            var remaining = Mathf.Min(settings.DefaultRemaining, settings.DefaultCapacity);
            plant.Configure(
                settings.DefaultCapacity,
                remaining,
                settings.DefaultRegenPerSecond,
                settings.DefaultRegenDelaySeconds,
                registry);
            var biomeRegen = zone != null ? zone.RegenMultiplier : biomeMap.RegenMultiplierAt(position);
            plant.SetBiomeRegenMultiplier(biomeRegen);
            return plant;
        }

        void PlaceWater(Vector3 origin, PlantSpawnSettings settings, System.Random rng)
        {
            for (var i = 0; i < waterSourceCount; i++)
            {
                Vector3 position;
                var wetland = FindFirstBiome(BiomeKind.Wetland);
                if (wetland != null)
                {
                    TrySamplePosition(wetland.Center, wetland.Radius, settings, rng, null, out position);
                }
                else if (!TrySamplePosition(origin, settings.WorldRadius, settings, rng, null, out position))
                {
                    position = origin;
                }

                var go = waterPrefab != null
                    ? Instantiate(waterPrefab, position, Quaternion.identity, transform)
                    : new GameObject("Water");
                if (waterPrefab == null)
                {
                    go.transform.SetParent(transform, false);
                    go.transform.position = position;
                }

                spawned.Add(go);
                var water = go.GetComponent<WaterSource>() ?? go.AddComponent<WaterSource>();
                water.Configure(true, 100f, 100f, 10f, 0f, registry);
                waters.Add(water);
            }
        }

        BiomeZone FindFirstBiome(BiomeKind kind)
        {
            for (var i = 0; i < biomeMap.Zones.Count; i++)
            {
                if (biomeMap.Zones[i] != null && biomeMap.Zones[i].Kind == kind)
                {
                    return biomeMap.Zones[i];
                }
            }

            return null;
        }

        static bool TrySamplePosition(
            Vector3 center,
            float radius,
            PlantSpawnSettings settings,
            System.Random rng,
            List<Vector3> existing,
            out Vector3 position)
        {
            var minSepSqr = settings.MinSeparation * settings.MinSeparation;
            for (var attempt = 0; attempt < settings.MaxPlacementAttempts; attempt++)
            {
                var angle = rng.NextDouble() * Math.PI * 2.0;
                var distance = radius <= 0f ? 0f : Math.Sqrt(rng.NextDouble()) * radius;
                var candidate = center + new Vector3(
                    (float)(Math.Cos(angle) * distance),
                    0f,
                    (float)(Math.Sin(angle) * distance));

                if (existing != null && minSepSqr > 0f)
                {
                    var tooClose = false;
                    for (var i = 0; i < existing.Count; i++)
                    {
                        var delta = existing[i] - candidate;
                        delta.y = 0f;
                        if (delta.sqrMagnitude < minSepSqr)
                        {
                            tooClose = true;
                            break;
                        }
                    }

                    if (tooClose)
                    {
                        continue;
                    }
                }

                position = candidate;
                return true;
            }

            position = center;
            return false;
        }

        void RefreshModifiers()
        {
            if (!modifiersDirty)
            {
                return;
            }

            for (var i = 0; i < plants.Count; i++)
            {
                var plant = plants[i];
                if (plant == null)
                {
                    continue;
                }

                var kind = biomeMap.ResolveKind(plant.Position);
                plant.SetBiomeRegenMultiplier(biomeMap.RegenMultiplierAt(plant.Position));
                plant.SetEventRegenMultiplier(modifiers.RegenMultiplierForBiome(kind));
            }

            var waterMul = modifiers.WaterRechargeMultiplier;
            for (var i = 0; i < waters.Count; i++)
            {
                waters[i]?.SetRechargeMultiplier(waterMul);
            }

            modifiersDirty = false;
        }

        void ForMatchingPlants(BiomeKind[] biomes, Action<PlantResource> action)
        {
            if (action == null)
            {
                return;
            }

            for (var i = 0; i < plants.Count; i++)
            {
                var plant = plants[i];
                if (plant == null)
                {
                    continue;
                }

                if (!MatchesBiome(biomes, biomeMap.ResolveKind(plant.Position)))
                {
                    continue;
                }

                action(plant);
            }
        }

        static bool MatchesBiome(BiomeKind[] biomes, BiomeKind kind)
        {
            if (biomes == null || biomes.Length == 0)
            {
                return true;
            }

            for (var i = 0; i < biomes.Length; i++)
            {
                if (biomes[i] == kind)
                {
                    return true;
                }
            }

            return false;
        }

        float ComputeTemperature()
        {
            var value = biomeMap.MeanTemperatureOffset() + modifiers.TemperatureDelta;
            if (value < 0f)
            {
                return 0f;
            }

            return value > 1f ? 1f : value;
        }

        void SyncSerializedZones()
        {
            biomeMap.ReplaceZones(zones);
            biomeMap.ConfigureDefaults(
                BiomeKind.Grassland,
                BiomeMap.DefaultRegenFor(BiomeKind.Grassland),
                BiomeMap.DefaultTemperatureFor(BiomeKind.Grassland),
                SpawnSettings.DefaultDensity);
        }

        void ClearSpawned()
        {
            for (var i = 0; i < spawned.Count; i++)
            {
                if (spawned[i] == null)
                {
                    continue;
                }

                if (Application.isPlaying)
                {
                    Destroy(spawned[i]);
                }
                else
                {
                    DestroyImmediate(spawned[i]);
                }
            }

            spawned.Clear();
            plants.Clear();
            waters.Clear();
            placed = false;
            InvalidateCensus();
        }

        void InvalidateCensus()
        {
            censusCached = false;
        }

        void OnDestroy()
        {
            if (!Application.isPlaying)
            {
                ClearSpawned();
            }
        }
    }
}
