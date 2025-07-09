// Copyright (c) coherence ApS.
// See the license file in the package root for more information.

namespace Coherence.Editor.Features
{
    internal sealed class MultiRoomSimulator : IFeature
    {
        public bool IsEnabled { get; }
        public bool IsUserConfigurable { get; }

        public MultiRoomSimulator()
        {
            IsEnabled = IsUserConfigurable =
#if COHERENCE_ENABLE_MULTI_ROOM_SIMULATOR
                true;
#else
                false;
#endif
        }
    }
}
