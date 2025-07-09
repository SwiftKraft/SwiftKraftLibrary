namespace Coherence.Core
{
    using Log;
    using System;
    using System.Net;
    using System.Threading;
    using System.Threading.Tasks;
    using Common;

    public interface IDomainNameResolver
    {
        void Resolve(string hostname, CancellationToken cancellationToken, Logger logger, Action<IPAddress> onSuccess, Action onFailure);
    }

    internal class DomainNameResolver : IDomainNameResolver
    {
        public void Resolve(string hostname, CancellationToken cancellationToken, Logger logger, Action<IPAddress> onSuccess, Action onFailure)
        {
            if (IPAddress.TryParse(hostname, out IPAddress ipAddress))
            {
                logger.Debug("Recognised given hostname as an IP address, no DNS resolving will take place.", ("hostname", hostname), ("address", ipAddress));

                onSuccess?.Invoke(ipAddress);
                return;
            }

            GetHostEntryAsync(hostname, cancellationToken).ContinueWith((Task<Task> result) =>
            {
                // check if while resolving the DNS the Disconnect was maybe called
                // in that case just don't connected
                if (cancellationToken.IsCancellationRequested)
                {
                    return;
                }

                if (result.IsFaulted)
                {
                    logger.Error(Error.CoreHostNameResolveResult,
                        ("hostname", hostname),
                        ("exception", result.Exception.ToString()));
                    onFailure?.Invoke();
                    return;
                }

                var innerTask = result.Result as Task<IPHostEntry>;

                if (innerTask == null ||  innerTask.IsFaulted)
                {
                    logger.Error(Error.CoreHostNameResolveInnerTask,
                        ("hostname", hostname),
                        ("exception", innerTask?.Exception.ToString()));
                    onFailure?.Invoke();
                    return;
                }

                if (!TryGetFirstIPv4Address(innerTask.Result.AddressList, out IPAddress address))
                {
                    logger.Error(Error.CoreHostNameResolveIPV4,
                        ("hostname", hostname));
                    onFailure?.Invoke();
                    return;
                }

                logger.Debug("Resolved hostname", ("hostname", hostname), ("address", address));

                onSuccess?.Invoke(address);

            }, cancellationToken, TaskContinuationOptions.None, TaskUtils.Scheduler);
        }

        private Task<Task> GetHostEntryAsync(string hostname, CancellationToken cancellationToken)
        {
            var completionSource = new TaskCompletionSource<Task>();
            var dnsTask = Dns.GetHostEntryAsync(hostname);
            dnsTask.Then(task => completionSource.TrySetResult(task), cancellationToken);
            cancellationToken.Register(() => completionSource.TrySetCanceled(), useSynchronizationContext: true);
            return completionSource.Task;
        }

        private bool TryGetFirstIPv4Address(IPAddress[] addressList, out IPAddress firstIPv4Address)
        {
            foreach (var address in addressList)
            {
                if (address.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)
                {
                    firstIPv4Address = address;
                    return true;
                }
            }

            firstIPv4Address = null;
            return false;
        }
    }
}
