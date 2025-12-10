namespace SwiftKraft.Gameplay.Interfaces
{
    public interface ITargetable : IFaction, IDamagable, ILineOfSight
    {
        public int Priority { get; set; }

        public bool CanTarget { get; }

        public object GetStatus(string statusId);
    }
}