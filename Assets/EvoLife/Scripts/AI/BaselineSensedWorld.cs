using UnityEngine;
using EvoLife.Common;

namespace EvoLife.AI
{
    /// <summary>
    /// Parsed local observation snapshot used by the scripted baseline.
    /// Layout matches <see cref="CreatureObservationSchema"/> v2 so PPO and the baseline
    /// share the same sensory constraints. Missing or short buffers read as zeros.
    /// Herbivore and predator channels are independent.
    /// </summary>
    public readonly struct BaselineSensedWorld
    {
        const float PresentThreshold = 0.5f;
        const float PredatorRoleThreshold = 0.5f;

        public BaselineSensedWorld(
            float health,
            float hunger,
            float thirst,
            float energy,
            float age,
            float role,
            float foodDirX,
            float foodDirZ,
            float foodDistance,
            bool foodPresent,
            float waterDirX,
            float waterDirZ,
            float waterDistance,
            bool waterPresent,
            float herbivoreDirX,
            float herbivoreDirZ,
            float herbivoreDistance,
            bool herbivorePresent,
            float predatorDirX,
            float predatorDirZ,
            float predatorDistance,
            bool predatorPresent)
        {
            Health = health;
            Hunger = hunger;
            Thirst = thirst;
            Energy = energy;
            Age = age;
            Role = role;
            FoodDirX = foodDirX;
            FoodDirZ = foodDirZ;
            FoodDistance = foodDistance;
            FoodPresent = foodPresent;
            WaterDirX = waterDirX;
            WaterDirZ = waterDirZ;
            WaterDistance = waterDistance;
            WaterPresent = waterPresent;
            HerbivoreDirX = herbivoreDirX;
            HerbivoreDirZ = herbivoreDirZ;
            HerbivoreDistance = herbivoreDistance;
            HerbivorePresent = herbivorePresent;
            PredatorDirX = predatorDirX;
            PredatorDirZ = predatorDirZ;
            PredatorDistance = predatorDistance;
            PredatorPresent = predatorPresent;
        }

        public float Health { get; }
        public float Hunger { get; }
        public float Thirst { get; }
        public float Energy { get; }
        public float Age { get; }
        public float Role { get; }
        public float FoodDirX { get; }
        public float FoodDirZ { get; }
        public float FoodDistance { get; }
        public bool FoodPresent { get; }
        public float WaterDirX { get; }
        public float WaterDirZ { get; }
        public float WaterDistance { get; }
        public bool WaterPresent { get; }
        public float HerbivoreDirX { get; }
        public float HerbivoreDirZ { get; }
        public float HerbivoreDistance { get; }
        public bool HerbivorePresent { get; }
        public float PredatorDirX { get; }
        public float PredatorDirZ { get; }
        public float PredatorDistance { get; }
        public bool PredatorPresent { get; }

        public CreatureRole SelfRole =>
            Role >= PredatorRoleThreshold ? CreatureRole.Predator : CreatureRole.Herbivore;

        public static BaselineSensedWorld Empty => default;

        public static BaselineSensedWorld FromObservations(float[] observations)
        {
            return new BaselineSensedWorld(
                ReadUnit(observations, CreatureObservationSchema.IndexHealth),
                ReadUnit(observations, CreatureObservationSchema.IndexHunger),
                ReadUnit(observations, CreatureObservationSchema.IndexThirst),
                ReadUnit(observations, CreatureObservationSchema.IndexEnergy),
                ReadUnit(observations, CreatureObservationSchema.IndexAge),
                ReadUnit(observations, CreatureObservationSchema.IndexRole),
                ReadSigned(observations, CreatureObservationSchema.IndexFood + CreatureObservationSchema.OffsetDirX),
                ReadSigned(observations, CreatureObservationSchema.IndexFood + CreatureObservationSchema.OffsetDirZ),
                ReadUnit(observations, CreatureObservationSchema.IndexFood + CreatureObservationSchema.OffsetDistance),
                ReadPresent(observations, CreatureObservationSchema.IndexFood + CreatureObservationSchema.OffsetPresent),
                ReadSigned(observations, CreatureObservationSchema.IndexWater + CreatureObservationSchema.OffsetDirX),
                ReadSigned(observations, CreatureObservationSchema.IndexWater + CreatureObservationSchema.OffsetDirZ),
                ReadUnit(observations, CreatureObservationSchema.IndexWater + CreatureObservationSchema.OffsetDistance),
                ReadPresent(observations, CreatureObservationSchema.IndexWater + CreatureObservationSchema.OffsetPresent),
                ReadSigned(observations, CreatureObservationSchema.IndexHerbivore + CreatureObservationSchema.OffsetDirX),
                ReadSigned(observations, CreatureObservationSchema.IndexHerbivore + CreatureObservationSchema.OffsetDirZ),
                ReadUnit(observations, CreatureObservationSchema.IndexHerbivore + CreatureObservationSchema.OffsetDistance),
                ReadPresent(observations, CreatureObservationSchema.IndexHerbivore + CreatureObservationSchema.OffsetPresent),
                ReadSigned(observations, CreatureObservationSchema.IndexPredator + CreatureObservationSchema.OffsetDirX),
                ReadSigned(observations, CreatureObservationSchema.IndexPredator + CreatureObservationSchema.OffsetDirZ),
                ReadUnit(observations, CreatureObservationSchema.IndexPredator + CreatureObservationSchema.OffsetDistance),
                ReadPresent(observations, CreatureObservationSchema.IndexPredator + CreatureObservationSchema.OffsetPresent));
        }

        static float Read(float[] observations, int index)
        {
            if (observations == null || index < 0 || index >= observations.Length)
            {
                return 0f;
            }

            var value = observations[index];
            return float.IsNaN(value) || float.IsInfinity(value) ? 0f : value;
        }

        static float ReadUnit(float[] observations, int index) => Mathf.Clamp01(Read(observations, index));

        static float ReadSigned(float[] observations, int index) => Mathf.Clamp(Read(observations, index), -1f, 1f);

        static bool ReadPresent(float[] observations, int index) => Read(observations, index) >= PresentThreshold;
    }
}
