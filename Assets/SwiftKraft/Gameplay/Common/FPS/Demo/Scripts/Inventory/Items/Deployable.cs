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
        public EquippedItemWaitState DeployState;

        public RaycastBuilder Builder { get; private set; }

        public GameObject Prefab;

        protected override void Awake()
        {
            base.Awake();
            Builder = GetComponent<RaycastBuilder>();
            Builder.Prefab = Prefab;

            EquipStateInstance = EquipState;
            UnequipStateInstance = UnequipState;

            DeployState.Init(this);
        }

        protected override void Update()
        {
            base.Update();

            if (Input.GetKeyDown(KeyCode.Mouse0) && CurrentState == IdleStateInstance && Builder.TestBuild())
                CurrentState = DeployState;

            if (Input.GetKeyDown(KeyCode.G))
                Parent.WishEquip = null;

            Builder.enabled = CurrentState == IdleStateInstance || CurrentState == DeployState;
        }

        public void Lock(bool state) => Builder.Lock = state;

        public void Despawn()
        {
            if (Builder.TryBuild())
                Instance.Despawn();
        }
    }
}
