// Copyright (c) coherence ApS.
// See the license file in the package root for more information.

namespace Coherence.Tests
{
    using System;
    using UnityEngine;
    using UnityEngine.Networking;

    /// <summary>
    /// Represents a request to a documentation URL.
    /// Requires calling <see cref="Update"/> in order to start and progress the request.
    /// </summary>
    internal class DocumentationRequest
    {
        public enum Result
        {
            Failure,
            UrlMismatch,
            Success,
        }

        private const int MaxBadResponseRetries = 3;
        private const int MaxUrlMismatchRetries = 5;

        /// <summary>
        /// Original URL of the request.
        /// </summary>
        public string Url { get; }

        /// <summary>
        /// Result of the request.
        /// </summary>
        public Result OperationResult { get; private set; }

        /// <summary>
        /// True if the request is done (either succeeded or failed definitively).
        /// </summary>
        public bool Done { get; private set; }

        public Func<UnityWebRequest, bool> ResponseValidator { get; set; } = IsPageOk;

        private UnityWebRequest request;
        private UnityWebRequestAsyncOperation requestOperation;

        private readonly bool expectNotFound;

        private int badStatusResults;
        private int urlMismatchResults;
        private DateTime? nextRetryTime;

        public DocumentationRequest(string url, bool expectNotFound = false)
        {
            this.expectNotFound = expectNotFound;
            Url = url;
        }

        public bool CheckDone()
        {
            Update();
            return Done;
        }

        public void Update()
        {
            if (Done)
            {
                return;
            }

            if (!CanRetry())
            {
                return;
            }

            if (request == null)
            {
                Start();
                return;
            }

            if (!requestOperation.isDone)
            {
                return;
            }

            if (!ValidateResponse(request))
            {
                badStatusResults++;
                Retry();
                return;
            }

            if (!ResponseValidator(request) && !expectNotFound)
            {
                // If the URL is different, we've hit the GitBook bug where we're getting
                // redirected based on the path from a different request. Simply retry.
                if (request.url != Url)
                {
                    urlMismatchResults++;
                    Retry();
                    return;
                }

                badStatusResults++;
                Retry();
                return;
            }

            SetDone(Result.Success);
        }

        private void SetDone(Result result)
        {
            request?.Dispose();
            OperationResult = result;
            Done = true;
        }

        private void Start()
        {
            if (request != null)
            {
                request.Dispose();
            }

            request = CreateBaseRequest(Url);
            requestOperation = request.SendWebRequest();
        }

        private bool CanRetry()
        {
            if (!nextRetryTime.HasValue || DateTime.UtcNow >= nextRetryTime)
            {
                nextRetryTime = null;
                return true;
            }

            return false;
        }

        private void Retry()
        {
            nextRetryTime = DateTime.UtcNow + TimeSpan.FromSeconds(Math.Pow(2, badStatusResults));

            if (badStatusResults > MaxBadResponseRetries)
            {
                Debug.LogWarning($"'{request.url}' failed with too many bad responses: {request.error}");
                SetDone(Result.Failure);
                return;
            }

            if (urlMismatchResults > MaxUrlMismatchRetries)
            {
                Debug.LogWarning($"'{request.url}' failed with too many URL mismatches");
                SetDone(Result.UrlMismatch);
                return;
            }

            Start();
        }

        private static UnityWebRequest CreateBaseRequest(string url)
        {
            var request = UnityWebRequest.Get(url);
            request.SetRequestHeader("User-Agent", "coherence/1.0");
            request.SetRequestHeader("Content-Type", "text/html; charset=utf-8");
            return request;
        }

        private static bool ValidateResponse(UnityWebRequest request)
        {
            if (request.result != UnityWebRequest.Result.Success)
            {
                Debug.LogWarning($"'{request.url}' failed with '{request.result}': {request.error}");
                return false;
            }

            if (request.responseCode != 200)
            {
                Debug.LogWarning($"'{request.url}' failed with '{request.responseCode}': {request.error}");
                return false;
            }

            return true;
        }

        private static bool IsPageOk(UnityWebRequest request)
        {
            if (request.downloadHandler == null)
            {
                return false;
            }

            var text = request.downloadHandler.text;

            // In case of false positive (i.e., GitBook changes layout and breaks this),
            // check the HTML body and update the test accordingly, if at all possible.
            return !string.IsNullOrEmpty(text) && request.downloadHandler.text.Contains("<meta property=\"og:title\"");
        }
    }
}
