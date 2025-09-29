namespace SwiftKraft.Gameplay.Interfaces
{
    public interface IAmmo
    {
        int MaxAmmo { get; }
        int CurrentAmmo { get; set; }
    }
}
