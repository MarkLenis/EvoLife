namespace EvoLife.AI
{
    /// <summary>
    /// Optional seam for a future reproduction system.
    /// <see cref="CreatureActionSchema.InteractionReproduceRequest"/> is reserved in action
    /// schema v2 so trained dimensions do not change when reproduction gameplay is added.
    /// Unattached handlers are a safe no-op.
    /// </summary>
    public interface IReproductionRequestHandler
    {
        void HandleReproduceRequest();
    }

    /// <summary>
    /// Local interaction adapter shared by PPO and the scripted baseline.
    /// Implementations must call Creatures / Environment owner APIs
    /// (<c>ConsumeFood</c>, <c>Drink</c>, <c>ApplyDamage</c>, <c>IResourceNode.TryConsume</c>)
    /// and must not write vital fields or teleport.
    /// </summary>
    public interface ICreatureInteractor
    {
        bool TryEat();

        bool TryDrink();

        bool TryAttack();

        void SetResting();

        /// <summary>
        /// Forwards a reproduce request to an optional future reproduction system.
        /// No-op when no handler is attached.
        /// </summary>
        void RequestReproduce();
    }

    /// <summary>
    /// Canonical discrete-interaction dispatch used by the action executor.
    /// </summary>
    public static class CreatureActionExecution
    {
        public static bool TryApplyInteraction(ICreatureInteractor interactor, int interaction)
        {
            if (interactor == null)
            {
                return false;
            }

            switch (CreatureActionSchema.ClampInteraction(interaction))
            {
                case CreatureActionSchema.InteractionEat:
                    return interactor.TryEat();
                case CreatureActionSchema.InteractionDrink:
                    return interactor.TryDrink();
                case CreatureActionSchema.InteractionAttack:
                    return interactor.TryAttack();
                case CreatureActionSchema.InteractionRest:
                    interactor.SetResting();
                    return true;
                case CreatureActionSchema.InteractionReproduceRequest:
                    interactor.RequestReproduce();
                    return true;
                default:
                    return false;
            }
        }
    }
}
