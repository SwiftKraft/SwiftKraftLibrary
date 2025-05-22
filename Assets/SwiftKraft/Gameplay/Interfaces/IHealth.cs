namespace SwiftKraft.Gameplay.Interfaces
{
    public interface IHealth : IDamagable, IKillable
    {
        float MaxHealth { get; }
        float CurrentHealth { get; set; }
    }
}
