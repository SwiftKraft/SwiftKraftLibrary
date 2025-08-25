using SwiftKraft.UI.Menus;
using SwiftKraft.Utils;

namespace SwiftKraft.Gameplay.NPCs.Demo
{
    public class SpawnMenu : MenuBase
    {
        BooleanLock.Lock cursorLock;

        private void Awake() => cursorLock = CursorManager.Unlocked.AddLock();
        private void OnDestroy() => cursorLock.Dispose();

        protected override void ActiveChanged(bool active)
        {
            base.ActiveChanged(active);
            cursorLock.Active = !CursorManager.DefaultUnlocked && active;
        }
    }
}
