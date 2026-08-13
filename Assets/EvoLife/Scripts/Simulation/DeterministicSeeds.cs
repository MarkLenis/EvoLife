using System;

namespace EvoLife.Simulation
{
    /// <summary>
    /// Derived RNG streams from one experiment master seed.
    /// Independent offsets keep founder genomes, mutation, resources, events, and
    /// scripted wandering from consuming one shared sequence.
    /// </summary>
    public static class DeterministicSeeds
    {
        public const int FounderGenomesOffset = 0;
        public const int ReproductionOffset = 17;
        public const int ResourceSpawnOffset = 29;
        public const int EventScheduleOffset = 41;
        public const int ScriptedWanderOffset = 59;
        public const int TrainingRespawnOffset = 31;
        public const int EnvironmentalCreaturesOffset = 53;

        public static int Combine(int masterSeed, int offset)
        {
            unchecked
            {
                return (masterSeed * 397) ^ offset;
            }
        }

        public static int FounderGenomes(int masterSeed) => Combine(masterSeed, FounderGenomesOffset);

        public static int Reproduction(int masterSeed) => Combine(masterSeed, ReproductionOffset);

        public static int ResourceSpawn(int masterSeed) => Combine(masterSeed, ResourceSpawnOffset);

        public static int EventSchedule(int masterSeed) => Combine(masterSeed, EventScheduleOffset);

        public static int TrainingRespawn(int masterSeed) => Combine(masterSeed, TrainingRespawnOffset);

        public static int EnvironmentalCreatures(int masterSeed) =>
            Combine(masterSeed, EnvironmentalCreaturesOffset);

        public static int ScriptedWander(int masterSeed, int creatureId) =>
            Combine(Combine(masterSeed, ScriptedWanderOffset), creatureId);
    }
}
