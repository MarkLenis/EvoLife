using EvoLife.Common;

namespace EvoLife.AI
{
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
        /// Forwards <see cref="CreatureActionSchema.InteractionReproduceRequest"/> to
        /// Simulation via <see cref="IReproductionRequestHandler"/>. No-op when no
        /// handler is attached. Does not decide mating success.
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
