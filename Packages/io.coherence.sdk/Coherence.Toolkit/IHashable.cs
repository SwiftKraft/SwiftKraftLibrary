// Copyright (c) coherence ApS.
// See the license file in the package root for more information.

namespace Coherence.Toolkit
{
    using UnityEngine;

    public interface IHashable
    {
        Hash128 ComputeHash();
    }
}
