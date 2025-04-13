namespace SwiftKraft.Gameplay.Interfaces
{
    public interface IHealth : IDamagable
    {
        float MaxHealth { get; }
        float CurrentHealth { get; set; }
    }
}
