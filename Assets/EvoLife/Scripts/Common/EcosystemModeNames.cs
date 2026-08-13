using System;

namespace EvoLife.Common
{
    /// <summary>
    /// Wire names for experiment configuration. Distinguishes persistent ecosystems
    /// from training-support runs that may enable controlled respawn.
    /// </summary>
    public static class EcosystemModeNames
    {
        public const string Persistent = "persistent_ecosystem";
        public const string TrainingSupport = "training_support";

        public static string ToWireName(EcosystemMode mode)
        {
            switch (mode)
            {
                case EcosystemMode.Persistent:
                    return Persistent;
                case EcosystemMode.TrainingSupport:
                    return TrainingSupport;
                default:
                    return Unreachable(mode);
            }
        }

        public static bool TryParse(string wireName, out EcosystemMode mode)
        {
            if (string.Equals(wireName, TrainingSupport, StringComparison.OrdinalIgnoreCase)
                || string.Equals(wireName, "TrainingSupport", StringComparison.OrdinalIgnoreCase))
            {
                mode = EcosystemMode.TrainingSupport;
                return true;
            }

            if (string.Equals(wireName, Persistent, StringComparison.OrdinalIgnoreCase)
                || string.Equals(wireName, "Persistent", StringComparison.OrdinalIgnoreCase))
            {
                mode = EcosystemMode.Persistent;
                return true;
            }

            mode = EcosystemMode.Persistent;
            return false;
        }

        static string Unreachable(EcosystemMode mode)
        {
            throw new ArgumentOutOfRangeException(nameof(mode), mode, "Unhandled EcosystemMode.");
        }
    }
}
