// Copyright (c) coherence ApS.
// See the license file in the package root for more information.

namespace Coherence.Brisk
{
    using Models;
    using System;
    using System.Threading;

    public partial class Brisk
    {
        private class KeepAliveTimer
        {
            private static readonly TimeSpan keepAlivePeriod = TimeSpan.FromSeconds(1f);

            private Timer timer;
            private readonly Brisk brisk;

            public KeepAliveTimer(Brisk brisk)
            {
                this.brisk = brisk;
            }

            public void StartKeepAlive()
            {
                // Just going to send a heartbeat. If we wanted, we could use a
                // system based on the last sent packet of any type, but that would
                // require more thread locks, so for the sake of simplicity, we're
                // just sending at a constant rate and a tiny amount of data.

                var keepAlive = new KeepAlive();
                var briskWeakRef = new WeakReference<Brisk>(brisk);

                timer = new Timer((state) =>
                {
                    // Brisk has to be a weak reference or it will keep itself
                    // referenced leading to a leak.
                    WeakReference<Brisk> briskRef = (WeakReference<Brisk>)state;
                    if (briskRef.TryGetTarget(out Brisk target))
                    {
                        target.SendOOBMessage(keepAlive, false);
                    }
                }, briskWeakRef, TimeSpan.Zero, keepAlivePeriod);
            }

            public void StopKeepAlive()
            {
                timer?.Dispose();
            }
        }
    }
}
