// Copyright (c) coherence ApS.
// See the license file in the package root for more information.

namespace Coherence.Plugins.NativeLauncher
{
    using System;

    public delegate void StreamDataReceivedEventHandler(object sender, StreamDataReceivedEvent e);

    public class StreamDataReceivedEvent : EventArgs
    {
        private readonly string data;

        internal StreamDataReceivedEvent(string data) => this.data = data;

        public string Data => data;
    }
}
