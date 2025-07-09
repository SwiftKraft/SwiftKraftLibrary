// Copyright (c) coherence ApS.
// See the license file in the package root for more information.

namespace Coherence.Editor.Toolkit
{
    using Coherence.Toolkit;
    using Coherence.Toolkit.Bindings;

    internal struct ValueSyncedCallbackInfo
    {
        public Binding Binding;
        public string Error;
        public string Callback => attribute?.Callback;
        public bool SuppressNotBoundError => attribute?.SuppressNotBoundError ?? false;
        public bool SuppressParamOrderError => attribute?.SuppressParamOrderError ?? false;

        private readonly OnValueSyncedAttribute attribute;

        public ValueSyncedCallbackInfo(OnValueSyncedAttribute attribute) : this()
        {
            this.attribute = attribute;
        }
    }
}
