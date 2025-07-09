// Copyright (c) coherence ApS.
// See the license file in the package root for more information.

#if UNITY_5_3_OR_NEWER
// IMPORTANT: Used by the pure-dotnet client, DON'T REMOVE.
// Any changes to the Unity version of the request should be reflected
// in the HttpClient version.
// TODO: Separate Http client impl. with common options/policy layer (coherence/unity#1764)
#define UNITY
#endif

namespace Coherence.Cloud
{
    using Log;
    using Newtonsoft.Json;
    using Runtime;
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
#if UNITY
    using Constants = Coherence.Constants;
#endif

    /// <summary>
    /// The old cloud-backed key-value store service. Superseded by <see cref="CloudStorage"/>.
    /// <remarks>
    /// To use this service, you must have 'Player Key-Value Store' enabled in Project Settings in your coherence Dashboard.
    /// </remarks>
    /// </summary>
    public partial class KvStoreClient : IUpdatable, IDisposable
    {
        private bool dirty;
        private bool isSyncing;
        private IRequestFactory requestFactory;
        private IAuthClientInternal authClient;

        // Backoff mechanism
        private readonly Stopwatch syncBackoffStopwatch = Stopwatch.StartNew();
        private TimeSpan syncBackoff = Runtime.Constants.minBackoff;

        private readonly Logger logger = Log.GetLogger<KvStoreClient>();
        private readonly List<DataSyncItem> syncPoint = new List<DataSyncItem>(16);
        private readonly Dictionary<string, DataSyncItem> dataItemByKey =
            new Dictionary<string, DataSyncItem>(16);

        private HashSet<string> invalidKeys = new HashSet<string>();

        // For testing purposes
        private readonly bool registerForUpdate;

        public KvStoreClient(RequestFactory requestFactory, AuthClient authClient) : this(requestFactory, (IAuthClientInternal)authClient) { }

        internal KvStoreClient(IRequestFactory requestFactory, IAuthClientInternal authClient, bool registerForUpdate = true)
        {
            this.requestFactory = requestFactory;
            this.authClient = authClient;
            this.registerForUpdate = registerForUpdate;

            this.authClient.OnLogin += SyncWithLogin;
            this.authClient.OnLogout += OnLogout;

            if (registerForUpdate)
            {
                Updater.RegisterForUpdate(this);
            }
        }

        public void Dispose()
        {
            if (registerForUpdate)
            {
                Updater.DeregisterForUpdate(this);
            }
        }

        /// <summary>
        ///     Adds entry to the key-value store.
        /// </summary>
        /// <returns>
        ///     True if entry was added (not necessarily synced yet). False if the <paramref name="key" /> or
        ///     <paramref name="value" /> are invalid.
        /// </returns>
        public bool Set(string key, string value)
        {
            if (!CheckKeyValidity(key) || value == null)
            {
                return false;
            }

            if (Get(key) == value)
            {
                return true;
            }

            dataItemByKey[key] = new DataSyncItem
            {
                Key = key,
                Value = value,
                Operation = DataOperationType.Set,
                Dirty = dirty = true
            };

            return true;
        }

        /// <summary>
        ///     Removes entry from the key-value store.
        /// </summary>
        /// <returns>True if an entry was removed, false otherwise.</returns>
        public bool Unset(string key)
        {
            if (!dataItemByKey.TryGetValue(key, out DataSyncItem dataItem))
            {
                return false;
            }

            dataItem.Operation = DataOperationType.Delete;
            dataItem.Dirty = dirty = true;
            dataItemByKey[key] = dataItem;
            return true;
        }

        /// <summary>
        ///     Returns a value for a given key.
        /// </summary>
        /// <param name="key"></param>
        /// <param name="defaultValue">Default value to be returned if the entry for a given key was not found.</param>
        /// <returns>Value for a given <paramref name="key" /> or <paramref name="defaultValue" /> if entry was not found.</returns>
        public string Get(string key, string defaultValue = null)
        {
            return dataItemByKey.TryGetValue(key, out DataSyncItem dataItem) ? dataItem.Value : defaultValue;
        }

