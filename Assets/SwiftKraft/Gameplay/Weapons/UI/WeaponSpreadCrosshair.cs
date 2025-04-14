using SwiftKraft.UI.HUD;
using SwiftKraft.Utils;

namespace SwiftKraft.Gameplay.Weapons.UI
{
    public class WeaponSpreadCrosshair : RequiredDependencyComponent<WeaponSpread>
    {
        protected virtual void Update()
        {
            if (Crosshair.Instance == null)
                return;

            Crosshair.Instance.SetDegrees(Component.GetSpread());
        }
    }
}
