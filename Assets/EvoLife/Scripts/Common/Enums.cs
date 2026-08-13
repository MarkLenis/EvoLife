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
}
