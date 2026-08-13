namespace EvoLife.AI
{
    /// <summary>
    /// Unity ML-Agents Behavior Parameters names. Training YAML keys must match these strings.
    /// </summary>
    public static class MlAgentsBehaviorNames
    {
        public const string Herbivore = "EvoLifeHerbivore";
        public const string Predator = "EvoLifePredator";

        public static string ForRole(Common.CreatureRole role) =>
            role == Common.CreatureRole.Predator ? Predator : Herbivore;
    }
}
