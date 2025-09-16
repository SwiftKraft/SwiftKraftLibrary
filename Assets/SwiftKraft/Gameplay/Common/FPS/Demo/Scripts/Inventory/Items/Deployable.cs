using SwiftKraft.Gameplay.Building;
using SwiftKraft.Gameplay.Inventory.Items;
using UnityEngine;

namespace SwiftKraft.Gameplay.Common.FPS.Demo
{
    [RequireComponent(typeof(RaycastBuilder))]
    public class Deployable : EquippedItemDrawTime
    {
        public EquippedItemWaitState EquipState;
        public EquippedItemWaitState UnequipState;

        public RaycastBuilder Builder { get; private set; }

        public GameObject Prefab;

        protected override void Awake()
        {
            base.Awake();
            Builder = GetComponent<RaycastBuilder>();
            Builder.Prefab = Prefab;

            EquipStateInstance = EquipState;
            UnequipStateInstance = UnequipState;
        }

        protected override void Update()
        {
            base.Update();

            if (Input.GetKeyDown(KeyCode.Mouse0) && Builder.TryBuild())
                Instance.Despawn();

            if (Input.GetKeyDown(KeyCode.G))
                Parent.WishEquip = null;

            Builder.enabled = CurrentState == IdleStateInstance;
        }
    }
}
