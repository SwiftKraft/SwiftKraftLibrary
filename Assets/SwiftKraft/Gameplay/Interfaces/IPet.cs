namespace SwiftKraft.Gameplay.Interfaces
{
    public interface IPet : IEntity
    {
        public IEntity Owner { get; set; }
    }
}
