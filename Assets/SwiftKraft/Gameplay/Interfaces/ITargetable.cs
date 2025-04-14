namespace SwiftKraft.Gameplay.Interfaces
{
    public interface ITargetable : IFaction, IDamagable, ILineOfSight
    {
        public int Priority { get; set; }
    }
}