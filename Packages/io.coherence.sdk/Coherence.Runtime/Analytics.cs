// Copyright (c) coherence ApS.
// See the license file in the package root for more information.

namespace Coherence.Runtime
{
    using Cloud;
    using Prefs;
    using System;
    using System.Threading.Tasks;
    using Common;
    using Log;
    using Logger = Log.Logger;

    public class AnalyticsClient
    {
        private readonly Logger logger = Log.GetLogger<AnalyticsClient>();
        private string cachedAnalyticsId;
        private readonly IRequestFactory requestFactory;
        private readonly IRuntimeSettings runtimeSettings;
        private readonly IPlayerAccountProvider playerAccountProvider;

        internal AnalyticsClient() { } // for test doubles

        internal AnalyticsClient(IPlayerAccountProvider playerAccountProvider, IRuntimeSettings runtimeSettings, IRequestFactory requestFactory)
        {
            this.requestFactory = requestFactory;
            this.runtimeSettings = runtimeSettings;
            this.playerAccountProvider = playerAccountProvider;

            if (string.IsNullOrEmpty(runtimeSettings.ProjectID))
            {
                return;
            }

            requestFactory.OnWebSocketConnect += OnConnect;
        }

        private async void OnConnect()
        {
            while (!playerAccountProvider.IsReady)
            {
                await Task.Yield();
            }

            var request = new AnalyticsRequest
            {
                TimestampMs = new DateTimeOffset(DateTime.Now).ToUnixTimeMilliseconds(),
                AnalyticsId = GetOrCreateAnalyticsId(),
                EventName = "connection",
                SDKVersion = runtimeSettings.VersionInfo.Sdk,
                EngineVersion = runtimeSettings.VersionInfo.Engine,
                SimSlug = runtimeSettings.SimulatorSlug,
                SchemaId = runtimeSettings.SchemaID,
            };
            var body = Coherence.Utils.CoherenceJson.SerializeObject(request);
            try
            {
                await requestFactory.SendRequestAsync("/analytics", "POST", body, null, $"{nameof(AnalyticsClient)}.Analytics", string.Empty);
            }
            catch (RequestException requestException) when (requestException.ErrorCode == ErrorCode.TooManyRequests)
            {
                // Ignore throttling errors.
            }
            catch(Exception exception)
            {
                logger.Warning(Warning.RuntimeAnalyticsFailedToWrite, ("exception", exception));
            }

            string GetOrCreateAnalyticsId()
            {
                if (cachedAnalyticsId is not null)
                {
                    return cachedAnalyticsId;
                }

                var prefsKey = GetAnalyticsIdPrefsKey();
                cachedAnalyticsId = Prefs.GetString(prefsKey);
                if (!string.IsNullOrEmpty(cachedAnalyticsId))
                {
                    return cachedAnalyticsId;
                }

                cachedAnalyticsId = Guid.NewGuid().ToString();
                Prefs.SetString(prefsKey, cachedAnalyticsId);
                return cachedAnalyticsId;

                string GetAnalyticsIdPrefsKey()
                {
                    var uniqueId = playerAccountProvider.CloudUniqueId;
                    return Utils.PrefsUtils.Format(PrefsKeys.AnalyticsId, runtimeSettings.ProjectID, uniqueId);
                }
            }
        }
    }
}
