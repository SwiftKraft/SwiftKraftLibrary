// Copyright (c) coherence ApS.
// See the license file in the package root for more information.

namespace Coherence.Toolkit
{
    using Log;

    public interface IInputBufferDebug
    {
        void DebugPrint(string operationName, bool includeInputs);
    }
}
