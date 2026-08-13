using UnityEngine;
using EvoLife.Common;

namespace EvoLife.AI
{
    /// <summary>
    /// Pure nearest-by-role selection used by physics sensing and EditMode tests.
    /// A nearer herbivore cannot hide a predator, and a nearer predator cannot hide prey.
    /// </summary>
    public static class CreatureProximitySelection
    {
        public static void SelectNearestByRole(
            Vector3 origin,
            float range,
            Vector3[] positions,
            CreatureRole[] roles,
            int count,
            out int herbivoreIndex,
            out int predatorIndex)
        {
            herbivoreIndex = -1;
            predatorIndex = -1;
            if (positions == null || roles == null || range <= 0f || count <= 0)
            {
                return;
            }

            var limit = Mathf.Min(count, Mathf.Min(positions.Length, roles.Length));
            var bestHerbivoreSqr = range * range;
            var bestPredatorSqr = range * range;

            for (var i = 0; i < limit; i++)
            {
                var offset = positions[i] - origin;
                offset.y = 0f;
                var sqr = offset.sqrMagnitude;
                if (roles[i] == CreatureRole.Predator)
                {
                    if (sqr <= bestPredatorSqr)
                    {
                        bestPredatorSqr = sqr;
                        predatorIndex = i;
                    }
                }
                else if (sqr <= bestHerbivoreSqr)
                {
                    bestHerbivoreSqr = sqr;
                    herbivoreIndex = i;
                }
            }
        }
    }
}
