using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SwiftKraft.Utils
{
    public class DestroyAfter : MonoBehaviour
    {
        public float Time = 1f;

        private void Start() => Destroy(gameObject, Time);
    }
}
