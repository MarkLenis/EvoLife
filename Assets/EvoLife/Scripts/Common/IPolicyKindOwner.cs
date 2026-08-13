namespace EvoLife.Common
{
    /// <summary>
    /// Implemented by creature brains so Simulation can select scripted vs PPO
    /// without referencing the AI assembly.
    /// </summary>
    public interface IPolicyKindOwner
    {
        AgentPolicyKind PolicyKind { get; }
        void SetPolicyKind(AgentPolicyKind kind);
    }
}
