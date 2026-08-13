using System.Collections.Generic;
using UnityEngine;

namespace EvoLife.Environment
{
    /// <summary>
    /// Lightweight registry for spatial resource queries used by AI observations later.
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
    }
}
