using SwiftKraft.Gameplay.Inventory.Items;
using System;
using System.Linq;
using UnityEngine;

namespace SwiftKraft.Gameplay.Common.Characters
{
    public class ThirdPersonModel : MonoBehaviour
    {
        public EquippedItemBase EquippedItem { get; private set; }
        public ThirdPersonAnimator Animator { get; private set; }

        public AnimationClip Idle;

        public Animation[] Animations;

        string queuedAction;
        bool initialized;

        private void Awake()
        {
            EquippedItem = GetComponentInParent<EquippedItemBase>();
            EquippedItem.OnStartAction += OnStartAction;
        }

        private void Start()
        {
            Animator = GetComponentInParent<ThirdPersonAnimator>();
            initialized = true;
            Animator.Idle = Idle;
            Animator.UpdateCustom();
            if (!string.IsNullOrEmpty(queuedAction))
                OnStartAction(queuedAction);
        }

        private void OnEnable()
        {
            if (!initialized)
                return;

            Animator.Idle = Idle;
            Animator.UpdateCustom();
        }

        private void OnDisable()
        {
            if (!initialized)
                return;

            Animator.Idle = null;
            Animator.UpdateCustom();
        }

        private void OnStartAction(string obj)
        {
            if (!isActiveAndEnabled)
                return;

            if (!initialized)
            {
                queuedAction = obj;
                return;
            }

            Animation anim = Animations.FirstOrDefault(n => n.ID.Equals(obj));
            if (anim != null)
                Animator.PlayCustom(anim.Clip, anim.TransitionDuration);
        }

        private void OnDestroy() => EquippedItem.OnStartAction -= OnStartAction;

        [Serializable]
        public class Animation
        {
            public string ID;
            public AnimationClip Clip;
            public float TransitionDuration = 0.2f;

            public ThirdPersonModel Parent { get; private set; }

            public void Init(ThirdPersonModel parent) => Parent = parent;
        }
    }
}
