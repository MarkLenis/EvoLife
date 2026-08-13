namespace EvoLife.AI
{
    /// <summary>
    /// Optional local interaction adapter for the scripted baseline.
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
    }
}
