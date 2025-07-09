// Copyright (c) coherence ApS.
// See the license file in the package root for more information.

namespace Coherence.Editor
{
    internal class WarningSource
    {
        public bool Active;
        public string Message;
        public WarningToken Token => new WarningToken(this);

        public WarningSource(string message)
        {
            Active = true;
            Message = message;
        }

        public void ClearWarning()
        {
            Active = false;
            Message = string.Empty;
        }
    }
}
