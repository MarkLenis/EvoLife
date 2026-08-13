namespace EvoLife.Common
{
    public enum CreatureRole : byte
    {
        Herbivore = 0,
        Predator = 1
    }

    public enum AgentPolicyKind : byte
    {
        ScriptedBaseline = 0,
        LearnedPpo = 1
    }

    public enum DeathCause : byte
    {
        Unknown = 0,
        Predation,
        Starvation,
        Dehydration,
        OldAge,
        Environmental,
    }

    /// <summary>
    /// Experiment lifecycle mode. Persistent ecosystems allow extinction;
    /// training-support may enable controlled respawn outside biology.
    /// </summary>
    public enum EcosystemMode : byte
    {
        Persistent = 0,
        TrainingSupport = 1
    }

    /// <summary>
    /// Population extinction snapshot. Derived from alive counts, not a fitness score.
    /// </summary>
    public enum ExtinctionState : byte
    {
        None = 0,
        HerbivoresExtinct = 1,
        PredatorsExtinct = 2,
        EcosystemExtinct = 3
    }

    /// <summary>
    /// Lightweight logical biome. Zones are spatial labels, not authored terrain.
    /// </summary>
    public enum BiomeKind : byte
    {
        Grassland = 0,
        Forest = 1,
        Wetland = 2,
        Rocky = 3
    }

    /// <summary>
    /// Configurable ecological event kinds. Effects are applied through Environment
    /// resource APIs, CreatureVitals, and Simulation lifecycle — never hidden state.
    /// </summary>
    public enum EnvironmentalEventKind : byte
    {
        Drought = 0,
        Wildfire = 1,
        HeatWave = 2,
        FoodBoom = 3,
        DiseasePressure = 4,
        PredatorIntroduction = 5,
        PredatorRemoval = 6
    }

    public enum DayNightPhase : byte
    {
        Day = 0,
        Night = 1
    }
}
