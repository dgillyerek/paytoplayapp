namespace Grove.Domain.Energy
{
    /// <summary>HUD / presentation hook. Domain calls this; Unity binds a 2D empty-state prompt.</summary>
    public interface IEnergyListener
    {
        void OnEnergyChanged(int current, int cap);

        void OnEnergyEmpty();
    }

    /// <summary>Spend seam used by producers (and later dig). DEV-003 crates call produce; DEV-004 implements the wallet.</summary>
    public interface IEnergySpender
    {
        bool TrySpendProduce();

        bool TrySpendDig();
    }
}
