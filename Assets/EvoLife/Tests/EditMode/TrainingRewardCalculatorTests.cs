using NUnit.Framework;
using EvoLife.AI;

namespace EvoLife.Tests
{
    public sealed class TrainingRewardCalculatorTests
    {
        [Test]
        public void NullVitals_ReturnZeroWithoutTermination()
        {
            var calculator = new TrainingRewardCalculator();
            var signal = calculator.Evaluate(null);
            Assert.AreEqual(0f, signal.Reward, 0.0001f);
            Assert.IsFalse(signal.TerminateEpisode);
            Assert.AreEqual(0f, calculator.CalculateReward(null, episodeEnded: true), 0.0001f);
        }

        [Test]
        public void Death_ReturnsConfiguredPenaltyAndEndsEpisode()
        {
            var settings = TrainingRewardSettings.CreateDefault();
            settings.DeathPenalty = -1.5f;
            var calculator = new TrainingRewardCalculator(settings);
            var signal = calculator.Evaluate(new StubVitalState { IsAlive = false });

            Assert.AreEqual(-1.5f, signal.Reward, 0.0001f);
            Assert.IsTrue(signal.TerminateEpisode);
        }

        [Test]
        public void HungerRelief_IsRewarded()
        {
            var settings = TrainingRewardSettings.CreateDefault();
            settings.AliveReward = 0f;
            settings.EnergyMaintenanceScale = 0f;
            settings.CriticalNeedPenalty = 0f;
            settings.HungerReliefScale = 1f;
            settings.ThirstReliefScale = 0f;
            settings.HealthLossScale = 0f;
            var calculator = new TrainingRewardCalculator(settings);

            var hungry = new StubVitalState { Hunger = 80f, MaxHunger = 100f, Energy = 0f };
            var fed = new StubVitalState { Hunger = 20f, MaxHunger = 100f, Energy = 0f };

            calculator.Evaluate(hungry);
            var afterEating = calculator.Evaluate(fed);

            Assert.AreEqual(0.6f, afterEating.Reward, 0.0001f);
            Assert.IsFalse(afterEating.TerminateEpisode);
        }

        [Test]
        public void CriticalNeed_MakesStarvingAliveRewardNetNegative()
        {
            var settings = TrainingRewardSettings.CreateDefault();
            settings.AliveReward = 0.001f;
            settings.CriticalNeedPenalty = 0.004f;
            settings.CriticalNeedThreshold = 0.85f;
            settings.EnergyMaintenanceScale = 0f;
            var calculator = new TrainingRewardCalculator(settings);

            var comfortable = calculator.Evaluate(new StubVitalState
            {
                Hunger = 10f,
                Thirst = 10f,
                Energy = 0f
            });
            calculator.OnEpisodeBegin();
            var starving = calculator.Evaluate(new StubVitalState
            {
                Hunger = 90f,
                Thirst = 90f,
                Energy = 0f
            });

            Assert.Greater(comfortable.Reward, 0f);
            Assert.Less(starving.Reward, 0f);
        }

        [Test]
        public void OnEpisodeBegin_ClearsDeltaHistory()
        {
            var settings = TrainingRewardSettings.CreateDefault();
            settings.AliveReward = 0f;
            settings.EnergyMaintenanceScale = 0f;
            settings.CriticalNeedPenalty = 0f;
            settings.HungerReliefScale = 1f;
            var calculator = new TrainingRewardCalculator(settings);

            calculator.Evaluate(new StubVitalState { Hunger = 80f, Energy = 0f });
            calculator.OnEpisodeBegin();
            var firstStep = calculator.Evaluate(new StubVitalState { Hunger = 20f, Energy = 0f });

            Assert.AreEqual(0f, firstStep.Reward, 0.0001f);
        }

        [Test]
        public void SurvivalRewardCalculator_StillPenalizesDeath()
        {
            var calculator = new SurvivalRewardCalculator();
            Assert.AreEqual(-1f, calculator.CalculateReward(new StubVitalState { IsAlive = false }, true), 0.0001f);
        }
    }
}
