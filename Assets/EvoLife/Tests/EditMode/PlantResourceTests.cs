using NUnit.Framework;
using UnityEngine;
using EvoLife.Environment;

namespace EvoLife.Tests
{
    public sealed class PlantResourceTests
    {
        [Test]
        public void TryConsume_DepletesRemainingFood()
        {
            var stock = new PlantStock(capacity: 20f, remaining: 20f, regenPerSecond: 0f);
            var taken = stock.TryConsume(7f);

            Assert.AreEqual(7f, taken);
            Assert.AreEqual(13f, stock.Remaining);
            Assert.IsFalse(stock.IsDepleted);
        }

        [Test]
        public void TryConsume_CannotExceedRemaining()
        {
            var stock = new PlantStock(capacity: 5f, remaining: 5f, regenPerSecond: 0f);
            var taken = stock.TryConsume(9f);

            Assert.AreEqual(5f, taken);
            Assert.AreEqual(0f, stock.Remaining);
            Assert.IsTrue(stock.IsDepleted);
        }

        [Test]
        public void TryConsume_ZeroOrNegative_ReturnsZero()
        {
            var stock = new PlantStock(capacity: 10f, remaining: 10f, regenPerSecond: 0f);

            Assert.AreEqual(0f, stock.TryConsume(0f));
            Assert.AreEqual(0f, stock.TryConsume(-4f));
            Assert.AreEqual(10f, stock.Remaining);
        }

        [Test]
        public void Regeneration_RefillsInPlaceWithoutRespawn()
        {
            var stock = new PlantStock(capacity: 10f, remaining: 1f, regenPerSecond: 2f);
            stock.Tick(2f);

            Assert.AreEqual(5f, stock.Remaining);
            Assert.AreEqual(10f, stock.Capacity);
        }

        [Test]
        public void Regeneration_WaitsConfiguredDelayAfterDepletion()
        {
            var stock = new PlantStock(capacity: 8f, remaining: 0f, regenPerSecond: 4f, regenDelaySeconds: 2f);
            stock.Tick(1.5f);
            Assert.AreEqual(0f, stock.Remaining);

            stock.Tick(0.5f);
            Assert.AreEqual(2f, stock.Remaining, 0.0001f);
        }

        [Test]
        public void Tick_DoesNotExceedCapacity()
        {
            var stock = new PlantStock(capacity: 4f, remaining: 3.5f, regenPerSecond: 10f);
            stock.Tick(1f);
            Assert.AreEqual(4f, stock.Remaining);
        }

        [Test]
        public void PlantResource_ForwardsConsumeAndTick()
        {
            var go = new GameObject("PlantUnderTest");
            try
            {
                var plant = go.AddComponent<PlantResource>();
                plant.Configure(12f, 12f, 1f, regenDelay: 0f);

                Assert.AreEqual(6f, plant.TryConsume(6f));
                plant.Tick(2f);
                Assert.AreEqual(8f, plant.AvailableAmount);
                Assert.AreEqual(12f, plant.Capacity);
            }
            finally
            {
                Object.DestroyImmediate(go);
            }
        }
    }

    public sealed class WaterSourceTests
    {
        [Test]
        public void InfiniteWater_DoesNotDisappearWhenDrunk()
        {
            var stock = new WaterStock(infiniteSource: true, drinkAmountPerRequest: 10f);
            var taken = stock.TryConsume(7f);

            Assert.AreEqual(7f, taken);
            Assert.IsFalse(stock.IsDepleted);
            Assert.IsTrue(float.IsPositiveInfinity(stock.Remaining));
        }

        [Test]
        public void FiniteWater_RechargesWhenConfigured()
        {
            var stock = new WaterStock(
                infiniteSource: false,
                capacity: 20f,
                remaining: 2f,
                drinkAmountPerRequest: 10f,
                rechargePerSecond: 3f);

            Assert.AreEqual(2f, stock.TryConsume(5f));
            Assert.AreEqual(0f, stock.Remaining);
            stock.Tick(4f);
            Assert.AreEqual(12f, stock.Remaining);
        }

        [Test]
        public void Water_ZeroOrNegativeRequest_ReturnsZero()
        {
            var stock = new WaterStock(infiniteSource: true);
            Assert.AreEqual(0f, stock.TryConsume(0f));
            Assert.AreEqual(0f, stock.TryConsume(-2f));
        }
    }
}
