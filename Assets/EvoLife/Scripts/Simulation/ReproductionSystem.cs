using System;
using System.Collections.Generic;
using UnityEngine;
using EvoLife.Common;
using EvoLife.Creatures;
using EvoLife.Genetics;

namespace EvoLife.Simulation
{
    /// <summary>
    /// Simulation-owned reproduction executor. Policies only request; this class
    /// checks local eligibility, spawns offspring through <see cref="CreatureSpawner"/>,
    /// and applies biological costs through CreatureVitals APIs only after a successful spawn.
    /// </summary>
    public sealed class ReproductionSystem : MonoBehaviour
    {
        [SerializeField] CreatureSpawner spawner;
        [SerializeField] PopulationTracker populationTracker;
        [SerializeField] CreatureLifecycleHub lifecycleHub;
        [SerializeField] SimulationClock clock;
        [SerializeField] SimulationConfig simulationConfig;
        [SerializeField] ReproductionConfig reproductionConfig;
        [SerializeField] GameObject herbivorePrefab;
        [SerializeField] GameObject predatorPrefab;

        readonly Dictionary<int, ReproductionParticipant> participants = new Dictionary<int, ReproductionParticipant>();
        readonly Dictionary<int, float> lastReproductionTime = new Dictionary<int, float>();
        ReproductionSettings settings = new ReproductionSettings();
        IGeneticOperators geneticOperators;
        System.Random random = new System.Random(1);
        bool requestInFlight;
        bool forceNextSpawnFailure;
        EcosystemSettings ecosystem = new EcosystemSettings();

        public ReproductionResult LastResult { get; private set; }
        public int LastRequestOffspringCount { get; private set; }

        public ReproductionSettings Settings => settings;

        public void Configure(
            CreatureSpawner creatureSpawner,
            PopulationTracker tracker,
            CreatureLifecycleHub hub,
            SimulationClock simulationClock,
            ReproductionSettings reproductionSettings,
            EcosystemSettings ecosystemSettings = null,
            SimulationConfig config = null,
            GameObject herbivore = null,
            GameObject predator = null)
        {
            spawner = creatureSpawner;
            populationTracker = tracker;
            BindHub(hub);
            clock = simulationClock;
            simulationConfig = config;
            settings = reproductionSettings ?? new ReproductionSettings();
            ecosystem = ecosystemSettings ?? config?.Ecosystem ?? new EcosystemSettings();
            herbivorePrefab = herbivore != null ? herbivore : herbivorePrefab;
            predatorPrefab = predator != null ? predator : predatorPrefab;
            geneticOperators = new DefaultGeneticOperators(settings.ToGeneticsConfig());
        }

        public void SetSeed(int seed)
        {
            random = new System.Random(seed);
            geneticOperators = new DefaultGeneticOperators(settings.ToGeneticsConfig());
        }

        public void SetGeneticOperators(IGeneticOperators operators)
        {
            geneticOperators = operators ?? new DefaultGeneticOperators(settings.ToGeneticsConfig());
        }

        public void SetSettings(ReproductionSettings reproductionSettings)
        {
            settings = reproductionSettings ?? new ReproductionSettings();
            geneticOperators = new DefaultGeneticOperators(settings.ToGeneticsConfig());
        }

        public void SetPrefabs(GameObject herbivore, GameObject predator)
        {
            herbivorePrefab = herbivore;
            predatorPrefab = predator;
        }

        /// <summary>
        /// Test seam: the next spawn attempt returns null after prepare succeeds.
        /// Costs and cooldown are not committed.
        /// </summary>
        public void ForceNextSpawnFailure() => forceNextSpawnFailure = true;

        public bool HasReproductionTimestamp(CreatureId id) =>
            lastReproductionTime.ContainsKey(id.Value);

        void OnEnable()
        {
            if (reproductionConfig != null)
            {
                settings = reproductionConfig.Settings;
            }

            if (simulationConfig != null)
            {
                ecosystem = simulationConfig.Ecosystem;
            }

            geneticOperators = new DefaultGeneticOperators(settings.ToGeneticsConfig());
            BindHub(lifecycleHub);
        }

        void OnDisable()
        {
            UnbindHub();
        }

        public void Register(GameObject instance)
        {
            if (instance == null)
            {
                return;
            }

            var identity = instance.GetComponent<CreatureIdentity>();
            if (identity == null)
            {
                return;
            }

            var vitals = instance.GetComponent<CreatureVitals>();
            var genome = instance.GetComponent<CreatureGenome>();
            var policy = instance.GetComponent<IPolicyKindOwner>();
            var id = identity.Id;
            var bridge = instance.GetComponent<CreatureReproductionBridge>();
            if (bridge == null)
            {
                bridge = instance.AddComponent<CreatureReproductionBridge>();
            }

            bridge.Bind(this, id);
            participants[id.Value] = new ReproductionParticipant(
                id,
                instance,
                identity,
                vitals,
                genome,
                policy);
        }

        public void Unregister(CreatureId id)
        {
            participants.Remove(id.Value);
            lastReproductionTime.Remove(id.Value);
        }

