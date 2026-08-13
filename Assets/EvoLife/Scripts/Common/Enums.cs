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
}
