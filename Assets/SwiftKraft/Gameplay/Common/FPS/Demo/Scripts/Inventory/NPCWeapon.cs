using SwiftKraft.Gameplay.Interfaces;
using SwiftKraft.Gameplay.Inventory.Items;
using SwiftKraft.Gameplay.NPCs;
using SwiftKraft.Gameplay.Weapons;
using UnityEngine;

namespace SwiftKraft.Gameplay.Common.FPS.Demo
{
    [RequireComponent(typeof(IItemEquipper), typeof(NPCAttackerWeapon))]
    public class NPCWeapon : MonoBehaviour
    {
        public NPCAttackerWeapon Attacker { get; private set; }
        public IItemEquipper Equipper { get; private set; }

        public ItemType StartingItem;

        ItemInstance instance;

        private void Awake()
        {
            Attacker = GetComponent<NPCAttackerWeapon>();
            Equipper = GetComponent<IItemEquipper>();
            instance = new ItemInstance(StartingItem);
        }

        private void Start() => Equipper.Equip(instance);

        private void FixedUpdate()
        {
            if (Attacker.EquippedWeapon == null)
                Attacker.EquippedWeapon = Equipper.Current as EquippedWeaponBase;
        }
    }
}
