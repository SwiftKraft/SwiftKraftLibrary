using UnityEngine;

namespace SwiftKraft.Gameplay.Dynamics
{
    public abstract class RagdollBase : MonoBehaviour
    {
        [Header("Settings")]
        public bool DefaultState = false;
        [Header("Cleanup")]
        public GameObject DestroyTarget;
        public bool DestroyAfter;
        public float DestroyTime;

        public abstract bool State { get; set; }

        public abstract void Ragdoll(bool state);

        protected virtual void Start() => State = DefaultState;
    }
}
