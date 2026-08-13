namespace EvoLife.Common
{
    /// <summary>
    /// Port for ecological events to apply biological damage.
    /// Simulation implements this by calling <c>CreatureVitals.ApplyDamage</c>.
    /// Environment must not own or mutate <c>CreatureBiology</c>.
    /// </summary>
    public interface IEnvironmentalVitalEffects
    {
        /// <summary>
        /// Damages living creatures through Creatures public APIs.
        /// Lethal damage publishes death once; callers must not also call Die.
        /// Returns how many living creatures were given a damage call.
        /// </summary>
        int ApplyEnvironmentalDamage(float amount, DeathCause cause);
    }
}
