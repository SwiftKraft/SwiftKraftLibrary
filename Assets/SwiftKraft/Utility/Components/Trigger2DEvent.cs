using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace SwiftKraft.Utils
{
    public class Trigger2DEvent : MonoBehaviour
    {
        public string Tag = "Player";

        public UnityEvent<Collider2D> OnTriggerEnterEvent;
        public UnityEvent<Collider2D> OnTriggerExitEvent;

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (other.CompareTag(Tag))
                OnTriggerEnterEvent?.Invoke(other);
        }

        private void OnTriggerExit2D(Collider2D other)
        {
            if (other.CompareTag(Tag))
                OnTriggerExitEvent?.Invoke(other);
        }
    }
}
