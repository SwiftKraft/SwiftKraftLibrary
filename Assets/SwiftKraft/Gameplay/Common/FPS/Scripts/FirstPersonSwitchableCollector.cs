using SwiftKraft.Gameplay.Common.FPS.Interfaces;
using System.Collections.Generic;
using UnityEngine;

namespace SwiftKraft.Gameplay.Common.FPS
{
    public class FirstPersonSwitchableCollector : MonoBehaviour
    {
        public FirstPersonSwitcher ParentSwitcher { get; private set; }
        public readonly List<IFirstPersonSwitchable> Switchables = new();

        private void Awake()
        {
            Switchables.AddRange(GetComponentsInChildren<IFirstPersonSwitchable>(true));
            ParentSwitcher = GetComponentInParent<FirstPersonSwitcher>();
            if (ParentSwitcher != null)
                ParentSwitcher.Switchables.AddRange(Switchables);
        }
    }
}
