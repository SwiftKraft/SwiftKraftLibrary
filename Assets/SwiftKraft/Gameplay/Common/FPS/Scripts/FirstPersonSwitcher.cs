using SwiftKraft.Gameplay.Common.FPS.Interfaces;
using System.Collections.Generic;
using UnityEngine;

namespace SwiftKraft.Gameplay.Common.FPS
{
    public class FirstPersonSwitcher : MonoBehaviour, IFirstPersonSwitcher
    {
        public List<IFirstPersonSwitchable> Switchables { get; } = new();

        public void Set(bool firstPerson)
        {
            for (int i = Switchables.Count - 1; i >= 0; i--)
            {
                if ((Object)Switchables[i] != null)
                    Switchables[i].FirstPerson = firstPerson;
                else
                    Switchables.RemoveAt(i);
            }
        }
    }
}
