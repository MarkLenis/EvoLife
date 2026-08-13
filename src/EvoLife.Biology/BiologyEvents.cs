namespace EvoLife.Biology
{
    public readonly struct HealthChangedEventArgs
    {
        public HealthChangedEventArgs(float previousHealth, float currentHealth, float maxHealth)
        {
            PreviousHealth = previousHealth;
            CurrentHealth = currentHealth;
            MaxHealth = maxHealth;
        }

        public float PreviousHealth { get; }
        public float CurrentHealth { get; }
        public float MaxHealth { get; }
        public float Delta => CurrentHealth - PreviousHealth;
    }

    public readonly struct CreatureDiedEventArgs
    {
        public CreatureDiedEventArgs(DeathCause cause, CreatureState finalState)
        {
            Cause = cause;
            FinalState = finalState;
        }

        public DeathCause Cause { get; }
        public CreatureState FinalState { get; }
    }

    public readonly struct CreatureStateChangedEventArgs
    {
        public CreatureStateChangedEventArgs(CreatureState previousState, CreatureState currentState, string changeKind)
        {
            PreviousState = previousState;
            CurrentState = currentState;
            ChangeKind = changeKind;
        }

        public CreatureState PreviousState { get; }
        public CreatureState CurrentState { get; }
        public string ChangeKind { get; }
    }
}
