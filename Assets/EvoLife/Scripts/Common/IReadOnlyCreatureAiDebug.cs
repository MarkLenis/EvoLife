namespace EvoLife.Common
{
    /// <summary>
    /// AI-owned read-only debug contract for inspectors and overlays.
    /// Does not expose policy internals. Missing components are treated as unavailable.
    /// </summary>
    public interface IReadOnlyCreatureAiDebug
    {
        string ControlMode { get; }

        string BehaviorName { get; }

        float Forward { get; }

        float Turn { get; }

        float SprintOrEffort { get; }

        string InteractionRequest { get; }

        bool HasScriptedMotive { get; }

        string ScriptedMotive { get; }

        float SensoryRange { get; }

        float InteractionRange { get; }

        float HeadingX { get; }

        float HeadingZ { get; }

        SensedTargetDebug NearestFood { get; }

        SensedTargetDebug NearestWater { get; }

        SensedTargetDebug NearestHerbivore { get; }

        SensedTargetDebug NearestPredator { get; }

        bool HasHeuristicTarget { get; }

        SensedTargetDebug HeuristicTarget { get; }
    }
}