        public ReproductionResult TryReproduce(CreatureId requesterId)
        {
            if (requestInFlight)
            {
                LastResult = ReproductionResult.Fail(ReproductionFailureReason.DuplicateRequest);
                LastRequestOffspringCount = 0;
                return LastResult;
            }

            requestInFlight = true;
            LastRequestOffspringCount = 0;
            try
            {
                LastResult = ReproduceOnce(requesterId);
                return LastResult;
            }
            finally
            {
                requestInFlight = false;
            }
        }

        ReproductionResult ReproduceOnce(CreatureId requesterId)
        {
            if (!TryPrepareBirth(requesterId, out var prepared))
            {
                return prepared.Failure;
            }

            var offspring = SpawnPrepared(prepared);
            if (offspring == null)
            {
                return ReproductionResult.Fail(ReproductionFailureReason.SpawnFailed);
            }

            CommitBirth(prepared);
            LastRequestOffspringCount = 1;
            var identity = offspring.GetComponent<CreatureIdentity>();
            var offspringId = identity != null ? identity.Id : default;
            return ReproductionResult.Success(offspring, offspringId, prepared.Blueprint.Genome);
        }

        bool TryPrepareBirth(CreatureId requesterId, out PreparedBirth prepared)
        {
            prepared = default;
            if (!participants.TryGetValue(requesterId.Value, out var requester))
            {
                prepared = PreparedBirth.Failed(ReproductionResult.Fail(ReproductionFailureReason.RequesterMissing));
                return false;
            }

            var now = Now();
            if (!IsParticipantEligible(requester, now))
            {
                prepared = PreparedBirth.Failed(ReproductionResult.Fail(ReproductionFailureReason.RequesterIneligible));
                return false;
            }

            var mate = FindNearestEligibleMate(requester, now);
            if (mate == null)
            {
                prepared = PreparedBirth.Failed(ReproductionResult.Fail(ReproductionFailureReason.NoCompatibleMate));
                return false;
            }

            if (IsAtCap(requester.Identity.Role))
            {
                prepared = PreparedBirth.Failed(ReproductionResult.Fail(ReproductionFailureReason.PopulationCapped));
                return false;
            }

            var operators = geneticOperators ?? new DefaultGeneticOperators(settings.ToGeneticsConfig());
            geneticOperators = operators;

            var prefab = PrefabFor(requester.Identity.Role);
            if (prefab == null || spawner == null)
            {
                prepared = PreparedBirth.Failed(ReproductionResult.Fail(ReproductionFailureReason.SpawnFailed));
                return false;
            }

            var position = Midpoint(requester, mate);
            var policy = ResolvePolicy(requester);
            var blueprint = OffspringComposer.Compose(
                requester.GenomeAsset,
                mate.GenomeAsset,
                requester.Id,
                mate.Id,
                requester.Identity.Generation,
                mate.Identity.Generation,
                requester.Identity.SpeciesId,
                requester.Identity.Role,
                position,
                policy,
                operators,
                random);

            prepared = PreparedBirth.Ready(requester, mate, blueprint, prefab, now);
            return true;
        }

        GameObject SpawnPrepared(PreparedBirth prepared)
        {
            if (forceNextSpawnFailure)
            {
                forceNextSpawnFailure = false;
                return null;
            }

            if (spawner == null || prepared.Prefab == null)
            {
                return null;
            }

            var blueprint = prepared.Blueprint;
            return spawner.Spawn(
                prepared.Prefab,
                blueprint.Position,
                blueprint.SpeciesId,
                blueprint.Role,
                blueprint.Genome,
                blueprint.PolicyKind,
                blueprint.Generation,
                blueprint.ParentA,
                blueprint.ParentB);
        }

        void CommitBirth(PreparedBirth prepared)
        {
            lastReproductionTime[prepared.Requester.Id.Value] = prepared.Now;
            lastReproductionTime[prepared.Mate.Id.Value] = prepared.Now;
            ApplyReproductionCost(prepared.Requester);
            ApplyReproductionCost(prepared.Mate);
        }

        bool IsParticipantEligible(ReproductionParticipant participant, float now)
        {
            if (participant == null || participant.Vitals == null || !participant.Vitals.IsAlive)
            {
                return false;
            }

            var threshold = ReproductionEligibility.ResolveReproductionThreshold(
                participant.GenomeAsset,
                participant.Genome);
            var hasReproduced = lastReproductionTime.TryGetValue(participant.Id.Value, out var lastTime);
            var input = ReproductionEligibilityInput.FromVitals(
                participant.Vitals,
                threshold,
                hasReproduced,
                lastTime);
            return ReproductionEligibility.IsEligible(input, settings, now);
        }

