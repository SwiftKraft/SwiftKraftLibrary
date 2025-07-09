// Copyright (c) coherence ApS.
// See the license file in the package root for more information.

#if UNITY_5_3_OR_NEWER
#define UNITY
#endif

namespace Coherence.Cloud
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.Diagnostics.CodeAnalysis;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using Log;
    using Runtime;
    using Utils;
#if UNITY
    using UnityEngine;
#endif

    /// <summary>
    /// Reference to a method that should be called when the first login operation to coherence Cloud starts,
    /// when the <see cref="PlayerAccount.Main">main player account</see> is changed to a different one using <see cref="PlayerAccount.SetAsMain"/>,
    /// and when the main player account logs out from coherence cloud.
    /// </summary>
    /// <param name="mainPlayerAccount">
    /// The new main player account.
    /// <remarks>
    /// At this point <see cref="PlayerAccount.Services"/> is not <see langword="null"/>,
    /// but <see cref="PlayerAccount.IsLoggedIn"/> is still <see langword="false"/>
    /// for a main player account that has just started logging in.
    /// </remarks>
    /// </param>
    public delegate void OnMainPlayerAccountChangedEventHandler([AllowNull] PlayerAccount mainPlayerAccount);

    /// <summary>
    /// Represents a player account that has logged in to coherence Cloud.
    /// </summary>
    public sealed class PlayerAccount : IDisposable, IEquatable<PlayerAccount>
    {
        [EditorBrowsable(EditorBrowsableState.Never)]
        [Obsolete("'null' is now being used to represent the lack of a player account instead.")]
        [Deprecated("04/2024", 1, 6, 0, Reason = "'null' is now being used to represent the lack of a PlayerAccount instead.")]
        public static readonly PlayerAccount None = null;

        /// <summary>
        /// We always use the same cloud unique id by default when <see cref="CoherenceCloud.LoginAsGuest(CancellationToken)"/> is called,
        /// so that only one guest player account gets created even if the method is called multiple times.
        /// </summary>
        internal static readonly CloudUniqueId DefaultCloudUniqueId = new("DefaultGuest");

        /// <summary>
        /// Event that is raised when the first player account starts logging in to coherence Cloud,
        /// when the main player account is changed to a different one using <see cref="SetAsMain"/>,
        /// and when the main player account logs out from coherence cloud.
        /// </summary>
        public static event OnMainPlayerAccountChangedEventHandler OnMainChanged;

        /// <summary>
        /// Event that is raised whenever the <see cref="PlayerAccount.Main">main player account</see> changes to a different one.
        /// <remarks>
        /// The event is raised when the first player account that started logging in to coherence Cloud successfully completes
        /// their login process, when a different player account is set as the main player account using <see cref="SetAsMain"/>,
        /// and when the player account that was previously the main player account has been logged out from coherence cloud.
        /// </remarks>
        /// </summary>
        /// <seealso cref="GetMainAsync(CancellationToken)"/>
        public static event Action<PlayerAccount> OnMainLoggedIn;

        private static PlayerAccount main;
        private static PlayerAccount[] all = Array.Empty<PlayerAccount>();
        private readonly HashSet<LoginInfo> loginInfos = new(1);
        internal IReadOnlyCollection<LoginInfo> LoginInfos => loginInfos;

        [MaybeNull] private LoginResult loginResult;
        [MaybeNull]
        internal LoginResult LoginResult
        {
            get => loginResult;
            set
            {
                loginResult = value;

                if (value?.Type is not Result.Success)
                {
                    return;
                }

                Id = value.Id;
                SessionToken = value.SessionToken;
                Username = value.Username;
                if (value.response is { } response)
                {
                    DisplayName = response.DisplayName;
                    AvatarUrl = response.AvatarUrl;
                    IsVerified = response.IsVerified;
                    IsNewPlayer = response.IsNewPlayer;
                }
            }
        }

        /// <summary>
        /// Gets a list containing all player accounts that are currently logged in to coherence Cloud locally.
        /// <para>
        /// If no player accounts are logged in, an empty list is returned.
        /// </para>
        /// </summary>
        public static IReadOnlyList<PlayerAccount> All => all;

        /// <summary>
        /// Gets the current main player account that has logged in to coherence Cloud locally.
        /// <remarks>
        /// By default, this returns the first player account that has logged in to coherence Cloud
        /// using any authentication method.
        /// <para>
        /// If no player accounts are logged in, this will be <see langword="null"/>.
        /// </para>
        /// <para>
        /// The main player account can also be manually set to a desired player account using <see cref="SetAsMain"/>.
        /// </para>
        /// </remarks>
        /// </summary>
        /// <seealso cref="GetMainAsync(CancellationToken)"/>
        [MaybeNull]
        public static PlayerAccount Main
        {
            get => main;

            private set
            {
                if (main == value)
                {
                    return;
                }

                main = value;

                OnMainChanged?.Invoke(value);

                if (value is { IsLoggedIn : true })
                {
                    OnMainLoggedIn?.Invoke(value);
                }
            }
        }

        internal bool shouldDisposeCloudService;
        private CloudService services;
        private bool shouldReleaseGuid;
        internal string projectId;
        private bool isDisposed;
        private CloudUniqueId cloudUniqueId;
        private State state = State.LoggedOut;

        /// <summary>
        /// The globally unique identifier for the player account.
        /// </summary>
        /// <remarks>
        /// <para>
        /// This id is generated on the coherence Cloud backend when the user logs in to coherence Cloud for the first time.
        /// </para>
        /// <para>
        /// The id will always remains the same, regardless of the identities are linked to it and unlinked
        /// from it after its creation.
        /// </para>
        /// </remarks>
        public PlayerAccountId Id { get; private set; } = PlayerAccountId.None;

        /// <summary>
        /// Guest identifier for the player account if they have <see cref="CoherenceCloud.LoginAsGuest(CancellationToken)">logged in as a guest</see>.
        /// </summary>
        /// <remarks>
        /// A new <see cref="GuestId"/> is generated automatically the first time when logging in as a guest to a particular project,
        /// and then cached and reused when logging in as a guest again to the same project using the same device.
        /// </remarks>
        public GuestId? GuestId { get; private set; }

        /// <summary>
        /// A locally unique identifier associated with the player account.
        /// <remarks>
        /// <para>
        /// A custom Cloud Unique Id can be provided via CoherenceBridge's Editor when 'Player Account' is set to 'Login As Guest',
        /// or via the <see cref="LoginAsGuestOptions"/> when calling
        /// <see cref="CoherenceCloud.LoginAsGuest(LoginAsGuestOptions, System.Threading.CancellationToken)"/>.
        /// If no custom Cloud Unique Id is provided, then one is generated automatically, and cached locally on the device.
        /// </para>
        /// <para>
        /// Distinct Cloud Unique Ids can be used to create and log into multiple different guest player accounts on the same device.
        /// This might be useful for local multiplayer games, allowing each player to log into their own guest player account.
        /// </para>
        /// </remarks>
        /// </summary>
        internal CloudUniqueId CloudUniqueId
        {
            get => cloudUniqueId;

            set
            {
                if (shouldReleaseGuid)
                {
                    CloudUniqueIdPool.Release(projectId, cloudUniqueId);
                }

                if (value == CloudUniqueId.None && !string.IsNullOrEmpty(projectId))
                {
                    cloudUniqueId = CloudUniqueIdPool.Get(projectId);
                    shouldReleaseGuid = true;
                }
                else
                {
                    cloudUniqueId = value;
                    shouldReleaseGuid = false;
                }
            }
        }

        /// <summary>
        /// Username associated with the player account, if it has been given one; otherwise, an empty string.
        /// </summary>
        /// <seealso cref="CoherenceCloud.LoginWithPassword"/>
        /// <seealso cref="SetUsername"/>
        public string Username { get; private set; } = "";

        /// <summary>
        /// Gets a value indicating whether this <see cref="PlayerAccount"/> is a guest account.
        /// </summary>
        /// <seealso cref="CoherenceCloud.LoginAsGuest(CancellationToken)"/>
        public bool IsGuest { get; private set; }

        /// <summary>
        /// Token uniquely identifying the player account once it has successfully logged-in.
        /// <para>
        /// The token can be stored on the user's device locally, and later used to
        /// <see cref="CoherenceCloud.LoginWithSessionToken(Coherence.Cloud.SessionToken, CancellationToken)">log in</see> to coherence Cloud again using the same credentials,
        /// without the user needing to provide them again.
        /// </para>
        /// </summary>
        public SessionToken SessionToken { get; private set; }

        /// <summary>
        /// The public display name of the player account.
        /// </summary>
        public string DisplayName { get; private set; } = "";

        /// <summary>
        /// The URL of the public avatar image for the player account.
        /// </summary>
        public string AvatarUrl { get; private set; } = "";

        /// <summary>
        /// Is this a newly created player account?
        /// </summary>
        public bool IsNewPlayer { get; private set; }

        /// <summary>
        /// Indicates whether the player account is verified.
        /// </summary>
        /// <remarks>
        /// Player accounts that have been authenticated using any of the following providers
        /// are considered to be human (as opposed to a potential bot) and are marked as verified:
        /// <list type="bullet">
        /// <item> <description>
        /// <see cref="CoherenceCloud.LoginWithSteam">Steam</see>
        /// </description> </item>
        /// <item> <description>
        /// <see cref="CoherenceCloud.LoginWithEpicGames">Epic Games</see>
        /// </description> </item>
        /// <item> <description>
        /// <see cref="CoherenceCloud.LoginWithPlayStation">PlayStation Network</see>
        /// </description> </item>
        /// <item> <description>
        /// <see cref="CoherenceCloud.LoginWithXbox">Xbox</see>
        /// </description> </item>
        /// <item> <description>
        /// <see cref="CoherenceCloud.LoginWithNintendo">Nintendo</see>
        /// </description> </item>
        /// </list>
        /// </remarks>
        public bool IsVerified { get; private set; }

        /// <summary>
        /// Is this player account logged in to coherence Cloud?
        /// </summary>
        public bool IsLoggedIn => services?.IsLoggedIn ?? false;

        /// <summary>
        /// Coherence cloud services for this player account.
        /// <remarks>
        /// The player account needs to be <see cref="IsLoggedIn">logged in</see> to coherence Cloud
        /// before they can use most of the services.
        /// </remarks>
        /// </summary>
        public CloudService Services
        {
            get => services;

            internal set
            {
                if (ReferenceEquals(services, value))
                {
                    return;
                }

                var wasLoggedIn = IsLoggedIn;
                if (services is not null)
                {
                    services.AuthClient.OnLoggingIn -= OnLoggingIn;
                    services.AuthClient.OnLogin -= OnLoggedIn;
                    services.AuthClient.OnLoggingOut -= OnLoggingOut;

                    if (shouldDisposeCloudService)
                    {
                        var servicesToDispose = services;
                        if (ReferenceEquals(services.AuthClient.PlayerAccount, this))
                        {
                            services.AuthClient.PlayerAccount = null;
                        }

                        services = null;
                        servicesToDispose.Dispose();
                    }
                }

                if (value is not null)
                {
                    value.AuthClient.OnLoggingIn += OnLoggingIn;
                    value.AuthClient.OnLogin += OnLoggedIn;
                    value.AuthClient.OnLoggingOut += OnLoggingOut;
                }

                if (IsLoggedIn && !wasLoggedIn)
                {
                    SetState(State.LoggedIn);
                }
                else if (!IsLoggedIn && wasLoggedIn)
                {
                    SetState(State.LoggedOut);
                }

                services = value;
            }
        }

        /// <summary>
        /// Gets a value indicating whether this is the <see cref="Main">main player account</see>.
        /// </summary>
        public bool IsMain => Equals(Main);

#if UNITY
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void OnStart()
        {
            Application.quitting += OnExitingApplicationOrPlayMode;

            static void OnExitingApplicationOrPlayMode()
            {
                Application.quitting -= OnExitingApplicationOrPlayMode;

                // Dispose player accounts in reverse order so that main player account is disposed last.
                // Otherwise the main player account could unnecessarily change multiple times during this process.
                while (all.Any())
                {
                    all[^1].Dispose();
                }

                all = Array.Empty<PlayerAccount>();
                Main = null;
            }
        }
#endif

        internal PlayerAccount(LoginInfo loginInfo, CloudUniqueId cloudUniqueId, string projectId, CloudService services)
        {
            this.projectId = projectId;
            CloudUniqueId = cloudUniqueId;
            SessionToken = loginInfo.SessionToken;
            AddLoginInfo(loginInfo);
            Services = services;
        }

        ~PlayerAccount() => Dispose();

        /// <summary>
        /// Retrieves the first player account that matches the conditions defined by the specified predicate.
        /// </summary>
        /// <param name="match">
        /// Reference to a method that returns <see langword="true"/> for a valid result.
        /// </param>
        /// <returns>
        /// The first matching player account, if any was found; otherwise, <see langword="None"/>.
        /// </returns>
        [return: MaybeNull]
        public static PlayerAccount Find([DisallowNull] Predicate<PlayerAccount> match) => Array.Find(all, match);

        [return: MaybeNull]
        internal static PlayerAccount Find(LoginInfo info)
        {
            if (info.LoginType is LoginType.SessionToken)
            {
                foreach (var playerAccount in all)
                {
                    if (playerAccount.SessionToken == info.SessionToken)
                    {
                        return playerAccount;
                    }
                }
            }
            else
            {
                foreach (var playerAccount in all)
                {
                    if (playerAccount.loginInfos.Contains(info))
                    {
                        return playerAccount;
                    }
                }
            }

            return null;
        }

        /// <summary>
        /// Retrieves all player accounts that match the conditions defined by the specified predicate.
        /// </summary>
        /// <param name="match">
        /// Reference to a method that returns <see langword="true"/> for valid player accounts to match.
        /// </param>
        /// <returns>
        /// All matching player accounts, if any were found; otherwise, an empty array.
        /// </returns>
        [return: NotNull]
        public static PlayerAccount[] FindAll([DisallowNull] Predicate<PlayerAccount> match) => Array.FindAll(all, match);

        /// <summary>
        /// Gets a task that returns the main player account when one exist and has finished logging in.
        /// </summary>
        /// <remarks>
        /// When the task completes <see cref="PlayerAccount.IsLoggedIn"/> will be <see langword="true"/>, and its
        /// <see cref="Services">cloud services</see> will be usable.
        /// </remarks>
        /// <param name="cancellationToken"> Can be used to cancel the operation. </param>
        /// <returns> Task that can be <see langword="await">awaited</see> to get the main player account. </returns>
        [return: NotNull]
        public static CoherenceTask<PlayerAccount> GetMainAsync(CancellationToken cancellationToken = default) => GetMainAsync(true, cancellationToken);

        /// <summary>
        /// Gets a task that returns the <see cref="Main">main player account</see> when one has started or finished logging in.
        /// </summary>
        /// <param name="waitUntilLoggedIn">
        /// <para>
        /// If <see langword="true"/>, the task will complete when a main player account exists and has finished logging in.
        /// At this point <see cref="PlayerAccount.IsLoggedIn"/> will be <see langword="true"/>, and <see cref="Services">cloud services</see>
        /// are usable.
        /// </para>
        /// <para>
        /// If <see langword="false"/>, the task will complete when a <see cref="Main">main player account</see> exists, even if they are still in the
        /// process of logging in. At this point <see cref="PlayerAccount.IsLoggedIn"/> might still be <see langword="false"/>, and
        /// <see cref="Services">cloud services</see> might not be usable.
        /// </para>
        /// </param>
        /// <param name="cancellationToken"> Can be used to cancel the operation. </param>
        /// <returns> Task that can be <see langword="await">awaited</see> to get the main player account. </returns>
        [return: NotNull]
        public static CoherenceTask<PlayerAccount> GetMainAsync(bool waitUntilLoggedIn, CancellationToken cancellationToken = default)
        {
            if (main is not null && (main.IsLoggedIn || !waitUntilLoggedIn))
            {
                return new(Task.FromResult(main));
            }

            var taskCompletionSource = new TaskCompletionSource<PlayerAccount>();
            if (waitUntilLoggedIn)
            {
                OnMainLoggedIn += OnMainPlayerAccountReady;
            }
            else
            {
                OnMainChanged += OnMainPlayerAccountReady;
            }

            if (cancellationToken != CancellationToken.None)
            {
                cancellationToken.Register(() =>
                {
                    OnMainLoggedIn -= OnMainPlayerAccountReady;
                    OnMainChanged -= OnMainPlayerAccountReady;
                    taskCompletionSource.TrySetCanceled();
                }, useSynchronizationContext: false);
            }

            return new(taskCompletionSource.Task);

            void OnMainPlayerAccountReady([MaybeNull] PlayerAccount playerAccount)
            {
                if (playerAccount is null)
                {
                    return;
                }

                OnMainChanged -= OnMainPlayerAccountReady;
                OnMainLoggedIn -= OnMainPlayerAccountReady;

                if (cancellationToken.IsCancellationRequested)
                {
                    taskCompletionSource.TrySetCanceled();
                    return;
                }

                taskCompletionSource.TrySetResult(playerAccount);
            }
        }

        /// <summary>
        /// Makes this <see cref="PlayerAccount"/> the <see cref="Main">main player account</see>.
        /// </summary>
        public void SetAsMain() => Main = this;

        /// <summary>
        /// Gets all the data associated with this player account from coherence Cloud.
        /// </summary>
        /// <param name="cancellationToken"> Can be used to cancel the operation. </param>
        /// <returns> Result of the operation. </returns>
        public PlayerAccountOperation<PlayerAccountInfo> GetInfo(CancellationToken cancellationToken = default)
            => Services.AuthClient.PlayerAccountOperationAsync<GetAccountInfoRequest, GetAccountInfoResponse, PlayerAccountInfo>(IPlayerAccountOperationRequest.GetAccountInfo(), x => new
            (
                id: x.Id,
                username: x.Username,
                displayName: x.DisplayName,
                avatarUrl: x.AvatarUrl,
                identities: x.Identities?.Select(i => new Identity(i.Type switch
                {
                    "guest" => IdentityType.Guest,
                    "userpass" => IdentityType.UsernameAndPassword,
                    "steam" => IdentityType.Steam,
                    "epic" => IdentityType.EpicGames,
                    "psn" => IdentityType.PlayStation,
                    "xbox" => IdentityType.Xbox,
                    "nintendo" => IdentityType.Nintendo,
                    "jwt" => IdentityType.Jwt,
                    "code" => IdentityType.OneTimeCode,
                    "google" => IdentityType.Google,
                    "apple" => IdentityType.Apple,
                    _ => IdentityType.Unknown
                }, i.Id)).ToArray() ?? Array.Empty<Identity>(),
                createdAt: DateTimeOffset.FromUnixTimeSeconds(x.CreatedAt),
                isVerified: x.Verified
            ), cancellationToken, null); // TODO: Update info locally

        /// <summary>
        /// Assign a unique username and an optional password to the current player account.
        /// </summary>
        /// <param name="username"> Username for the current account. </param>
        /// <param name="password">
        /// (Optional) Password for the account.
        /// <remarks>
        /// If no password is provided, then the option to <see cref="CoherenceCloud.LoginWithPassword">login with password</see> will not be available.
        /// This might still be useful if the username is used for game purposes only and not as an authentication method.
        /// </remarks>
        /// </param>
        /// <param name="force">
        /// <para>
        /// If the username is already taken by another player account, and a <paramref name="force"/> value of <see langword="false"/> is passed, then an error will be returned.
        /// </para>
        /// <para>
        /// If the username is already linked to another player account, and a <paramref name="force"/> value of <see langword="true"/> is passed, then the username
        /// will be unlinked from the other player account, and linked to this one.
        /// </para>
        /// <para>
        /// WARNING: If the username is unlinked from another player account, and that other player account has no other authentication methods set up besides said username and its associated password,
        /// then access to that player account will be lost.
        /// </para>
        /// </param>
        /// <param name="cancellationToken"> Can be used to cancel the operation. </param>
        /// <returns> Result of the operation. </returns>
        public PlayerAccountOperation SetUsername(string username, string password = "", bool force = false, CancellationToken cancellationToken = default)
            => Services.AuthClient.PlayerAccountOperationAsync(IPlayerAccountOperationRequest.SetUsername(username, password, force), cancellationToken,
                () => Username = username);

        /// <summary>
        /// Remove the username from the current player account.
        /// </summary>
        /// <param name="force">
        /// <para>
        /// If the player account has no other authentication methods set up besides the username, and a <paramref name="force"/> value of <see langword="false"/> is passed, then an error will be returned.
        /// </para>
        /// <para>
        /// If the player account has no other authentication methods set up besides the username, and a <paramref name="force"/> value of <see langword="true"/> is passed, then the username will still be removed from the account.
        /// </para>
        /// <para>
        /// WARNING: If the username is removed from the account, and the account has no other authentication methods set up besides said username and its associated password, then access to this player account can be lost.
        /// </para>
        /// </param>
        /// <param name="cancellationToken"></param>
        /// <returns> Result of the operation. </returns>
        public PlayerAccountOperation RemoveUsername(bool force = false, CancellationToken cancellationToken = default)
            => Services.AuthClient.PlayerAccountOperationAsync(IPlayerAccountOperationRequest.RemoveUsername(force), cancellationToken,
                () => Username = "");

        /// <summary>
        /// Assign a display name and an optional avatar image URL to the current player account.
        /// </summary>
        /// <param name="displayName"> Display name for the current account. </param>
        /// <param name="avatarUrl"> (Optional) Web address to an avatar image for the account. </param>
        /// <param name="cancellationToken"> Can be used to cancel the operation. </param>
        /// <returns> Result of the operation. </returns>
        public PlayerAccountOperation SetDisplayInfo(string displayName, string avatarUrl = "", CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrEmpty(avatarUrl))
            {
                avatarUrl = AvatarUrl;
            }

            return Services.AuthClient.PlayerAccountOperationAsync(IPlayerAccountOperationRequest.SetDisplayInfo(displayName, avatarUrl), cancellationToken,
                () =>
                {
                    DisplayName = displayName;
                    AvatarUrl = avatarUrl;
                });
        }

        /// <summary>
        /// Remove display name and avatar image from the current player account.
        /// </summary>
        /// <param name="cancellationToken"> Can be used to cancel the operation. </param>
        /// <returns> Result of the operation. </returns>
        public PlayerAccountOperation RemoveDisplayInfo(CancellationToken cancellationToken = default)
            => Services.AuthClient.PlayerAccountOperationAsync(IPlayerAccountOperationRequest.RemoveDisplayInfo(), cancellationToken,
            () =>
            {
                DisplayName = "";
                AvatarUrl = "";
            });

        /// <summary>
        /// Assign an email address to the current player account.
        /// </summary>
        /// <param name="emailAddress"> Email address for the current account. </param>
        /// <param name="cancellationToken"> Can be used to cancel the operation. </param>
        /// <returns>Result of the operation.</returns>
        public PlayerAccountOperation SetEmail(string emailAddress, CancellationToken cancellationToken = default)
            => Services.AuthClient.PlayerAccountOperationAsync(IPlayerAccountOperationRequest.SetEmail(emailAddress), cancellationToken, null);

        /// <summary>
        /// Remove the email address from the current player account.
        /// </summary>
        /// <param name="cancellationToken">Can be used to cancel the operation.</param>
        /// <returns>Result of the operation.</returns>
        public PlayerAccountOperation RemoveEmail(CancellationToken cancellationToken = default)
            => Services.AuthClient.PlayerAccountOperationAsync(IPlayerAccountOperationRequest.RemoveEmail(), cancellationToken, null);

        /// <summary>
        /// Acquire a one-time code that can be used with <see cref="CoherenceCloud.LoginWithOneTimeCode"/>.
        /// </summary>
        /// <param name="cancellationToken">Can be used to cancel the operation.</param>
        /// <returns>Result of the operation.</returns>
        /// <example>
        /// <code source="Runtime/PlayerAccount/LoginWithOneTimeCode.cs" language="csharp"/>
        /// </example>
        public PlayerAccountOperation<string> GetOneTimeCode(CancellationToken cancellationToken = default)
            => Services.AuthClient.PlayerAccountOperationAsync<GetOneTimeCodeRequest, GetOneTimeCodeResponse, string>(IPlayerAccountOperationRequest.GetOneTimeCode(), response => response.OneTimeCode, cancellationToken, null);

        /// <summary>
        /// Links the guest id to the current player account.
        /// </summary>
        /// <param name="guestId"> <see cref="GuestId"/> to link to the player account. </param>
        /// <param name="force">
        /// <para>
        /// If the guest id is already linked to another player account, and a <paramref name="force"/> value of <see langword="false"/> is passed, then an error will be returned.
        /// </para>
        /// <para>
        /// If the guest id is already linked to another player account, and a <paramref name="force"/> value of <see langword="true"/> is passed, then the guest id
        /// will be unlinked from the other player account, and linked to this one.
        /// </para>
        /// <para>
        /// WARNING: If the guest id is unlinked from another player account, and that other player account has no other authentication methods set up, then
        /// access to that player account will be lost.
        /// </para>
        /// </param>
        /// <param name="cancellationToken">Can be used to cancel the operation.</param>
        /// <returns>Result of the operation.</returns>
        public PlayerAccountOperation LinkGuest(GuestId guestId, bool force = false, CancellationToken cancellationToken = default)
            => Services.AuthClient.PlayerAccountOperationAsync(IPlayerAccountOperationRequest.LinkGuest(guestId, force), cancellationToken,
            () => AddLoginInfo(LoginInfo.ForGuest(guestId)));

        /// <summary>
        /// Removes guest authentication from the current player account.
        /// </summary>
        /// <param name="force">
        /// <para>
        /// If the player account has no other authentication methods set up besides the guest id, and a <paramref name="force"/> value of <see langword="false"/> is passed, then an error will be returned.
        /// </para>
        /// <para>
        /// If the player account has no other authentication methods set up besides the guest id, and a <paramref name="force"/> value of <see langword="true"/> is passed, then the guest id will still be unlinked from the other player account.
        /// </para>
        /// <para>
        /// WARNING: If the guest id is unlinked from another player account, and that other player account has no other authentication methods set up, then access to this player account can be lost.
        /// </para>
        /// </param>
        /// <param name="cancellationToken">Can be used to cancel the operation.</param>
        /// <returns>Result of the operation.</returns>
        public PlayerAccountOperation UnlinkGuest(bool force = false, CancellationToken cancellationToken = default)
        {
            if (!GuestId.HasValue)
            {
                return new(PlayerAccountErrorType.IdentityNotFound, Error.RuntimeIdentityNotFound, $"{nameof(PlayerAccount)}.{nameof(UnlinkGuest)} was called but player account has not logged in as a guest.");
            }

            return Services.AuthClient.PlayerAccountOperationAsync(IPlayerAccountOperationRequest.UnlinkGuest(GuestId.Value, force), cancellationToken,
                () => RemoveLoginInfos(LoginType.Guest));
        }

        /// <summary>
        /// Adds Steam authentication to the current player account.
        /// </summary>
        /// <param name="ticket"> Steam ticket for the account. </param>
        /// <param name="identity">
        /// (Optional) The identifier string that was passed as a parameter to the GetAuthTicketForWebApi method
        /// of the Steamworks Web API when the ticket was created.
        /// </param>
        /// <param name="force">
        /// <para>
        /// If the Steam account is already linked to another player account, and a <paramref name="force"/> value of <see langword="false"/> is passed, then an error will be returned.
        /// </para>
        /// <para>
        /// If the Steam account is already linked to another player account, and a <paramref name="force"/> value of <see langword="true"/> is passed, then the Steam account
        /// will be unlinked from the other player account, and linked to this one.
        /// </para>
        /// <para>
        /// WARNING: If the Steam account is unlinked from another player account, and that other player account has no other authentication methods set up, then
        /// access to that player account will be lost.
        /// </para>
        /// </param>
        /// <param name="cancellationToken">Can be used to cancel the operation.</param>
        /// <returns>Result of the operation.</returns>
        public PlayerAccountOperation LinkSteam(string ticket, string identity = "", bool force = false, CancellationToken cancellationToken = default)
            => Services.AuthClient.PlayerAccountOperationAsync(IPlayerAccountOperationRequest.LinkSteam(ticket, identity, force), cancellationToken,
                () => AddLoginInfo(LoginInfo.WithSteam(ticket, identity)));

        /// <summary>
        /// Removes Steam authentication from the current player account.
        /// </summary>
        /// <param name="force">
        /// <para>
        /// If the player account has no other authentication methods set up besides the guest id, and a <paramref name="force"/> value of <see langword="false"/> is passed, then an error will be returned.
        /// </para>
        /// <para>
        /// If the player account has no other authentication methods set up besides the guest id, and a <paramref name="force"/> value of <see langword="true"/> is passed, then the guest id will still be unlinked from the other player account.
        /// </para>
        /// <para>
        /// WARNING: If the guest id is unlinked from another player account, and that other player account has no other authentication methods set up, then access to this player account can be lost.
        /// </para>
        /// </param>
        /// <param name="cancellationToken">Can be used to cancel the operation.</param>
        /// <returns>Result of the operation.</returns>
        public PlayerAccountOperation UnlinkSteam(bool force = false, CancellationToken cancellationToken = default)
            => Services.AuthClient.PlayerAccountOperationAsync(IPlayerAccountOperationRequest.UnlinkSteam(force), cancellationToken,
                () => RemoveLoginInfos(LoginType.Steam));

        /// <summary>
        /// Adds Steam authentication to the current player account.
        /// </summary>
        /// <param name="token"> Authentication token acquired using Epic Online Services. </param>
        /// <param name="force">
        /// <para>
        /// If the Epic Games account is already linked to another player account, and a <paramref name="force"/> value of <see langword="false"/> is passed, then an error will be returned.
        /// </para>
        /// <para>
        /// If the Epic Games account is already linked to another player account, and a <paramref name="force"/> value of <see langword="true"/> is passed, then the Epic Games account
        /// will be unlinked from the other player account, and linked to this one.
        /// </para>
        /// <para>
        /// WARNING: If the Epic Games account is unlinked from another player account, and that other player account has no other authentication methods set up, then
        /// access to that player account will be lost.
        /// </para>
        /// </param>
        /// <param name="cancellationToken">Can be used to cancel the operation.</param>
        /// <returns>Result of the operation.</returns>
        public PlayerAccountOperation LinkEpicGames(string token, bool force = false, CancellationToken cancellationToken = default)
            => Services.AuthClient.PlayerAccountOperationAsync(IPlayerAccountOperationRequest.LinkEpicGames(token, force), cancellationToken,
            () => AddLoginInfo(LoginInfo.WithEpicGames(token)));

        /// <summary>
        /// Removes Steam authentication from the current player account.
        /// </summary>
        /// <param name="force">
        /// <para>
        /// If the player account has no other authentication methods set up besides the guest id, and a <paramref name="force"/> value of <see langword="false"/> is passed, then an error will be returned.
        /// </para>
        /// <para>
        /// If the player account has no other authentication methods set up besides the guest id, and a <paramref name="force"/> value of <see langword="true"/> is passed, then the guest id will still be unlinked from the other player account.
        /// </para>
        /// <para>
        /// WARNING: If the guest id is unlinked from another player account, and that other player account has no other authentication methods set up, then access to this player account can be lost.
        /// </para>
        /// </param>
        /// <param name="cancellationToken">Can be used to cancel the operation.</param>
        /// <returns>Result of the operation.</returns>
        public PlayerAccountOperation UnlinkEpicGames(bool force = false, CancellationToken cancellationToken = default)
            => Services.AuthClient.PlayerAccountOperationAsync(IPlayerAccountOperationRequest.UnlinkEpicGames(force), cancellationToken,
                () => RemoveLoginInfos(LoginType.EpicGames));

        /// <summary>
        /// Adds PlayStation Network authentication to the current player account.
        /// </summary>
        /// <param name="token"> A JWT ID token with link to the public certificate embedded. </param>
        /// <param name="force">
        /// <para>
        /// If the PlayStation Network account is already linked to another player account, and a <paramref name="force"/> value of <see langword="false"/> is passed, then an error will be returned.
        /// </para>
        /// <para>
        /// If the PlayStation Network account is already linked to another player account, and a <paramref name="force"/> value of <see langword="true"/> is passed, then the PlayStation Network account
        /// will be unlinked from the other player account, and linked to this one.
        /// </para>
        /// <para>
        /// WARNING: If the PlayStation Network account is unlinked from another player account, and that other player account has no other authentication methods set up, then
        /// access to that player account will be lost.
        /// </para>
        /// </param>
        /// <param name="cancellationToken">Can be used to cancel the operation.</param>
        /// <returns>Result of the operation.</returns>
        public PlayerAccountOperation LinkPlayStation(string token, bool force = false, CancellationToken cancellationToken = default)
            => Services.AuthClient.PlayerAccountOperationAsync(IPlayerAccountOperationRequest.LinkPlayStation(token, force), cancellationToken,
                () => AddLoginInfo(LoginInfo.WithPlayStation(token)));

        /// <summary>
        /// Removes PlayStation Network authentication from the current player account.
        /// </summary>
        /// <param name="force">
        /// <para>
        /// If the player account has no other authentication methods set up besides the guest id, and a <paramref name="force"/> value of <see langword="false"/> is passed, then an error will be returned.
        /// </para>
        /// <para>
        /// If the player account has no other authentication methods set up besides the guest id, and a <paramref name="force"/> value of <see langword="true"/> is passed, then the guest id will still be unlinked from the other player account.
        /// </para>
        /// <para>
        /// WARNING: If the guest id is unlinked from another player account, and that other player account has no other authentication methods set up, then access to this player account can be lost.
        /// </para>
        /// </param>
        /// <param name="cancellationToken">Can be used to cancel the operation.</param>
        /// <returns>Result of the operation.</returns>
        public PlayerAccountOperation UnlinkPlayStation(bool force = false, CancellationToken cancellationToken = default)
            => Services.AuthClient.PlayerAccountOperationAsync(IPlayerAccountOperationRequest.UnlinkPlayStation(force), cancellationToken,
            () => RemoveLoginInfos(LoginType.PlayStation));

        /// <summary>
        /// Adds Xbox account authentication to the current player account.
        /// </summary>
        /// <param name="token">Xbox Live Token provided by the Xbox Live SDK method GetTokenAndSignatureAsync.</param>
        /// <param name="force">
        /// <para>
        /// If the Xbox account is already linked to another player account, and a <paramref name="force"/> value of <see langword="false"/> is passed, then an error will be returned.
        /// </para>
        /// <para>
        /// If the Xbox account is already linked to another player account, and a <paramref name="force"/> value of <see langword="true"/> is passed, then the Xbox account
        /// will be unlinked from the other player account, and linked to this one.
        /// </para>
        /// <para>
        /// WARNING: If the Xbox account is unlinked from another player account, and that other player account has no other authentication methods set up, then
        /// access to that player account will be lost.
        /// </para>
        /// </param>
        /// <param name="cancellationToken">Can be used to cancel the operation.</param>
        /// <returns>Result of the operation.</returns>
        public PlayerAccountOperation LinkXbox(string token, bool force = false, CancellationToken cancellationToken = default)
            => Services.AuthClient.PlayerAccountOperationAsync(IPlayerAccountOperationRequest.LinkXbox(token, force), cancellationToken,
                () => AddLoginInfo(LoginInfo.WithXbox(token)));

        /// <summary>
        /// Removes Xbox account authentication from the current player account.
        /// </summary>
        /// <param name="force">
        /// <para>
        /// If the player account has no other authentication methods set up besides the guest id, and a <paramref name="force"/> value of <see langword="false"/> is passed, then an error will be returned.
        /// </para>
        /// <para>
        /// If the player account has no other authentication methods set up besides the guest id, and a <paramref name="force"/> value of <see langword="true"/> is passed, then the guest id will still be unlinked from the other player account.
        /// </para>
        /// <para>
        /// WARNING: If the guest id is unlinked from another player account, and that other player account has no other authentication methods set up, then access to this player account can be lost.
        /// </para>
        /// </param>
        /// <param name="cancellationToken">Can be used to cancel the operation.</param>
        /// <returns>Result of the operation.</returns>
        public PlayerAccountOperation UnlinkXbox(bool force = false, CancellationToken cancellationToken = default)
            => Services.AuthClient.PlayerAccountOperationAsync(IPlayerAccountOperationRequest.UnlinkXbox(force), cancellationToken,
                () => RemoveLoginInfos(LoginType.Xbox));

        /// <summary>
        /// Adds Nintendo Services Account authentication to the current player account.
        /// </summary>
        public PlayerAccountOperation LinkNintendo(string token, bool force = false, CancellationToken cancellationToken = default)
            => Services.AuthClient.PlayerAccountOperationAsync(IPlayerAccountOperationRequest.LinkNintendo(token, force), cancellationToken,
                () => AddLoginInfo(LoginInfo.WithNintendo(token)));


        /// <summary>
        /// Removes Nintendo Services Account authentication from the current player account.
        /// </summary>
        /// <param name="force">
        /// <para>
        /// If the player account has no other authentication methods set up besides the guest id, and a <paramref name="force"/> value of <see langword="false"/> is passed, then an error will be returned.
        /// </para>
        /// <para>
        /// If the player account has no other authentication methods set up besides the guest id, and a <paramref name="force"/> value of <see langword="true"/> is passed, then the guest id will still be unlinked from the other player account.
        /// </para>
        /// <para>
        /// WARNING: If the guest id is unlinked from another player account, and that other player account has no other authentication methods set up, then access to this player account can be lost.
        /// </para>
        /// </param>
        /// <param name="cancellationToken">Can be used to cancel the operation.</param>
        /// <returns>Result of the operation.</returns>
        public PlayerAccountOperation UnlinkNintendo(bool force = false, CancellationToken cancellationToken = default)
            => Services.AuthClient.PlayerAccountOperationAsync(IPlayerAccountOperationRequest.UnlinkNintendo(force), cancellationToken,
                () => RemoveLoginInfos(LoginType.Nintendo));

        /// <summary>
        /// Adds custom JSON Web Token (JWT) authentication to the current player account.
        /// </summary>
        /// <param name="token">
        /// <para>
        /// JSON Web Token (JWT) to link.
        /// </para>
        /// <para>
        /// Signed with an asymmetric algorithm (such as RS256 or ES256).
        /// Must contain a valid “sub” value which is the external unique player ID.
        /// </para>
        /// </param>
        /// <param name="force">
        /// <para>
        /// If the JWT is already linked to another player account, and a <paramref name="force"/> value of <see langword="false"/> is passed, then an error will be returned.
        /// </para>
        /// <para>
        /// If the JWT is already linked to another player account, and a <paramref name="force"/> value of <see langword="true"/> is passed, then the JWT
        /// will be unlinked from the other player account, and linked to this one.
        /// </para>
        /// <para>
        /// WARNING: If the JWT is unlinked from another player account, and that other player account has no other authentication methods set up, then
        /// access to that player account will be lost.
        /// </para>
        /// </param>
        /// <param name="cancellationToken">Can be used to cancel the operation.</param>
        /// <returns>Result of the operation.</returns>
        public PlayerAccountOperation LinkJwt(string token, bool force = false, CancellationToken cancellationToken = default)
            => Services.AuthClient.PlayerAccountOperationAsync(IPlayerAccountOperationRequest.LinkJwt(token, force), cancellationToken,
                () => AddLoginInfo(LoginInfo.WithJwt(token)));

        /// <summary>
        /// Removes custom JSON Web Token (JWT) authentication from the current player account.
        /// </summary>
        /// <param name="force">
        /// <para>
        /// If the player account has no other authentication methods set up besides the guest id, and a <paramref name="force"/> value of <see langword="false"/> is passed, then an error will be returned.
        /// </para>
        /// <para>
        /// If the player account has no other authentication methods set up besides the guest id, and a <paramref name="force"/> value of <see langword="true"/> is passed, then the guest id will still be unlinked from the other player account.
        /// </para>
        /// <para>
        /// WARNING: If the guest id is unlinked from another player account, and that other player account has no other authentication methods set up, then access to this player account can be lost.
        /// </para>
        /// </param>
        /// <param name="cancellationToken">Can be used to cancel the operation.</param>
        /// <returns>Result of the operation.</returns>
        public PlayerAccountOperation UnlinkJwt(bool force = false, CancellationToken cancellationToken = default)
            => Services.AuthClient.PlayerAccountOperationAsync(IPlayerAccountOperationRequest.UnlinkJwt(force), cancellationToken,
                () => RemoveLoginInfos(LoginType.Jwt));

        /// <summary>
        /// Logs this player account out from coherence Cloud.
        /// <remarks>
        /// If this player account is not logged in, this method does nothing.
        /// </remarks>
        /// </summary>
        public void Logout() => Services?.AuthClient.Logout();

        internal static void Register([DisallowNull] PlayerAccount playerAccount)
        {
            Add(playerAccount);
            if (main is null)
            {
                all[0].SetAsMain();
            }
        }

        internal static void Unregister([DisallowNull] PlayerAccount playerAccount)
        {
            var index = Array.IndexOf(all, playerAccount);
            if (index != -1)
            {
                var newLength = all.Length - 1;
                var newArray = new PlayerAccount[newLength];

                Array.Copy(all, 0, newArray, 0, index);
                Array.Copy(all, index + 1, newArray, index, newLength - index);

                all = newArray;
            }

            if (playerAccount.IsMain)
            {
                Main = all.FirstOrDefault();
            }
        }

        private static void Add(PlayerAccount playerAccount)
        {
            var index = Array.IndexOf(all, playerAccount);
            if (index != -1)
            {
                return;
            }

            index = all.Length;
            Array.Resize(ref all, index + 1);
            all[index] = playerAccount;
        }

        internal void AddLoginInfo(LoginInfo loginInfo)
        {
            if (!loginInfos.Add(loginInfo))
            {
                return;
            }

            if (loginInfo.IsGuest && loginInfo.GuestId != Cloud.GuestId.None)
            {
                GuestId = loginInfo.GuestId;
            }

            if (loginInfo.Username is { Length: > 0 } username)
            {
                Username = username;
            }

            if (loginInfo.SessionToken != SessionToken.None)
            {
                SessionToken = loginInfo.SessionToken;
            }

            UpdateIsGuest();
        }

        private void RemoveLoginInfos(LoginType loginType) => RemoveLoginInfos(x => x.LoginType == loginType);

        private void RemoveLoginInfos(Func<LoginInfo, bool> predicate)
        {
            foreach (var loginInfo in loginInfos.Where(predicate).ToArray())
            {
                loginInfos.Remove(loginInfo);

                if (loginInfo.IsGuest && loginInfo.GuestId == GuestId)
                {
                    GuestId = default;
                }
                else if (loginInfo.LoginType is LoginType.Password && string.Equals(loginInfo.Username, Username))
                {
                    Username = "";
                }
            }

            UpdateIsGuest();
        }

        private void UpdateIsGuest() => IsGuest = loginInfos.All(x => x.LoginType is LoginType.Guest or LoginType.LegacyGuest or LoginType.OneTimeCode or LoginType.SessionToken);

        public override string ToString()
        {
            var id = Id != PlayerAccountId.None ? Id.ToString() : CloudUniqueId.ToString();
            if (string.IsNullOrEmpty(id))
            {
                return AddSuffix("None");
            }

            if (DisplayName is { Length: > 0 })
            {
                return AddSuffix(DisplayName);
            }

            if (IsGuest)
            {
                return AddSuffix($"Guest {id}");
            }

            if (Username is { Length: > 0 })
            {
                return AddSuffix(Username);
            }

            return AddSuffix($"Player {id}");

            string AddSuffix(string prefix) => IsMain ? prefix + " (Main)" : prefix;
        }

        public static implicit operator CloudUniqueId(PlayerAccount playerAccount) => playerAccount.CloudUniqueId;

        private void OnLoggingIn(PlayerAccount playerAccount) => SetState(State.LoggingIn);
        private void OnLoggedIn(LoginResponse response) => SetState(State.LoggedIn);

        private void OnLoggingOut(PlayerAccount playerAccount)
        {
            SetState(State.LoggingOut);
            SetState(State.LoggedOut);
        }

        /// <summary>
        /// Removes this player account from <see cref="All">the list of all player accounts</see>,
        /// logs them out from coherence Cloud, and releases all resources used by it.
        /// </summary>
        public void Dispose()
        {
            if (isDisposed)
            {
                return;
            }

            DisposeShared();
            Services = null;
        }

        /// <param name="waitForOngoingOperationsToFinish">
        /// If true, then ongoing and queued cloud operations are allowed to finish before the services
        /// performing them are shut down; otherwise, the operations should be canceled immediately.
        /// </param>
        internal async ValueTask DisposeAsync(bool waitForOngoingOperationsToFinish)
        {
            if (isDisposed)
            {
                return;
            }

            DisposeShared();
            if (services is not null)
            {
                await services.DisposeAsync(waitForOngoingOperationsToFinish);
            }
        }

        private void DisposeShared()
        {
            isDisposed = true;
            Unregister(this);
            if (shouldReleaseGuid)
            {
                shouldReleaseGuid = false;
                CloudUniqueIdPool.Release(projectId, CloudUniqueId);
            }
        }

        internal static Action<Task<LoginResult>> OnLoginAttemptCompleted(TaskCompletionSource<PlayerAccount> taskCompletionSource, CloudService services, CancellationToken cancellationToken) => task =>
        {
            if (cancellationToken.IsCancellationRequested || task.IsCanceled)
            {
                services.Dispose();
                taskCompletionSource.TrySetCanceled();
                CoherenceCloud.RaiseOnLoggingInFailed(new(LoginErrorType.None, new TaskCanceledException().ToString(), Error.OperationCanceled, hasBeenObserved: true));
                return;
            }

            if (task.IsFaulted)
            {
                services.Dispose();
                taskCompletionSource.TrySetException(task.Exception);
                var errorType = task.Exception.TryExtract(out LoginError loginError) ? loginError.LoginErrorType : LoginErrorType.None;
                CoherenceCloud.RaiseOnLoggingInFailed(new(errorType, task.Exception.ToString(), (Error.RuntimeInternalException)));
                return;
            }

            var loginResult = task.Result;

            if (loginResult.Type is Result.Success)
            {
                var playerAccount = loginResult.PlayerAccount;
                playerAccount.LoginResult = loginResult;

                #if UNITY
                playerAccount.shouldDisposeCloudService = !SimulatorUtility.UseSharedCloudCredentials;
                #else
                playerAccount.shouldDisposeCloudService = true;
                #endif

                taskCompletionSource.TrySetResult(playerAccount);
            }
            else
            {
                services.Dispose();
                taskCompletionSource.TrySetException(new LoginError(loginResult.ErrorType, loginResult.LoginErrorType, loginResult.Error, loginResult.ErrorMessage));
                CoherenceCloud.RaiseOnLoggingInFailed(new(LoginErrorType.None, task.Exception.ToString(), (Error.RuntimeInternalException)));
            }
        };

        public static bool operator ==(PlayerAccount x, PlayerAccount y) => x is null ? y is null : x.Equals(y);
        public static bool operator !=(PlayerAccount x, PlayerAccount y) => x is null ? y is not null : !x.Equals(y);
        public bool Equals(PlayerAccount other)
        {
            if (other is null)
            {
                return false;
            }

            if (!string.IsNullOrEmpty(cloudUniqueId) && !string.IsNullOrEmpty(other.cloudUniqueId))
            {
                return string.Equals(cloudUniqueId, other.cloudUniqueId);
            }

            foreach (var loginInfo in other.loginInfos)
            {
                if (loginInfos.Contains(loginInfo))
                {
                    return true;
                }
            }

            return false;
        }

        public override bool Equals(object obj) => obj is PlayerAccount other && Equals(other);
        public override int GetHashCode() => HashCode.Combine(cloudUniqueId, Username, Id);

        private void SetState(State setState)
        {
            if(state == setState)
            {
                return;
            }

            if (state is State.LoggedIn && setState is State.LoggedOut)
            {
                SetState(State.LoggingOut);
            }
            else if (state is State.LoggedOut && setState is State.LoggedIn)
            {
                SetState(State.LoggingIn);
            }

            this.state = setState;

            switch (setState)
            {
                case State.LoggingOut:
                    CoherenceCloud.RaiseOnLoggingOut(this);
                    break;
               case State.LoggedOut:
                    Unregister(this);
                    loginInfos.Clear();
                    LoginResult = null;
                    SessionToken = SessionToken.None;
                    IsGuest = false;
                    break;
                case State.LoggingIn:
                    Register(this);
                    CoherenceCloud.RaiseOnLoggingIn(this);
                    break;
                case State.LoggedIn:
                    Register(this);
                    CoherenceCloud.RaiseOnLoggedIn(this);
                    if (IsMain)
                    {
                        OnMainLoggedIn?.Invoke(this);
                    }
                    break;
            }
        }

        private enum State
        {
            LoggedOut,
            LoggingIn,
            LoggedIn,
            LoggingOut
        }
    }
}
