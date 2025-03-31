namespace SwiftKraft.Utils
{
    public abstract class OverrideBase
    {
        public abstract void Dispose();
    }

    public abstract class OverrideBase<T1> : OverrideBase where T1 : IOverrideParent
    {
        public readonly T1 Parent;

        public OverrideBase(T1 parent) => Parent = parent;

        public override void Dispose() => Parent.RemoveOverride(this);
    }
}
