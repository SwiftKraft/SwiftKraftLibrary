// Copyright (c) coherence ApS.
// See the license file in the package root for more information.

#if UNITY_5_3_OR_NEWER
#define UNITY
#endif

namespace Coherence.Cloud
{
    using Common;

    /// <summary>
    /// Specifies options that can be provided when
    /// <see cref="CoherenceCloud.LoginAsGuest(LoginAsGuestOptions, System.Threading.CancellationToken)">
    /// logging in to coherence Cloud as a guest </see>.
    /// </summary>
    /// <remarks>
    /// Providing a custom <see cref="Cloud.CloudUniqueId"/> makes it possible to log in as multiple different guest users on the same device.
    /// This might be useful for local multiplayer games, allowing each player to log into their own guest player account.
    /// </remarks>
    public readonly struct LoginAsGuestOptions
    {
        /// <summary>
        /// Options that are used by default when <see cref="CoherenceCloud.LoginAsGuest(System.Threading.CancellationToken)">logging in as a guest</see>.
        /// </summary>
        public static readonly LoginAsGuestOptions Default = new(PlayerAccount.DefaultCloudUniqueId);

        private readonly CloudUniqueId cloudUniqueId;
        private readonly string projectId;

        /// <summary>
        /// A locally unique identifier for the guest player account to create.
        /// </summary>
        public CloudUniqueId CloudUniqueId => cloudUniqueId != CloudUniqueId.None ? cloudUniqueId : PlayerAccount.DefaultCloudUniqueId;

        internal string ProjectId => projectId ?? "";

        /// <summary>
        /// Initializes a new instance of the <see cref="LoginAsGuestOptions"/> struct.
        /// </summary>
        /// <param name="cloudUniqueId">
        /// A locally unique identifier for the guest player account to create.
        /// </param>
        /// <remarks>
        /// Providing a custom <see cref="Cloud.CloudUniqueId"/> makes it possible to log in as multiple different guest users on the same device.
        /// This might be useful for local multiplayer games, allowing each player to log into their own guest player account.
        /// </remarks>>
        public LoginAsGuestOptions(CloudUniqueId cloudUniqueId) : this(cloudUniqueId, "") { }

        /// <summary>
        /// Initializes a new instance of the <see cref="LoginAsGuestOptions"/> struct.
        /// </summary>
        /// <param name="cloudUniqueId">
        /// A locally unique identifier for the guest player account to create.
        /// <para>
        /// If a value of <see cref="CloudUniqueId.None"/> is passed, then the default unique id will be used.
        /// </para>
        /// <para>
        /// Providing a custom <see cref="cloudUniqueId"/> makes it possible to log in as multiple different guest users on the same device.
        /// This could be useful for local multiplayer.
        /// </para>
        /// </param>
        /// <param name="projectId">
        /// The <see cref="IRuntimeSettings.ProjectID">identifier of the project</see> that the guest player account should be associated with.
        /// <para>
        /// If a null or empty string is passed, then the id of the currently active project will be used.
        /// </para>
        /// </param>
        internal LoginAsGuestOptions(CloudUniqueId cloudUniqueId, string projectId)
        {
            this.cloudUniqueId = cloudUniqueId;
            this.projectId = projectId;
        }

        public override string ToString() => $"LoginAsGuestOptions({nameof(CloudUniqueId)}:\"{cloudUniqueId}\", {nameof(ProjectId)}:\"{projectId}\")";
    }
}