        /// <summary>
        ///     Performs a key-value store sync with backend. Only one sync operation can be in flight at any given moment.
        /// </summary>
        public async void Update()
        {
            if (!dirty || isSyncing || !authClient.LoggedIn)
            {
                return;
            }

            if (syncBackoffStopwatch.Elapsed < syncBackoff)
            {
                return;
            }
            syncBackoffStopwatch.Restart();

            dirty = false;
            isSyncing = true;

            foreach (KeyValuePair<string, DataSyncItem> kv in dataItemByKey)
            {
                if (kv.Value.Dirty)
                {
                    syncPoint.Add(kv.Value);
                }
            }

            if (syncPoint.Count <= 0)
            {
                isSyncing = false;
                return;
            }

            for (var i = 0; i < syncPoint.Count; i++)
            {
                DataSyncItem dataSyncItem = syncPoint[i];
                dataSyncItem.Dirty = false;
                dataItemByKey[dataSyncItem.Key] = dataSyncItem;
            }

            DataSync payload = new DataSync
            {
                Data = syncPoint
            };

            string body = Utils.CoherenceJson.SerializeObject(payload);
            try
            {
                var text = await requestFactory.SendRequestAsync("/kv", "POST", body, null, $"{nameof(KvStoreClient)}.{nameof(Update)}", authClient.SessionToken);
                syncBackoff = Runtime.Constants.minBackoff;
            }
            catch (RequestException ex)
            {
                if (ex.ErrorCode != ErrorCode.TooManyRequests)
                {
                    if (ex.StatusCode == 423)
                    {
                        logger.Error(Error.RuntimeCloudGameServicesKVNotEnabled);
                        syncBackoff = TimeSpan.FromMinutes(5);
                        return;
                    }

                    logger.Warning(Warning.RuntimeCloudGameServicesFailedKVSync, ("Status code", ex.StatusCode));
                }

                // Mark objects from last sync point as dirty again
                for (int i = 0; i < syncPoint.Count; i++)
                {
                    if (dataItemByKey.TryGetValue(syncPoint[i].Key, out DataSyncItem syncItem))
                    {
                        syncItem.Dirty = true;
                        dataItemByKey[syncItem.Key] = syncItem;
                    }
                }

                // Double the backoff on failure and cap at Config.MaxBackoff
                syncBackoff = TimeSpan.FromSeconds(Math.Min(syncBackoff.TotalSeconds * 2,
                    Runtime.Constants.maxBackoff.TotalSeconds));
                dirty = true;
            }

            syncPoint.Clear();
            isSyncing = false;
        }

        private void Clear()
        {
            dataItemByKey.Clear();
            syncPoint.Clear();
        }

        private void OnLogin(IEnumerable<KvPair> kv)
        {
            Clear();

            if (kv == null)
            {
                return;
            }

            foreach (KvPair kvPair in kv)
            {
                dataItemByKey[kvPair.Key] = new DataSyncItem
                {
                    Key = kvPair.Key,
                    Value = kvPair.Value,
                    Operation = DataOperationType.Set,
                    Dirty = false
                };
            }
        }

        private void OnLogout()
        {
            Clear();
        }

        private bool CheckKeyValidity(string key)
        {
            if (string.IsNullOrEmpty(key))
            {
                return false;
            }

            foreach (char c in key)
            {
                if ((c >= 'a' && c <= 'z') ||
                    (c >= 'A' && c <= 'Z') ||
                    (c >= '0' && c <= '9') ||
                    c == '_' || c == '-')
                {
                    continue;
                }

                if (invalidKeys.Add(key))
                {
                    logger.Warning(Warning.RuntimeCloudGameServicesInvalidKey, ("key", key));
                }

                return false;
            }

            return true;
        }

        private void SyncWithLogin(LoginResponse loginResponse)
        {
            if (loginResponse.KvStoreState != null)
            {
                foreach (var kv in loginResponse.KvStoreState)
                {
                    Set(kv.Key, kv.Value);
                }
            }
        }
    }
}
