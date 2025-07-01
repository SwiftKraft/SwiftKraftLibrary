using FishNet.Object;
using UnityEngine.Events;

namespace SwiftKraft.Networking
{
    public class OwnershipEvents : NetworkBehaviour
    {
        public UnityEvent OnOwner;
        public UnityEvent OnRemote;

        public override void OnStartClient()
        {
            if (IsOwner)
                OnOwner?.Invoke();
            else
                OnRemote?.Invoke();
        }
    }
}
