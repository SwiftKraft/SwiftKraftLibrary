using SwiftKraft.Utils;
using UnityEngine;

namespace SwiftKraft.Gameplay.Animation
{
    public class AnimationReceiver : MonoBehaviour
    {
        [field: SerializeField]
        public Transform[] Roots { get; private set; } = new Transform[1];

        public void Copy(Transform otherRoot, int index)
        {
            if (otherRoot == null || !Roots.InRange(index) || Roots[index] == null)
                return;

            HierarchyCopier.Copy(otherRoot, Roots[index], false);
        }
    }
}
