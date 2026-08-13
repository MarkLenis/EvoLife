using UnityEngine;
using EvoLife.Common;
using EvoLife.Genetics;

namespace EvoLife.Simulation
{
    public enum ReproductionFailureReason : byte
    {
        None = 0,
        RequesterMissing,
        RequesterIneligible,
        NoCompatibleMate,
        PopulationCapped,
        SpawnFailed,
        DuplicateRequest
    }

    public readonly struct ReproductionResult
    {
        ReproductionResult(
            bool succeeded,
            ReproductionFailureReason failure,
            CreatureId? offspringId,
            GameObject offspring,
            Genome offspringGenome)
        {
            Succeeded = succeeded;
            Failure = failure;
            OffspringId = offspringId;
            Offspring = offspring;
            OffspringGenome = offspringGenome;
        }

        public bool Succeeded { get; }
        public ReproductionFailureReason Failure { get; }
        public CreatureId? OffspringId { get; }
        public GameObject Offspring { get; }
        public Genome OffspringGenome { get; }

        public static ReproductionResult Success(GameObject offspring, CreatureId id, Genome genome) =>
            new ReproductionResult(true, ReproductionFailureReason.None, id, offspring, genome);

        public static ReproductionResult Fail(ReproductionFailureReason reason) =>
            new ReproductionResult(false, reason, null, null, null);
    }

    public readonly struct OffspringBlueprint
    {
        public OffspringBlueprint(
            Genome genome,
            string speciesId,
            CreatureRole role,
            int generation,
            CreatureId parentA,
            CreatureId parentB,
            Vector3 position,
            AgentPolicyKind policyKind)
        {
            Genome = genome;
            SpeciesId = speciesId;
            Role = role;
            Generation = generation;
            ParentA = parentA;
            ParentB = parentB;
            Position = position;
            PolicyKind = policyKind;
        }

        public Genome Genome { get; }
        public string SpeciesId { get; }
        public CreatureRole Role { get; }
        public int Generation { get; }
        public CreatureId ParentA { get; }
        public CreatureId ParentB { get; }
        public Vector3 Position { get; }
        public AgentPolicyKind PolicyKind { get; }
    }

    /// <summary>
    /// Builds a child genome and lineage from two parents using canonical Genetics operators.
    /// Does not spawn creatures or score fitness.
    /// </summary>
    public static class OffspringComposer
    {
        public static OffspringBlueprint Compose(
            Genome parentAGenome,
            Genome parentBGenome,
            CreatureId parentAId,
            CreatureId parentBId,
            int parentAGeneration,
            int parentBGeneration,
            string speciesId,
            CreatureRole role,
            Vector3 position,
            AgentPolicyKind policyKind,
            IGeneticOperators operators,
            System.Random random)
        {
            var ops = operators ?? new DefaultGeneticOperators();
            var genome = ops is DefaultGeneticOperators defaultOps
                ? defaultOps.CreateOffspring(parentAGenome, parentBGenome, random)
                : MutateAfterCrossover(ops, parentAGenome, parentBGenome, random);
            var generation = System.Math.Max(parentAGeneration, parentBGeneration) + 1;
            if (generation < 1)
            {
                generation = 1;
            }

            return new OffspringBlueprint(
                genome,
                speciesId,
                role,
                generation,
                parentAId,
                parentBId,
                position,
                policyKind);
        }

        static Genome MutateAfterCrossover(
            IGeneticOperators operators,
            Genome parentA,
            Genome parentB,
            System.Random random)
        {
            var crossed = operators.Crossover(parentA, parentB, random);
            return operators.Mutate(crossed, random);
        }
    }
}
