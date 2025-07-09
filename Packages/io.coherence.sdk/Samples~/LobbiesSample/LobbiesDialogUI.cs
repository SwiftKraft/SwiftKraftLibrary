using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Coherence.Cloud;
using Coherence.Connection;
using Coherence.Runtime;
using Coherence.Toolkit;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace Coherence.Samples.LobbiesDialog
{
    public class LobbiesDialogUI : MonoBehaviour
    {
        #region References
        [Header("References")]
        public GameObject sampleUi;
        public GameObject connectDialog;
        public GameObject disconnectDialog;
        public GameObject createRoomPanel;
        public GameObject noCloudPlaceholder;
        public GameObject noLobbiesAvailable;
        public GameObject loadingSpinner;
        public LobbySessionUI lobbySessionUI;
        public Text joinLobbyTitleText;
        public ConnectDialogLobbyView templateLobbyView;
        public InputField lobbyNameInputField;
        public InputField lobbyLimitInputField;
        public Dropdown regionDropdown;
        public Button refreshRegionsButton;
        public Button refreshLobbiesButton;
        public Button joinLobbyButton;
        public Button showCreateLobbyPanelButton;
        public Button hideCreateLobbyPanelButton;
        public Button createAndJoinLobbyButton;
        public Button disconnectButton;
        public GameObject popupDialog;
        public Text popupText;
        public Text popupTitleText;
        public Button popupDismissButton;
        public InputField nameText;
        public GameObject matchmakingRegionsContainer;
        public Toggle matchmakingRegionsTemplate;
        public Text matchmakingTag;
        public Button matchmakingButton;
        public GameObject matchmakingCreateRegionsContainer;
        public Toggle matchmakingCreateRegionsTemplate;
        public ToggleGroup matchmakingCreateRegionToggleGroup;
        #endregion

        private int PlayerLobbyLimit => int.TryParse(lobbyLimitInputField.text, out var limit) ? limit : 10;

        private string initialJoinLobbyTitle;
        private ListView lobbiesListView;
        private string lastCreatedLobbyId;
        private Coroutine cloudServiceReady;
        private CoherenceBridge bridge;
        private PlayerAccount playerAccount;

        private string selectedRegion;

        private readonly List<Toggle> instantiatedRegionToggles = new();
        private readonly List<Toggle> instantiatedCreateRegionToggles = new();

        private CloudRooms CloudRooms => bridge.CloudService?.Rooms;
        private bool cloudServiceHasBeenSetUp;

        #region Unity Events
        private void OnEnable()
        {
            if (bridge == null)
            {
                if (!CoherenceBridgeStore.TryGetBridge(gameObject.scene, out bridge))
                {
                    Debug.LogError(
                        $"Couldn't find a {nameof(CoherenceBridge)} in your scene. This dialog will not function properly.");
                    return;
                }
            }

            SetUpCloudService();

            disconnectDialog.SetActive(false);

            if (!string.IsNullOrEmpty(RuntimeSettings.Instance.ProjectID))
            {
                cloudServiceReady = StartCoroutine(WaitForCloudService());
            }
            else if (regionDropdown.gameObject.activeInHierarchy)
            {
                noCloudPlaceholder.SetActive(true);
            }
        }

        private void SetUpCloudService()
        {
            if (cloudServiceHasBeenSetUp || bridge.CloudService is not { } cloudService)
            {
                return;
            }

            cloudServiceHasBeenSetUp = true;
            var auth = cloudService.GameServices.AuthService;
            auth.OnLogin -= AuthServiceOnLogin;
            auth.OnLogin += AuthServiceOnLogin;
            bridge.onConnected.AddListener(_ => UpdateDialogsVisibility());
            bridge.onDisconnected.AddListener((_, _) => UpdateDialogsVisibility());
            bridge.onConnectionError.AddListener(OnConnectionError);
            if (bridge.IsConnected)
            {
                UpdateDialogsVisibility();
            }
        }

        private void AuthServiceOnLogin(LoginResponse loginResponse) => OnLogin(loginResponse.LobbyIds);

        private async void OnLogin(IReadOnlyList<string> lobbyIds)
        {
            if (lobbyIds?.FirstOrDefault() is { } lobbyId)
            {
                try
                {
                    var session = await CloudRooms.LobbyService.GetActiveLobbySessionForLobbyId(lobbyId);

                    OnJoinedLobby(new()
                    {
                        Status = RequestStatus.Success,
                        Exception = null,
                        Result = session
                    });
                }
                catch (Exception e)
                {
                    OnJoinedLobby(new()
                    {
                        Status = RequestStatus.Fail,
                        Exception = e,
                        Result = null
                    });
                }
            }
        }

        private void OnDisable()
        {
            if (cloudServiceReady != null)
            {
                StopCoroutine(cloudServiceReady);
                cloudServiceReady = null;
            }
        }

        void Start()
        {
            matchmakingRegionsTemplate.gameObject.SetActive(false);
            matchmakingCreateRegionsTemplate.gameObject.SetActive(false);
            nameText.text = Environment.UserName;
            joinLobbyButton.onClick.AddListener(() => JoinLobby(lobbiesListView.Selection.LobbyData));
            showCreateLobbyPanelButton.onClick.AddListener(ShowCreateRoomPanel);
            hideCreateLobbyPanelButton.onClick.AddListener(HideCreateRoomPanel);
            createAndJoinLobbyButton.onClick.AddListener(CreateLobbyAndJoin);
            regionDropdown.onValueChanged.AddListener(OnRegionChanged);
            refreshRegionsButton.onClick.AddListener(RefreshRegions);
            refreshLobbiesButton.onClick.AddListener(RefreshLobbies);
            disconnectButton.onClick.AddListener(bridge.Disconnect);
            popupDismissButton.onClick.AddListener(HideError);
            matchmakingButton.onClick.AddListener(MatchmakingLobby);

            popupDialog.SetActive(false);
            noLobbiesAvailable.SetActive(false);
            joinLobbyButton.interactable = false;
            showCreateLobbyPanelButton.interactable = false;
            templateLobbyView.gameObject.SetActive(false);
            lobbyNameInputField.text = "My Lobby";

            lobbiesListView = new()
            {
                Template = templateLobbyView,
                onSelectionChange = view =>
                {
                    joinLobbyButton.interactable = view != default && view.LobbyData.Id != default(LobbyData).Id;
                }
            };

            initialJoinLobbyTitle = joinLobbyTitleText.text;

            if (bridge.PlayerAccountAutoConnect is not CoherenceBridgePlayerAccount.AutoLoginAsGuest)
            {
                ConnectToCoherenceCloud();
            }
        }

        private void OnDestroy() => playerAccount?.Dispose();

        #endregion

        #region Cloud & Replication Server Requests
        private void ConnectToCoherenceCloud()
        {
            PlayerAccount.OnMainChanged += OnMainPlayerAccountChanged;
            OnMainPlayerAccountChanged(PlayerAccount.Main);
            CoherenceCloud.LoginAsGuest().OnFail(error =>
            {
                var errorMessage = $"Logging in to coherence Cloud Failed.\n{error}";
                ShowError("Logging in Failed", errorMessage);
                Debug.LogError(errorMessage);
            });


            void OnMainPlayerAccountChanged(PlayerAccount mainPlayerAccount)
            {
                if (mainPlayerAccount is null)
                {
                    return;
                }

                playerAccount = mainPlayerAccount;
                PlayerAccount.OnMainChanged -= OnMainPlayerAccountChanged;
                if (bridge.PlayerAccountAutoConnect is CoherenceBridgePlayerAccount.None)
                {
                    bridge.CloudService = mainPlayerAccount.Services;
                }

                SetUpCloudService();
            }
        }

        private IEnumerator WaitForCloudService()
        {
            ShowLoadingState();

            while (CloudRooms is not { IsLoggedIn : true })
            {
                yield return null;
            }

            HideLoadingState();

            RefreshRegions();
            cloudServiceReady = null;
        }

        private void RefreshLobbies()
        {
            if (CloudRooms is not { IsLoggedIn : true })
            {
                return;
            }

            ShowLoadingState();
            noLobbiesAvailable.SetActive(false);
            refreshLobbiesButton.interactable = false;

            LobbyFilter fetchLobbyFilter = new LobbyFilter()
                .WithRegion(FilterOperator.Any, new List<string> { selectedRegion });

            var lobbyFilters = new List<LobbyFilter> { fetchLobbyFilter };
            CloudRooms.LobbyService.FindLobbies(OnFetchLobbies, new() { LobbyFilters = lobbyFilters });
        }

        private void CreateLobby()
        {
            if (CloudRooms is not { IsLoggedIn : true })
            {
                return;
            }

            ShowLoadingState();

            var playerAttribute = GetPlayerNameAttribute();

            var createOptions = new CreateLobbyOptions()
            {
                Region = matchmakingCreateRegionToggleGroup.GetFirstActiveToggle().GetComponentInChildren<Text>().text.ToLowerInvariant(),
                MaxPlayers = PlayerLobbyLimit,
                Name = lobbyNameInputField.text,
                PlayerAttributes = playerAttribute
            };

            CloudRooms.LobbyService.CreateLobby(createOptions, OnCreatedLobby);
            HideCreateRoomPanel();
        }

        private List<CloudAttribute> GetPlayerNameAttribute()
        {
            var playerAttribute = new List<CloudAttribute> { new("player_name", nameText.text, true) };
            return playerAttribute;
        }

        private void JoinLobby(LobbyData lobbyData)
        {
            if (CloudRooms is not { IsLoggedIn : true })
            {
                return;
            }

            ShowLoadingState();

            var playerAttribute = GetPlayerNameAttribute();

            CloudRooms.LobbyService.JoinLobby(lobbyData, OnJoinedLobby, playerAttribute);
        }

        private void CreateLobbyAndJoin() => CreateLobby();

        private void RefreshRegions()
        {
            if (CloudRooms is { IsLoggedIn : true })
            {
                CloudRooms.RefreshRegions(OnRegionsChanged);
            }
        }

        private void MatchmakingLobby()
        {
            if (CloudRooms is not { IsLoggedIn : true })
            {
                return;
            }

            ShowLoadingState();

            var regions = new List<string>();

            foreach (var regionToggle in instantiatedRegionToggles)
            {
                if (regionToggle.isOn)
                {
                    regions.Add(regionToggle.GetComponentInChildren<Text>().text.ToLowerInvariant());
                }
            }

            LobbyFilter filter = new LobbyFilter()
                .WithAnd()
                .WithRegion(FilterOperator.Any, regions)
                .WithTag(FilterOperator.Any, new() { matchmakingTag.text });

            var findOptions = new FindLobbyOptions { LobbyFilters = new() { filter } };
            var playerNameAttribute = GetPlayerNameAttribute();
            var region = matchmakingCreateRegionToggleGroup.GetFirstActiveToggle().GetComponentInChildren<Text>().text.ToLowerInvariant();

            var createOptions = new CreateLobbyOptions
            {
                Tag = matchmakingTag.text,
                Region = region,
                MaxPlayers = PlayerLobbyLimit,
                PlayerAttributes = playerNameAttribute
            };

            CloudRooms.LobbyService.FindOrCreateLobby(findOptions, createOptions, OnJoinedLobby);
            HideCreateRoomPanel();
        }

        #endregion

        #region Request Callbacks
        private void OnRegionsChanged(RequestResponse<IReadOnlyList<string>> requestResponse)
        {
            HideLoadingState();

            if (!AssertRequestResponse("Error while fetching room regions", requestResponse.Status, requestResponse.Exception))
            {
                return;
            }

            var options = new List<Dropdown.OptionData>();
            var regions = requestResponse.Result;
            foreach (var region in regions)
            {
                options.Add(new(region));
            }

            regionDropdown.options = options;

            if (regions.Count > 0)
            {
                selectedRegion = regions[0];
                regionDropdown.captionText.text = regions[0];
                RefreshLobbies();
            }
        }

        private void OnFetchLobbies(RequestResponse<IReadOnlyList<LobbyData>> requestResponse)
        {
            var lobbies = requestResponse.Result;

            if (gameObject.activeInHierarchy)
            {
                StartCoroutine(ReactivateRefreshButton());
            }
            else
            {
                refreshLobbiesButton.interactable = true;
            }

            loadingSpinner.SetActive(false);
            HideLoadingState();

            joinLobbyTitleText.text = initialJoinLobbyTitle + " (0)";
            noLobbiesAvailable.SetActive(requestResponse.Status != RequestStatus.Success || requestResponse.Result.Count == 0);

            if (!AssertRequestResponse("Error while fetching available lobbies", requestResponse.Status, requestResponse.Exception))
            {
                lobbiesListView.Clear();
                return;
            }

            if (lobbies.Count == 0)
            {
                lobbiesListView.Clear();
                return;
            }

            lobbiesListView.SetSource(lobbies, lastCreatedLobbyId);
            lastCreatedLobbyId = default; // selection was already set.
            joinLobbyTitleText.text = $"{initialJoinLobbyTitle} ({lobbies.Count})";

            joinLobbyButton.interactable = lobbiesListView.Selection != default;
        }

        private IEnumerator ReactivateRefreshButton()
        {
            while (CloudRooms.LobbyService.GetFindLobbiesCooldown() > TimeSpan.Zero)
            {
                yield return null;
            }

            refreshLobbiesButton.interactable = true;
        }

        private void OnCreatedLobby(RequestResponse<LobbySession> response)
        {
            HideLoadingState();

            if (!AssertRequestResponse("Error while creating and joining lobby", response.Status, response.Exception))
            {
                return;
            }

            ActivateLobbySessionUI(response);
        }

        private void ActivateLobbySessionUI(RequestResponse<LobbySession> response)
        {
            connectDialog.SetActive(false);
            lobbySessionUI.gameObject.SetActive(true);
            lobbySessionUI.Initialize(response.Result);
        }

        private void OnJoinedLobby(RequestResponse<LobbySession> response)
        {
            HideLoadingState();

            if (!AssertRequestResponse("Error while joining lobby", response.Status, response.Exception))
            {
                return;
            }

            ActivateLobbySessionUI(response);
        }
        #endregion

        #region Error Handling
        private void ShowError(string title, string message = "Unknown Error")
        {
            popupDialog.SetActive(true);
            popupTitleText.text = title;
            popupText.text = message;
            Debug.LogError(message);
        }

        private void HideError() => popupDialog.SetActive(false);

        private bool AssertRequestResponse(string title, RequestStatus status, Exception exception)
        {
            if (status == RequestStatus.Success)
            {
                return true;
            }

            var message = exception?.Message;

            if (exception is RequestException requestEx && requestEx.ErrorCode == ErrorCode.FeatureDisabled)
            {
                message += "\n\nMake sure Persisted Accounts is enabled in your coherence Dashboard.";
            }

            ShowError(title, message);

            return false;
        }

        private void OnConnectionError(CoherenceBridge bridge, ConnectionException exception)
        {
            HideLoadingState();
            RefreshLobbies();
            ShowError("Error connecting to Room", exception?.Message);
        }
        #endregion

        #region Update UI
        private void ShowCreateRoomPanel()
        {
            createRoomPanel.SetActive(true);

            InstantiateRegionToggles(instantiatedRegionToggles, matchmakingRegionsTemplate, matchmakingRegionsContainer.transform);
            InstantiateRegionToggles(instantiatedCreateRegionToggles, matchmakingCreateRegionsTemplate, matchmakingCreateRegionsContainer.transform);
        }

        private void InstantiateRegionToggles(List<Toggle> instantiatedToggles, Toggle template, Transform parent)
        {
            foreach (var toggle in instantiatedToggles)
            {
                Destroy(toggle.gameObject);
            }

            instantiatedToggles.Clear();

            foreach (var region in regionDropdown.options)
            {
                var instantiatedToggle = Instantiate(template, parent);
                instantiatedToggle.gameObject.SetActive(true);
                instantiatedToggle.GetComponentInChildren<Text>().text = region.text.ToUpperInvariant();
                instantiatedToggles.Add(instantiatedToggle);
            }
        }

        private void HideCreateRoomPanel() => createRoomPanel.SetActive(false);

        private void UpdateDialogsVisibility()
        {
            var isConnected = bridge.IsConnected;
            sampleUi.SetActive(!isConnected);
            disconnectDialog.SetActive(isConnected);

            if (!isConnected)
            {
                RefreshLobbies();
            }
        }

        private void HideLoadingState()
        {
            loadingSpinner.SetActive(false);
            showCreateLobbyPanelButton.interactable = true;
            joinLobbyButton.interactable = lobbiesListView != null && lobbiesListView.Selection != default
                                                                && lobbiesListView.Selection.LobbyData.Id != default(LobbyData).Id;
        }

        private void ShowLoadingState()
        {
            loadingSpinner.SetActive(true);
            showCreateLobbyPanelButton.interactable = false;
            joinLobbyButton.interactable = false;
        }

        private void OnRegionChanged(int region)
        {
            if (CloudRooms is not { IsLoggedIn : true })
            {
                return;
            }

            selectedRegion = regionDropdown.options[region].text;

            RefreshLobbies();
        }
        #endregion
    }

    internal class ListView
    {
        public ConnectDialogLobbyView Template;
        public Action<ConnectDialogLobbyView> onSelectionChange;

        public ConnectDialogLobbyView Selection
        {
            get => selection;
            set
            {
                if (selection != value)
                {
                    selection = value;
                    lastSelectedId = selection == default ? default : selection.LobbyData.Id;
                    onSelectionChange?.Invoke(Selection);
                    foreach (var viewRow in Views)
                    {
                        viewRow.IsSelected = selection == viewRow;
                    }
                }
            }
        }

        public List<ConnectDialogLobbyView> Views { get; }
        private ConnectDialogLobbyView selection;
        private string lastSelectedId;

        public ListView(int capacity = 50) => Views = new List<ConnectDialogLobbyView>(capacity);

        public void SetSource(IReadOnlyList<LobbyData> dataSource, string idToSelect = default)
        {
            Clear();

            if (dataSource.Count <= 0)
            {
                return;
            }

            var sortedData = dataSource.ToList();
            sortedData.Sort((lobbyA, lobbyB) => String.CompareOrdinal(lobbyA.Name, lobbyA.Name));

            if (idToSelect == default && lastSelectedId != default)
            {
                idToSelect = lastSelectedId;
            }

            foreach (var data in sortedData)
            {
                var view = MakeViewItem(data);
                Views.Add(view);
                if (data.Id == idToSelect)
                {
                    Selection = view;
                }
            }
        }

        private ConnectDialogLobbyView MakeViewItem(LobbyData data, bool isSelected = false)
        {
            ConnectDialogLobbyView view = Object.Instantiate(Template, Template.transform.parent);
            view.LobbyData = data;
            view.IsSelected = isSelected;
            view.OnClick = () => Selection = view;
            view.gameObject.SetActive(true);
            return view;
        }

        public void Clear()
        {
            Selection = default;
            foreach (var view in Views)
            {
                Object.Destroy(view.gameObject);
            }
            Views.Clear();
        }
    }
}
