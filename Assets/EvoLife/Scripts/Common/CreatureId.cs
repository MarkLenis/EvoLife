namespace EvoLife.Common
{
    /// <summary>
    /// Stable identity for a creature instance within a simulation run.
    /// </summary>
    public readonly struct CreatureId : System.IEquatable<CreatureId>
    {
        public readonly int Value;

        public CreatureId(int value) => Value = value;

        public bool Equals(CreatureId other) => Value == other.Value;
        public override bool Equals(object obj) => obj is CreatureId other && Equals(other);
        public override int GetHashCode() => Value;
        public override string ToString() => $"Creature:{Value}";

        public static bool operator ==(CreatureId left, CreatureId right) => left.Equals(right);
        public static bool operator !=(CreatureId left, CreatureId right) => !left.Equals(right);
    }
}
