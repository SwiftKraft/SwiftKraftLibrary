// Copyright (c) coherence ApS.
// See the license file in the package root for more information.
namespace Coherence.Cloud
{
    using Runtime;

    /// <summary>
    /// Exposes additional members on top of the ones already offered by <see cref="IRequestFactory"/>
    /// for internal-only use.
    /// </summary>
    /// <inheritdoc/>
    internal interface IRequestFactoryInternal : IRequestFactory
    {
        RequestThrottle Throttle { get; }
    }
}
