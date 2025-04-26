using UnityEngine;

namespace SwiftKraft.Gameplay.Interfaces
{
    public interface ILookable
    {
        public Transform LookPoint { get; }

        public Quaternion WishLookRotation { get; set; }
    }
}
