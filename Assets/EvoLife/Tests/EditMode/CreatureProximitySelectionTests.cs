using NUnit.Framework;
using UnityEngine;
using EvoLife.AI;
using EvoLife.Common;

namespace EvoLife.Tests
{
    public sealed class CreatureProximitySelectionTests
    {
        [Test]
        public void NearerHerbivoreDoesNotHidePredator()
        {
            var positions = new[]
            {
                new Vector3(1f, 0f, 0f),
                new Vector3(0f, 0f, 8f)
            };
            var roles = new[] { CreatureRole.Herbivore, CreatureRole.Predator };

            CreatureProximitySelection.SelectNearestByRole(
                Vector3.zero,
                10f,
                positions,
                roles,
                2,
                out var herbivoreIndex,
                out var predatorIndex);

            Assert.AreEqual(0, herbivoreIndex);
            Assert.AreEqual(1, predatorIndex);
        }

        [Test]
        public void NearerPredatorDoesNotHideHerbivore()
        {
            var positions = new[]
            {
                new Vector3(1f, 0f, 0f),
                new Vector3(0f, 0f, 9f)
            };
            var roles = new[] { CreatureRole.Predator, CreatureRole.Herbivore };

            CreatureProximitySelection.SelectNearestByRole(
                Vector3.zero,
                10f,
                positions,
                roles,
                2,
                out var herbivoreIndex,
                out var predatorIndex);

            Assert.AreEqual(1, herbivoreIndex);
            Assert.AreEqual(0, predatorIndex);
        }

        [Test]
        public void AbsentRoles_ReturnNegativeIndices()
        {
            CreatureProximitySelection.SelectNearestByRole(
                Vector3.zero,
                10f,
                new[] { new Vector3(1f, 0f, 0f) },
                new[] { CreatureRole.Herbivore },
                1,
                out var herbivoreIndex,
                out var predatorIndex);

            Assert.AreEqual(0, herbivoreIndex);
            Assert.AreEqual(-1, predatorIndex);
        }
    }
}
