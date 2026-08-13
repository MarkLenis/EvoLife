namespace EvoLife.Common
{
    /// <summary>
    /// Optional seed hook so Simulation can make scripted wandering deterministic
    /// without referencing the AI assembly.
    /// </summary>
    public interface IPolicySeedOwner
    {
        void SetPolicySeed(int seed);
    }
}
