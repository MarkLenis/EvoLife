namespace EvoLife.AI
{
    /// <summary>
    /// Executes canonical CreatureActionSchema v2 actions: local locomotion plus discrete interaction.
    /// PPO and the scripted baseline must share this path.
    /// </summary>
    public interface IActionExecutor
    {
        int ActionSize { get; }

        /// <summary>
        /// Applies continuous locomotion with interaction = none.
        /// </summary>
        void ApplyActions(float[] actions);

        /// <summary>
        /// Applies continuous locomotion and one discrete interaction from
        /// <see cref="CreatureActionSchema"/>. Invalid interactions are safe no-ops.
        /// </summary>
        void ApplyActions(float[] continuousActions, int interaction);
    }
}
