using EvoLife.Common;

namespace EvoLife.Environment
{
    /// <summary>
    /// Resource mutation API used by ecological events. Implemented by <see cref="ResourceManager"/>.
    /// </summary>
    public interface IEnvironmentEffectHost
    {
        void PushEventModifiers(
            int eventId,
            float regenMultiplier,
            float temperatureDelta,
            float waterRechargeMultiplier,
            BiomeKind[] biomes);

        void RemoveEventModifiers(int eventId);

        void BoostPlantAvailability(float amount, BiomeKind[] biomes);

        void DepletePlants(float fraction, BiomeKind[] biomes);

        float TemperatureNormalized { get; }

        ResourceCensus CaptureCensus();
    }
}
