using SwiftKraft.Gameplay.Inventory.Items;

namespace SwiftKraft.Gameplay.Weapons
{
    public class EquippedWeaponBase : EquippedItemDrawTime
    {
        public EquippedItemWaitState EquipState;
        public EquippedItemWaitState UnequipState;

        public class Idle : EquippedItemState<EquippedWeaponBase>
        {
            public override void Begin()
            {
                throw new System.NotImplementedException();
            }

            public override void End()
            {
                throw new System.NotImplementedException();
            }

            public override void Frame()
            {
                throw new System.NotImplementedException();
            }

            public override void Tick()
            {
                throw new System.NotImplementedException();
            }
        }
    }
}
