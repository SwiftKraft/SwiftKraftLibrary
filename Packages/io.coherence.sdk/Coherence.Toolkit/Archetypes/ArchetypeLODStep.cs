// Copyright (c) coherence ApS.
// See the license file in the package root for more information.

namespace Coherence.Toolkit
{
    using System;
    using UnityEngine;

    [Serializable]
    internal class ArchetypeLODStep
    {
        public float Distance => distance;
        [SerializeField] private float distance;

        public ArchetypeLODStep()
        {
            distance = float.MaxValue;
        }

        public void SetDistance(float newDistance)
        {
            distance = newDistance;
        }

        public ArchetypeLODStep(ArchetypeLODStep other)
        {
            distance = other.distance;
        }
    }
}
