// Copyright (c) coherence ApS.
// See the license file in the project root for more information.

namespace Coherence.Editor
{
    using System.Collections.Generic;
    using UnityEngine;

    public struct EntryInfo
    {
        public int Variables;
        public int Methods;
        public int Bits;
        public int ComponentActions;
        public int NetworkComponents;
        public int InvalidBindings;
        public int AssetsWithInvalidBindings;
        public int MissingAssets;
        public HashSet<Component> UniqueComponentsWithBindings;
        public int BindingsWithInputAuthPrediction;
    }
}
