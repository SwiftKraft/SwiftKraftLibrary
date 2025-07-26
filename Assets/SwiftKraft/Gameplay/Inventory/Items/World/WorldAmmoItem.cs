using SwiftKraft.Gameplay.Weapons;

namespace SwiftKraft.Gameplay.Inventory.Items
{
    public class WorldAmmoItem : WorldItemAddonBase
    {
        public int InitialAmmo = 30;
        public int InitialReserve = 60;

        WeaponAmmo.Data data;

        public override void Init(WorldItemBase parent)
        {
            base.Init(parent);
            if (!Item.TryGetData(WeaponAmmo.AmmoSaveID, out data) && Item.TryAddData(WeaponAmmo.AmmoSaveID, out data))
            {
                data.CurrentAmmo = InitialAmmo;
                data.ReserveAmmo = InitialReserve;
            }
        }
    }
}