        ReproductionParticipant FindNearestEligibleMate(ReproductionParticipant requester, float now)
        {
            ReproductionParticipant best = null;
            var bestSqr = settings.MateRange * settings.MateRange;
            var origin = requester.Position;

            foreach (var candidate in participants.Values)
            {
                if (candidate == null || candidate.Id == requester.Id)
                {
                    continue;
                }

                if (!ReproductionEligibility.AreSpeciesCompatible(
                        requester.Identity.SpeciesId,
                        candidate.Identity.SpeciesId,
                        requester.Identity.Role,
                        candidate.Identity.Role))
                {
                    continue;
                }

                var offset = candidate.Position - origin;
                offset.y = 0f;
                var sqr = offset.sqrMagnitude;
                if (sqr > bestSqr)
                {
                    continue;
                }

                if (!IsParticipantEligible(candidate, now))
                {
                    continue;
                }

                bestSqr = sqr;
                best = candidate;
            }

            return best;
        }

        bool IsAtCap(CreatureRole role)
        {
            if (populationTracker == null || ecosystem == null)
            {
                return false;
            }

            var cap = ecosystem.CapFor(role);
            if (cap <= 0)
            {
                return false;
            }

            return populationTracker.CountFor(role) >= cap;
        }

        void ApplyReproductionCost(ReproductionParticipant participant)
        {
            if (participant?.Vitals == null)
            {
                return;
            }

            if (settings.EnergyCost > 0f)
            {
                participant.Vitals.ConsumeEnergy(settings.EnergyCost);
            }

            if (settings.HealthCost > 0f)
            {
                participant.Vitals.ApplyDamage(settings.HealthCost, DeathCause.Environmental);
            }
        }

        AgentPolicyKind ResolvePolicy(ReproductionParticipant requester)
        {
            if (requester?.Policy != null)
            {
                return requester.Policy.PolicyKind;
            }

            if (simulationConfig == null)
            {
                return AgentPolicyKind.ScriptedBaseline;
            }

            return requester.Identity.Role == CreatureRole.Predator
                ? simulationConfig.PredatorPolicy
                : simulationConfig.HerbivorePolicy;
        }

        GameObject PrefabFor(CreatureRole role)
        {
            switch (role)
            {
                case CreatureRole.Predator:
                    return predatorPrefab;
                case CreatureRole.Herbivore:
                    return herbivorePrefab;
                default:
                    throw new ArgumentOutOfRangeException(nameof(role), role, "Unhandled CreatureRole.");
            }
        }

        Vector3 Midpoint(ReproductionParticipant a, ReproductionParticipant b)
        {
            var mid = (a.Position + b.Position) * 0.5f;
            var jitter = 0.25f;
            mid.x += ((float)random.NextDouble() * 2f - 1f) * jitter;
            mid.z += ((float)random.NextDouble() * 2f - 1f) * jitter;
            return mid;
        }

        float Now() => clock != null ? clock.SimulationTimeSeconds : 0f;

        void BindHub(CreatureLifecycleHub hub)
        {
            if (lifecycleHub != null)
            {
                lifecycleHub.Died -= OnDied;
            }

            lifecycleHub = hub;
            if (lifecycleHub != null)
            {
                lifecycleHub.Died += OnDied;
            }
        }

        void UnbindHub()
        {
            if (lifecycleHub != null)
            {
                lifecycleHub.Died -= OnDied;
            }
        }

        void OnDied(CreatureDeathNotice notice, IAnalyticsCreatureView view)
        {
            Unregister(notice.Id);
        }

        readonly struct PreparedBirth
        {
            PreparedBirth(
                ReproductionResult failure,
                ReproductionParticipant requester,
                ReproductionParticipant mate,
                OffspringBlueprint blueprint,
                GameObject prefab,
                float now)
            {
                Failure = failure;
                Requester = requester;
                Mate = mate;
                Blueprint = blueprint;
                Prefab = prefab;
                Now = now;
            }

            public ReproductionResult Failure { get; }
            public ReproductionParticipant Requester { get; }
            public ReproductionParticipant Mate { get; }
            public OffspringBlueprint Blueprint { get; }
            public GameObject Prefab { get; }
            public float Now { get; }

            public static PreparedBirth Failed(ReproductionResult failure) =>
                new PreparedBirth(failure, null, null, default, null, 0f);

            public static PreparedBirth Ready(
                ReproductionParticipant requester,
                ReproductionParticipant mate,
                OffspringBlueprint blueprint,
                GameObject prefab,
                float now) =>
                new PreparedBirth(default, requester, mate, blueprint, prefab, now);
        }

        sealed class ReproductionParticipant
        {
            public ReproductionParticipant(
                CreatureId id,
                GameObject instance,
                CreatureIdentity identity,
                CreatureVitals vitals,
                CreatureGenome genome,
                IPolicyKindOwner policy)
            {
                Id = id;
                Instance = instance;
                Identity = identity;
                Vitals = vitals;
                Genome = genome;
                Policy = policy;
            }

            public CreatureId Id { get; }
            public GameObject Instance { get; }
            public CreatureIdentity Identity { get; }
            public CreatureVitals Vitals { get; }
            public CreatureGenome Genome { get; }
            public IPolicyKindOwner Policy { get; }

            public Genome GenomeAsset => Genome != null ? Genome.Genome : null;

            public Vector3 Position => Instance != null ? Instance.transform.position : Vector3.zero;
        }
    }
}
