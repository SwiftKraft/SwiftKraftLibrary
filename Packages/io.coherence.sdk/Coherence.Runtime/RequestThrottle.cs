// Copyright (c) coherence ApS.
// See the license file in the package root for more information.

namespace Coherence.Runtime
{
    using Log;
    using System;
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Tasks;
    using Common;
    using Utils;

    internal class RequestThrottle
    {
        public TimeSpan RequestInterval;

        private readonly Logger logger = Log.GetLogger<RequestThrottle>();
        private readonly Dictionary<(string basePath, string method), DateTime> requestTimeByPath = new Dictionary<(string basePath, string method), DateTime>();
        private readonly IDateTimeProvider dateTimeProvider;

        public RequestThrottle(TimeSpan requestInterval)
            : this(requestInterval, SystemDateTimeProvider.Instance) {}

        internal RequestThrottle(TimeSpan requestInterval, IDateTimeProvider dateTimeProvider)
        {
            this.RequestInterval = requestInterval;
            this.dateTimeProvider = dateTimeProvider;
        }

        public TimeSpan RequestCooldown(string basePath, string method)
        {
            var now = dateTimeProvider.UtcNow;
            if (requestTimeByPath.TryGetValue((basePath, method), out var requestTime) && (requestTime + RequestInterval > now))
            {
                return (requestTime + RequestInterval) - now;
            }

            return TimeSpan.Zero;
        }

        public bool HandleTooManyRequests(string basePath, string method, string requestName)
        {
            var now = dateTimeProvider.UtcNow;
            var cooldown = RequestCooldown(basePath, method);
            if (cooldown > TimeSpan.Zero)
            {
                return true;
            }

            requestTimeByPath[(basePath, method)] = now;
            return false;
        }

        public virtual async Task WaitForCooldown(string basePath, string method, CancellationToken cancellationToken)
        {
            while(true)
            {
                var cooldown = RequestCooldown(basePath, method);
                if (cooldown <= TimeSpan.Zero)
                {
                    break;
                }

                await Wait.For(TimeSpan.FromSeconds(cooldown.TotalSeconds), cancellationToken);
            }

            cancellationToken.ThrowIfCancellationRequested();
        }
    }
}
