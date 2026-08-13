using UnityEngine;

namespace EvoLife.Environment
{
    public enum ResourceKind : byte
    {
        Plant = 0,
        Water = 1
    }

    /// <summary>
    /// Read contract for consumable world resources.
    /// </summary>
    public interface IResourceNode
    {
        ResourceKind Kind { get; }
        Vector3 Position { get; }
        float AvailableAmount { get; }
        bool IsDepleted { get; }

        /// <summary>Attempts to consume up to requested amount. Returns amount actually taken.</summary>
        float TryConsume(float requestedAmount);
    }
}
