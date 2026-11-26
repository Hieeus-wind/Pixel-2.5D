using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Aetherlands.Coast;
using Fusion;
using Fusion.Photon.Realtime;
using Fusion.Sockets;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Aetherlands.Networking
{
    public enum CoastalSessionState
    {
        LocalSolo = 0,
        Connecting = 1,
        Connected = 2,
        Disconnecting = 3,
        Error = 4
    }

    /// <summary>
    /// Manages Photon Fusion 2 Shared Mode lifecycle (capacity 8, playable with 2+ peers)
    /// and clean toggling between Local Solo Play and Shared Mode Multiplayer.
    /// </summary>
    public sealed class CoastalSessionRunner : MonoBehaviour, INetworkRunnerCallbacks
    {
        [Header("Configuration")]
        [SerializeField] private CoastalSessionConfig sessionConfig;
        [SerializeField] private NetworkObject travelerPrefab;
        [SerializeField] private NetworkObject worldRecordPrefab;

        [Header("Scene Integration (Optional)")]
        [SerializeField] private GameObject localSoloTraveler;
        [SerializeField] private CoastalCamera sceneCamera;

        private NetworkRunner _runner;
        private NetworkedTraveler _localTraveler;
        private CoastalWorldRecord _boundWorldRecord;
        private PlayerRef _lastMasterClient;
        private Transform _savedSoloCameraTarget;
        private bool _isShuttingDown;

        public CoastalSessionConfig SessionConfig => sessionConfig;
        public CoastalSessionState State { get; private set; } = CoastalSessionState.LocalSolo;
        public string ActiveRoomName { get; private set; } = CoastalSessionConfig.DefaultRoom;
        public string LastStatusMessage { get; private set; } = "Local solo play active.";
        public NetworkRunner Runner => _runner;
        public NetworkedTraveler LocalTraveler => _localTraveler;

        public bool IsPhotonAppIdConfigured
        {
            get
            {
                if (!PhotonAppSettings.TryGetGlobal(out PhotonAppSettings settings) || settings == null || settings.AppSettings == null)
                {
                    return false;
                }

                return !string.IsNullOrWhiteSpace(settings.AppSettings.AppIdFusion);
            }
        }

        public int MaxPlayers => sessionConfig != null
            ? sessionConfig.EffectiveMaxPlayers
            : CoastalSessionConfig.DefaultMaxPlayers;

        public int PeerCount
        {
            get
            {
                if (_runner == null || !_runner.IsRunning)
                {
                    return 1;
                }

                int count = 0;
                foreach (PlayerRef _ in _runner.ActivePlayers)
                {
                    count++;
                }

                return count;
            }
        }

        public PlayerRef LocalPlayer => _runner != null && _runner.IsRunning ? _runner.LocalPlayer : PlayerRef.None;
        public PlayerRef MasterClient => _runner != null && _runner.IsRunning ? _runner.GetMasterClient() : PlayerRef.None;
        public bool IsMasterClient => _runner != null && _runner.IsRunning && _runner.IsSharedModeMasterClient;

        public CoastalVersionValidationResult VersionGateStatus
        {
            get
            {
                if (State != CoastalSessionState.Connected)
                {
                    return new CoastalVersionValidationResult(true, true, true, "Local solo (n/a)");
                }

                CoastalWorldRecord record = CoastalWorldRecord.Active;
                if (record == null)
                {
                    return new CoastalVersionValidationResult(false, false, false, "Waiting for world record...");
                }

                return record.LastValidationResult;
            }
        }

        public event Action<CoastalSessionState> OnStateChanged;
        public event Action<PlayerRef> OnMasterClientChanged;
        public event Action<NetworkedTraveler> OnLocalTravelerSpawned;

        public void ConfigureSceneReferences(
            CoastalSessionConfig config,
            NetworkObject travelerNetPrefab,
            NetworkObject worldRecordNetPrefab,
            GameObject soloTraveler,
            CoastalCamera camera)
        {
            if (config != null) sessionConfig = config;
            if (travelerNetPrefab != null) travelerPrefab = travelerNetPrefab;
            if (worldRecordNetPrefab != null) worldRecordPrefab = worldRecordNetPrefab;
            if (soloTraveler != null) localSoloTraveler = soloTraveler;
            if (camera != null) sceneCamera = camera;
        }

        private void Awake()
        {
            ActiveRoomName = sessionConfig != null
                ? sessionConfig.EffectiveRoomName
                : CoastalSessionConfig.DefaultRoom;

            CacheSceneReferences();
        }

        private void Update()
        {
            if (_runner == null || !_runner.IsRunning)
            {
                return;
            }

            BindActiveWorldRecordIfNeeded();
            CheckMasterClientChanged(_runner);
        }

        private void OnDestroy()
        {
            UnbindWorldRecord();

            if (_runner != null)
            {
                _runner.RemoveCallbacks(this);
                if (_runner.IsRunning)
                {
                    _runner.Shutdown(destroyGameObject: false);
                }

                _runner = null;
            }
        }

        public async void JoinSharedRoom(string roomName = null)
        {
            await StartSharedSessionAsync(roomName);
        }

        public async void LeaveSharedRoom()
        {
            await LeaveSessionAndReturnToLocalAsync();
        }

        public async Task<bool> StartSharedSessionAsync(string roomName = null)
        {
            if (State == CoastalSessionState.Connecting || State == CoastalSessionState.Connected)
            {
                return State == CoastalSessionState.Connected;
            }

            if (!IsPhotonAppIdConfigured)
            {
                SetState(
                    CoastalSessionState.Error,
                    "Photon AppIdFusion is not configured in Assets/Photon/Fusion/Resources/PhotonAppSettings.asset.");
                return false;
            }

            if (travelerPrefab == null)
            {
                SetState(
                    CoastalSessionState.Error,
                    "NetworkedTraveler prefab is not assigned. Run Tools > Aetherlands > Bake Network Prefabs.");
                return false;
            }

            CacheSceneReferences();
            ActiveRoomName = string.IsNullOrWhiteSpace(roomName)
                ? (sessionConfig != null ? sessionConfig.EffectiveRoomName : CoastalSessionConfig.DefaultRoom)
                : roomName.Trim();

            SetState(CoastalSessionState.Connecting, $"Joining shared room '{ActiveRoomName}'...");

            GameObject runnerGo = new GameObject($"FusionRunner_{ActiveRoomName}");
            runnerGo.transform.SetParent(transform, false);

            _runner = runnerGo.AddComponent<NetworkRunner>();
            _runner.ProvideInput = true;
            _runner.AddCallbacks(this);

            NetworkSceneManagerDefault sceneManager = runnerGo.AddComponent<NetworkSceneManagerDefault>();
            Scene activeScene = SceneManager.GetActiveScene();
            SceneRef sceneRef = activeScene.buildIndex >= 0
                ? SceneRef.FromIndex(activeScene.buildIndex)
                : default;

            StartGameArgs startArgs = new StartGameArgs
            {
                GameMode = GameMode.Shared,
                SessionName = ActiveRoomName,
                PlayerCount = MaxPlayers,
                Scene = sceneRef,
                SceneManager = sceneManager
            };

            StartGameResult result = await _runner.StartGame(startArgs);

            if (!result.Ok)
            {
                string message = $"Failed to join '{ActiveRoomName}': {result.ShutdownReason} {result.ErrorMessage}".Trim();
                CleanupRunnerObject();
                RestoreLocalSoloPlay();
                SetState(CoastalSessionState.Error, message);
                return false;
            }

            _lastMasterClient = _runner.GetMasterClient();
            SetState(CoastalSessionState.Connected, $"Connected to '{ActiveRoomName}' (Shared Mode).");
            return true;
        }

        public async Task LeaveSessionAndReturnToLocalAsync()
        {
            if (_runner == null)
            {
                RestoreLocalSoloPlay();
                SetState(CoastalSessionState.LocalSolo, "Local solo play active.");
                return;
            }

            if (_isShuttingDown)
            {
                return;
            }

            _isShuttingDown = true;
            SetState(CoastalSessionState.Disconnecting, "Leaving shared room...");

            NetworkRunner runnerToStop = _runner;
            try
            {
                await runnerToStop.Shutdown(destroyGameObject: false);
            }
            finally
            {
                _isShuttingDown = false;
                CleanupRunnerObject();
                RestoreLocalSoloPlay();
                SetState(CoastalSessionState.LocalSolo, "Returned to local solo play.");
            }
        }

        #region INetworkRunnerCallbacks

        public void OnPlayerJoined(NetworkRunner runner, PlayerRef player)
        {
            CheckMasterClientChanged(runner);

            if (runner.IsSharedModeMasterClient)
            {
                EnsureWorldRecordSpawned(runner);
            }

            bool hasExisting = runner.TryGetPlayerObject(player, out NetworkObject existingObj) && existingObj != null;
            if (!CoastalSpawnPolicy.ShouldSpawnForJoinedPlayer(
                player.RawEncoded,
                runner.LocalPlayer.RawEncoded,
                hasExisting))
            {
                return;
            }

            int peerSlot = Mathf.Max(0, PeerCount - 1);
            Vector3 spawnPos = sessionConfig != null
                ? sessionConfig.ComputeSpawnPosition(peerSlot)
                : CoastalSpawnPolicy.ComputeSpawnPosition(new Vector3(0f, 0.015f, 0.9f), peerSlot, 0.85f);

            NetworkObject spawned = runner.Spawn(travelerPrefab, spawnPos, Quaternion.identity, player);
            if (spawned == null)
            {
                SetState(CoastalSessionState.Error, "Runner.Spawn returned null for NetworkedTraveler.");
                return;
            }

            runner.SetPlayerObject(player, spawned);

            _localTraveler = spawned.GetComponent<NetworkedTraveler>();
            if (_localTraveler != null)
            {
                DisableLocalSoloAvatar();
                if (sceneCamera != null)
                {
                    _localTraveler.BindCamera(sceneCamera);
                }

                ApplyVersionGateToLocalTraveler();
                OnLocalTravelerSpawned?.Invoke(_localTraveler);
            }
        }

        public void OnPlayerLeft(NetworkRunner runner, PlayerRef player)
        {
            CheckMasterClientChanged(runner);
        }

        public void OnShutdown(NetworkRunner runner, ShutdownReason shutdownReason)
        {
            bool wasExpected = _isShuttingDown || shutdownReason == ShutdownReason.Ok;
            CleanupRunnerObject();
            RestoreLocalSoloPlay();

            if (wasExpected)
            {
                SetState(CoastalSessionState.LocalSolo, "Local solo play active.");
            }
            else
            {
                SetState(CoastalSessionState.Error, $"Session ended: {shutdownReason}");
            }
        }

        public void OnConnectFailed(NetworkRunner runner, NetAddress remoteAddress, NetConnectFailedReason reason)
        {
            CleanupRunnerObject();
            RestoreLocalSoloPlay();
            SetState(CoastalSessionState.Error, $"Connect failed ({reason}).");
        }

        public void OnDisconnectedFromServer(NetworkRunner runner, NetDisconnectReason reason)
        {
            LastStatusMessage = $"Disconnected from server: {reason}";
        }

        public void OnConnectedToServer(NetworkRunner runner)
        {
            LastStatusMessage = $"Connected to '{ActiveRoomName}'.";
        }

        public void OnObjectExitAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player) { }
        public void OnObjectEnterAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player) { }
        public void OnInput(NetworkRunner runner, NetworkInput input) { }
        public void OnInputMissing(NetworkRunner runner, PlayerRef player, NetworkInput input) { }
        public void OnConnectRequest(NetworkRunner runner, NetworkRunnerCallbackArgs.ConnectRequest request, byte[] token) { }
        public void OnUserSimulationMessage(NetworkRunner runner, SimulationMessagePtr message) { }
        public void OnReliableDataReceived(NetworkRunner runner, PlayerRef player, ReliableKey key, ReadOnlySpan<byte> data) { }
        public void OnReliableDataProgress(NetworkRunner runner, PlayerRef player, ReliableKey key, float progress) { }
        public void OnSessionListUpdated(NetworkRunner runner, List<SessionInfo> sessionList) { }
        public void OnCustomAuthenticationResponse(NetworkRunner runner, Dictionary<string, object> data) { }
        public void OnHostMigration(NetworkRunner runner, HostMigrationToken hostMigrationToken) { }
        public void OnSceneLoadDone(NetworkRunner runner) { }
        public void OnSceneLoadStart(NetworkRunner runner) { }

        #endregion

        private void EnsureWorldRecordSpawned(NetworkRunner runner)
        {
            if (CoastalWorldRecord.Active != null || worldRecordPrefab == null)
            {
                return;
            }

            NetworkObject recordObj = runner.Spawn(worldRecordPrefab, Vector3.zero, Quaternion.identity);
            if (recordObj != null)
            {
                CoastalWorldRecord record = recordObj.GetComponent<CoastalWorldRecord>();
                if (record != null && sessionConfig != null)
                {
                    record.Configure(sessionConfig);
                }
            }
        }

        private void BindActiveWorldRecordIfNeeded()
        {
            CoastalWorldRecord active = CoastalWorldRecord.Active;
            if (active == _boundWorldRecord)
            {
                return;
            }

            UnbindWorldRecord();

            _boundWorldRecord = active;
            if (_boundWorldRecord != null)
            {
                if (sessionConfig != null)
                {
                    _boundWorldRecord.Configure(sessionConfig);
                }

                _boundWorldRecord.OnVersionValidated += HandleVersionValidated;
                ApplyVersionGateToLocalTraveler();
            }
        }

        private void UnbindWorldRecord()
        {
            if (_boundWorldRecord != null)
            {
                _boundWorldRecord.OnVersionValidated -= HandleVersionValidated;
                _boundWorldRecord = null;
            }
        }

        private void HandleVersionValidated(CoastalVersionValidationResult result)
        {
            ApplyVersionGateToLocalTraveler();
        }

        private void ApplyVersionGateToLocalTraveler()
        {
            if (_localTraveler == null)
            {
                return;
            }

            CoastalWorldRecord record = CoastalWorldRecord.Active;
            bool allowInteraction = worldRecordPrefab == null
                || (record != null && record.IsInteractionAllowed);
            _localTraveler.SetInteractionEnabled(allowInteraction);
        }

        private void CheckMasterClientChanged(NetworkRunner runner)
        {
            if (runner == null || !runner.IsRunning)
            {
                return;
            }

            PlayerRef currentMaster = runner.GetMasterClient();
            if (currentMaster == _lastMasterClient)
            {
                return;
            }

            _lastMasterClient = currentMaster;
            if (CoastalWorldRecord.Active != null)
            {
                CoastalWorldRecord.Active.NotifyMasterClientChanged();
            }

            OnMasterClientChanged?.Invoke(currentMaster);
        }

        private void CacheSceneReferences()
        {
            if (sceneCamera == null)
            {
                sceneCamera = FindFirstObjectByType<CoastalCamera>();
            }

            if (localSoloTraveler == null)
            {
                TravelerMotor existingMotor = FindFirstObjectByType<TravelerMotor>();
                if (existingMotor != null && existingMotor.GetComponent<NetworkObject>() == null)
                {
                    localSoloTraveler = existingMotor.gameObject;
                }
            }

            if (_savedSoloCameraTarget == null && sceneCamera != null && sceneCamera.target != null)
            {
                _savedSoloCameraTarget = sceneCamera.target;
            }
        }

        private void DisableLocalSoloAvatar()
        {
            if (localSoloTraveler != null && localSoloTraveler.activeSelf)
            {
                localSoloTraveler.SetActive(false);
            }
        }

        private void RestoreLocalSoloPlay()
        {
            _localTraveler = null;

            if (localSoloTraveler != null && !localSoloTraveler.activeSelf)
            {
                localSoloTraveler.SetActive(true);
            }

            if (sceneCamera != null)
            {
                if (localSoloTraveler != null)
                {
                    sceneCamera.target = localSoloTraveler.transform;
                }
                else if (_savedSoloCameraTarget != null)
                {
                    sceneCamera.target = _savedSoloCameraTarget;
                }
            }
        }

        private void CleanupRunnerObject()
        {
            UnbindWorldRecord();
            _lastMasterClient = PlayerRef.None;

            if (_runner != null)
            {
                _runner.RemoveCallbacks(this);
                GameObject runnerGo = _runner.gameObject;
                _runner = null;
                if (runnerGo != null)
                {
                    Destroy(runnerGo);
                }
            }
        }

        private void SetState(CoastalSessionState state, string statusMessage)
        {
            State = state;
            LastStatusMessage = statusMessage;
            OnStateChanged?.Invoke(state);
        }
    }
}
