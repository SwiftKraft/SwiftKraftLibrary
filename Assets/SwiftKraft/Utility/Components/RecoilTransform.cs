using SwiftKraft.Gameplay.Interfaces;
using UnityEngine;

namespace SwiftKraft.Utils
{
    public class RecoilTransform : MonoBehaviour, IRecoilTransform
    {
        public Transform Transform => transform;
    }
}
