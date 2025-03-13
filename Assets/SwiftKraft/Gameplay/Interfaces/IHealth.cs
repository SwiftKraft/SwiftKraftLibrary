namespace SwiftKraft.Gameplay.Interfaces
{
    public interface IHealth<T> : IDamagable
    {
        float MaxHealth { get; }
        float CurrentHealth { get; set; }
    }
}
