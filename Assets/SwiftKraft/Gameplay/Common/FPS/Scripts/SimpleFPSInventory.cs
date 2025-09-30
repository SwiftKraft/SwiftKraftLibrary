using SwiftKraft.Gameplay.Interfaces;
using SwiftKraft.Gameplay.Inventory;
using SwiftKraft.Gameplay.Inventory.Items;
using SwiftKraft.Utils;
using System;
using UnityEngine;

namespace SwiftKraft.Gameplay.Common.FPS
{
    public class SimpleFPSInventory : InventoryBase
    {
        public EquippableItemType[] DefaultItems;

        public IItemEquipper Equipper { get; private set; }

        public SlotSelector[] Selectors;

        public Transform DropTransform;
        public Vector3 DropOffset;
        public float ThrowStrength = 10f;


        public KeyCode DropKey;

        private void Awake()
        {
            Equipper = GetComponentInChildren<IItemEquipper>();

            foreach (EquippableItemType it in DefaultItems)
                AddItem(new(it));
        }

        private void Update()
        {
            foreach (SlotSelector sel in Selectors)
                if (Input.GetKeyDown(sel.Key) && Data.Items.Count > sel.Slot && Data.Items[sel.Slot] > 0)
                    Equip(sel.Slot);

            if (Input.GetKeyDown(DropKey))
                Drop();
        }

        public void Equip(int index)
        {
            if (Data.Items.InRange(index))
                Equipper.Equip(Data.Items[index]);
        }

        public void Drop()
        {
            if (Equipper.Current == null)
                return;

            WorldItemBase wib = DropItem(Equipper.Current.Instance, DropTransform.position + DropTransform.rotation * DropOffset, DropTransform.rotation);
            Equipper.ForceUnequip(true);
            if (wib.TryGetComponent(out Rigidbody rb))
                rb.AddForce(DropTransform.forward * ThrowStrength, ForceMode.Impulse);
        }

        public void DropInventory() => DropInventory(DropTransform.position + DropTransform.rotation * DropOffset, DropTransform.rotation);

        protected override void OnDestroy()
        {
#if UNITY_EDITOR
            if (!UnityEditor.EditorApplication.isPlayingOrWillChangePlaymode && UnityEditor.EditorApplication.isPlaying)
                return;
#endif

            if (DropOnDestroy)
                DropInventory();
        }

        [Serializable]
        public class SlotSelector
        {
            public KeyCode Key;
            public int Slot;
        }
    }
}
