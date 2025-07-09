// Copyright (c) coherence ApS.
// See the license file in the package root for more information.

namespace Coherence.Core
{
    using System;
    using Entities;
    using ProtocolDef;

    public interface INativeCoreComponentUpdater
    {
        void UpdateComponent<T>(InteropEntity entity, UInt32 componentId, T component, Int32 dataSize, UInt32 fieldMask,
            UInt32 stoppedMask, Int64[] frames) where T : unmanaged;
    }

    public interface INativeCoreCommandSender
    {
        bool SendCommand<T>(InteropEntity id, MessageTarget target, UInt32 commandType, T message, Int32 dataSize) where T : unmanaged;
    }

    public interface INativeCoreInputSender
    {
        void SendInput<T>(InteropEntity id, Int64 frame, UInt32 inputType, T message, Int32 dataSize) where T : unmanaged;
    }

    public interface IDataInteropHandler
    {
        public unsafe ICoherenceComponentData GetComponent(UInt32 type, IntPtr data, Int32 dataSize, InteropAbsoluteSimulationFrame* simFrames, Int32 simFramesCount);
        public void UpdateComponent(INativeCoreComponentUpdater updater, InteropEntity entity, ICoherenceComponentData component);

        public IEntityCommand GetCommand(UInt32 type, IntPtr data, Int32 dataSize);
        public bool SendCommand(INativeCoreCommandSender sender, InteropEntity entity, MessageTarget target, IEntityCommand command);

        public IEntityInput GetInput(UInt32 type, IntPtr data, Int32 dataSize);
        public void SendInput(INativeCoreInputSender sender, InteropEntity entity, Int64 frame, IEntityInput input);
    }
}
