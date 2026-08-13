using System.Collections.Generic;
using UnityEngine;

namespace EvoLife.Environment
{
    /// <summary>
    /// Lightweight registry for spatial resource queries used by AI observations and analytics.
    /// </summary>
    public sealed class ResourceRegistry : MonoBehaviour
    {
        readonly List<IResourceNode> nodes = new List<IResourceNode>(64);

        public IReadOnlyList<IResourceNode> Nodes => nodes;

        public void Register(IResourceNode node)
        {
            if (node != null && !nodes.Contains(node))
            {
                nodes.Add(node);
            }
        }

        public void Unregister(IResourceNode node)
        {
            if (node != null)
            {
                nodes.Remove(node);
            }
        }

        public IResourceNode FindNearest(Vector3 origin, ResourceKind kind, float maxDistance)
        {
            IResourceNode best = null;
            var bestSqr = maxDistance * maxDistance;

            for (var i = 0; i < nodes.Count; i++)
            {
                var node = nodes[i];
                if (node == null || node.Kind != kind || node.IsDepleted)
                {
                    continue;
                }

                var sqr = (node.Position - origin).sqrMagnitude;
                if (sqr <= bestSqr)
                {
                    bestSqr = sqr;
                    best = node;
                }
            }

            return best;
        }

        public ResourceCensus CaptureCensus(float worldArea)
        {
            var plants = 0;
            var water = 0;
            var remaining = 0f;
            var capacity = 0f;

            for (var i = 0; i < nodes.Count; i++)
            {
                var node = nodes[i];
                if (node == null)
                {
                    continue;
                }

                switch (node.Kind)
                {
                    case ResourceKind.Plant:
                        plants++;
                        remaining += FiniteOrZero(node.AvailableAmount);
                        capacity += FiniteOrZero(node.Capacity);
                        break;
                    case ResourceKind.Water:
                        water++;
                        break;
                    default:
                        Unreachable(node.Kind);
                        continue;
                }
            }

            return new ResourceCensus(plants, water, remaining, capacity, worldArea);
        }

        static float FiniteOrZero(float value) =>
            float.IsNaN(value) || float.IsInfinity(value) ? 0f : (value < 0f ? 0f : value);

        static void Unreachable(ResourceKind kind)
        {
            throw new System.ArgumentOutOfRangeException(nameof(kind), kind, "Unhandled ResourceKind.");
        }
    }
}
