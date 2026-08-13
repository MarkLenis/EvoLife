using System;

namespace EvoLife.Common
{
    /// <summary>
    /// Cross-module death notice. Analytics and Simulation subscribe via
    /// <see cref="ICreatureDeathObservable"/>; Creatures still own vitals mutation.
    /// </summary>
    public readonly struct CreatureDeathNotice
    {
        public CreatureDeathNotice(CreatureId id, DeathCause cause, float age, float maxAge)
        {
            Id = id;
            Cause = cause;
            Age = age;
            MaxAge = maxAge;
        }

        public CreatureId Id { get; }
        public DeathCause Cause { get; }
        public float Age { get; }
        public float MaxAge { get; }
    }

    public interface ICreatureDeathObservable
    {
        event Action<CreatureDeathNotice> DeathObserved;
    }
}
