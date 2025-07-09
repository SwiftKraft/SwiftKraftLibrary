// Copyright (c) coherence ApS.
// See the license file in the package root for more information.

namespace Coherence.Editor
{
    using Log;
    using System;

    internal class Endpoints
    {
        private static readonly Lazy<Logger> Logger = new(() => Log.GetLogger<Endpoints>());
        public const int localWebPort = 3000;
        public const int localPortalPort = 4023;
        public const int localPlayPort = 4024;

        public const string apiVersion = "v1";
        public const string portalUrl = "https://coherence.io/portal";
        public const string loginDomain = "coherence.io";
        public const string apiDomain = "api.prod.coherence.io";

        // Endpoints
        // All endpoints should be listed and used from this file.
        // This will help us visualize which endpoints are used as the SDK evolves.
        // These are passed through Path() in PortalRequest by default to have orgid and projectid filled in.
        public const string sdkConnectPath = "/sdk-connections";
        public const string organizationsPath = "/me/orgs/sdk";
        public const string organizationSubscriptionPath = "/orgs/[orgid]/subscription";

        public const string releasesPath = "/releases/{0}?count={1}";
        public const string releasePath = "/releases/{0}/{1}";

        public const string deployUrlPath = "/deploy/replicator";
        public const string restartUrlPath = "/deploy/replicator/restart";

        public const string schemasPath = "/projects/[projectid]/schemas";
        public const string simUploadUrlPath = "/projects/[projectid]/simulators/uploadurl?size={0}&slug={1}&schema_id={2}&rs_ver={3}";
        public const string gameUploadUrlPath = "/projects/[projectid]/game/{0}/upload?size={1}";
        public const string webglUploadUrlPath = "/projects/[projectid]/game/webgl/upload?filename={0}&size={1}&context={2}";
        public const string registerBuildUrlPath = "/projects/[projectid]/game";
        public const string registerSimUrlPath = "/projects/[projectid]/simulators";
        public const string regionsPath = "/projects/[projectid]/regions";

        private const string ProjectIDToken = "[projectId]";
        private const string OrganizationIDToken = "[orgid]";

        public static string Play
        {
            get
            {
                string domain = apiDomain;
                if (ProjectSettings.instance.UseCustomEndpoints && !string.IsNullOrEmpty(ProjectSettings.instance.CustomAPIDomain))
                {
                    domain = ProjectSettings.instance.CustomAPIDomain;
                }
                return Build(domain, $"/{apiVersion}/play", localPlayPort);
            }
        }

        public static string Portal
        {
            get
            {
                string domain = apiDomain;
                if (ProjectSettings.instance.UseCustomEndpoints && !string.IsNullOrEmpty(ProjectSettings.instance.CustomAPIDomain))
                {
                    domain = ProjectSettings.instance.CustomAPIDomain;
                }
                return Build(domain, $"/{apiVersion}/portal", localPortalPort);
            }
        }

        public static string LoginBaseURL
        {
            get
            {
                var domain = loginDomain;
                if (ProjectSettings.instance.UseCustomEndpoints)
                {
                    domain = LoadBalancerToDomain(ProjectSettings.instance.CustomAPIDomain);
                }
                return Build(domain, string.Empty, localWebPort);
            }
        }

        // Due to a specific case only with the portal's "master" or main stage environment,
        // we need to convert the load balancer URL to the site's URL for the login
        // domain.
        private static string LoadBalancerToDomain(string url)
        {
            return url.Replace("api.stage.", "stage.");
        }

        private static string Build(string domain, string path, int port)
        {
            return domain == "localhost" ?
                $"http://localhost:{port}{path}" :
                $"https://{domain}{path}";
        }

        // All calls to project-specific resources should go through this function or TryGet.
        // `path` should contain the leading '/', e.g. '/schemas';
        //  Ideally, use a const defined above.
        public static string Get(string path)
        {
            var orgid = ProjectSettings.instance.RuntimeSettings.OrganizationID;
            var projectid = ProjectSettings.instance.RuntimeSettings.ProjectID;
            if (!TryGet(path, orgid, projectid, out var result, out var error))
            {
                Logger.Value.Error(Error.EditorEndpointGet, error);
            }

            return result;
        }

        // All calls to project-specific resources should go through this function or Get.
        // `path` should contain the leading '/', e.g. '/schemas';
        //  Ideally, use a const defined above.
        internal static bool TryGet(string path, string organizationID, string projectID, out string result, out string error)
        {
            result = path;

            if (path.Contains(OrganizationIDToken, StringComparison.InvariantCultureIgnoreCase))
            {
                if (string.IsNullOrEmpty(organizationID))
                {
                    error = $"Attempt to access an Organization-specific resource ({path}) without an organization ID. Please log into coherence Cloud and select an organization.";
                    result = null;
                    return false;
                }

                result = result.Replace(OrganizationIDToken, organizationID, StringComparison.OrdinalIgnoreCase);
            }

            if (path.Contains(ProjectIDToken, StringComparison.InvariantCultureIgnoreCase))
            {
                if (string.IsNullOrEmpty(projectID))
                {
                    error = $"Attempt to access a Project-specific resource ({path}) without a project ID. Please log into coherence Cloud and select a project.";
                    result = null;
                    return false;
                }

                result = result.Replace(ProjectIDToken, projectID, StringComparison.OrdinalIgnoreCase);
            }

            error = null;
            return true;
        }
    }
}
