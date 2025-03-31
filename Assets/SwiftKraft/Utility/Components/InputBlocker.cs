using System.Collections.Generic;
using UnityEngine;

namespace SwiftKraft.Utils
{
    public class InputBlocker : MonoBehaviour
    {
        public static readonly List<InputBlocker> Blockers = new();

        public static bool Blocked
        {
            get
            {
                foreach (InputBlocker blocker in Blockers)
                    if (blocker.Blocking)
                        return true;
                return false;
            }
        }

        public bool ActivenessBlocking = true;

        public bool Blocking { get; set; }

        private void Awake() => Blockers.Add(this);

        private void Start()
        {
            if (ActivenessBlocking)
                Blocking = true;
        }

        private void OnEnable()
        {
            if (ActivenessBlocking)
                Blocking = true;
        }

        private void OnDisable()
        {
            if (ActivenessBlocking)
                Blocking = false;
        }

        private void OnDestroy() => Blockers.Remove(this);
    }
}
