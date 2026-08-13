namespace EvoLife.Common
{
    /// <summary>
    /// Identity for an experiment / evaluation run (shared with backend records).
    /// </summary>
    public readonly struct ExperimentId : System.IEquatable<ExperimentId>
    {
        public readonly string Value;

        public ExperimentId(string value) => Value = value ?? string.Empty;

        public bool Equals(ExperimentId other) => Value == other.Value;
        public override bool Equals(object obj) => obj is ExperimentId other && Equals(other);
        public override int GetHashCode() => Value != null ? Value.GetHashCode() : 0;
        public override string ToString() => Value;

        public static bool operator ==(ExperimentId left, ExperimentId right) => left.Equals(right);
        public static bool operator !=(ExperimentId left, ExperimentId right) => !left.Equals(right);
    }
}
