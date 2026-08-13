namespace EvoLife.Creatures
{
    /// <summary>
    /// Current physical exertion during a biology tick. Behavior/AI systems choose this per tick.
    /// </summary>
    public enum ActivityLevel
    {
        Idle = 0,
        Resting,
        Walking,
        Sprinting,
        Attacking,
    }
}
