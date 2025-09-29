using System.Collections.Generic;

namespace SwiftKraft.Gameplay.Common.FPS.Interfaces
{
    public interface IFirstPersonSwitcher
    {
        List<IFirstPersonSwitchable> Switchables { get; }
    }
}
