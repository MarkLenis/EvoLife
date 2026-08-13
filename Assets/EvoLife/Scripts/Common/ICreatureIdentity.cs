namespace EvoLife.Common
{
    /// <summary>
    /// Minimal creature identity + role facade for cross-module lookups.
    /// </summary>
    public interface ICreatureIdentity
    {
        CreatureId Id { get; }
        CreatureRole Role { get; }
        string SpeciesId { get; }
    }
}
