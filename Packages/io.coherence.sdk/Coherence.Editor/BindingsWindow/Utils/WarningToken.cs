// Copyright (c) coherence ApS.
// See the license file in the package root for more information.

namespace Coherence.Editor
{
    internal class WarningToken
    {
        public string Message;
        public WarningSource Source;
        public bool Active => Source.Active;

        public WarningToken(WarningSource source)
        {
            Source = source;
            Message = source.Message;
        }

        public void ClearWarning()
        {
            Message = string.Empty;
            Source.ClearWarning();
        }
    }
}
