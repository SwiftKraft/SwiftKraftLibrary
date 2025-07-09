// Copyright (c) coherence ApS.
// See the license file in the package root for more information.

namespace Coherence.Common
{
    using System;

    public class SystemDateTimeProvider : IDateTimeProvider
    {
        public static readonly SystemDateTimeProvider Instance = new SystemDateTimeProvider();

        public DateTime UtcNow => DateTime.UtcNow;
    }
}
