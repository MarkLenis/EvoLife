namespace EvoLife.Common
{
    /// <summary>
    /// One locally sensed target in agent-local XZ, matching CreatureObservationSchema
    /// channel layout. Distance is normalized by sense range. UI converts to world lines.
    /// </summary>
    public readonly struct SensedTargetDebug
    {
        public SensedTargetDebug(bool present, float localDirX, float localDirZ, float normalizedDistance)
        {
            Present = present;
            LocalDirX = localDirX;
            LocalDirZ = localDirZ;
            NormalizedDistance = normalizedDistance < 0f ? 0f : normalizedDistance;
        }

        public bool Present { get; }
        public float LocalDirX { get; }
        public float LocalDirZ { get; }
        public float NormalizedDistance { get; }

        public static SensedTargetDebug None => default;
    }
}
