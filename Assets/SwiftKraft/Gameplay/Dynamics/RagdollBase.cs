using UnityEngine;

namespace SwiftKraft.Gameplay.Dynamics
{
    public abstract class RagdollBase : MonoBehaviour
    {
        [Header("Cleanup")]
        public GameObject DestroyTarget;
        public bool DestroyAfter;
        public float DestroyTime;

        public abstract bool State { get; set; }

        public abstract void Ragdoll(bool state);
    }
}
