using UnityEngine;

namespace SwiftKraft.Gameplay.Dynamics
{
    public abstract class RagdollBase : MonoBehaviour
    {
        public abstract bool State { get; set; }

        public abstract void Ragdoll(bool state);
    }
}
