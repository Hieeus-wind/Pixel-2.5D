using System;
using System.Collections.Generic;
using Fusion;
using UnityEngine;

namespace Aetherlands.Networking
{
    /// <summary>
    /// Authoritative room-level world record owned by the Shared Mode Master Client
    /// and retained across Master Client departures.
    /// </summary>
    public sealed class CoastalWorldRecord : NetworkBehaviour, IStateAuthorityChanged
    {
        public static CoastalWorldRecord Active { get; private set; }

        [SerializeField] private CoastalSessionConfig sessionConfig;

        [Networked] public int Seed { get; set; }
        [Networked] public NetworkString<_32> GeneratorVersion { get; set; }
        [Networked] public NetworkString<_32> ContentVersion { get; set; }
        [Networked] public int ActiveSessionRevision { get; set; }
        [Networked] public NetworkBool IsInitialized { get; set; }

        private readonly MasterClientReconciliationState _reconciliation = new MasterClientReconciliationState();
        private readonly List<string> _reusableWorldKeys = new List<string>(16);

        private int _observedRevision = -1;
        private NetworkString<_32> _observedGeneratorVersion;
        private NetworkString<_32> _observedContentVersion;
        private bool _observedInitialized;

        public CoastalVersionValidationResult LastValidationResult { get; private set; }
        public bool IsInteractionAllowed => LastValidationResult.IsCompatible;
        public IReadOnlyCollection<string> IndexedWorldKeys => _reconciliation.ActiveWorldKeys;

        public event Action<CoastalWorldRecord> OnRecordChanged;
        public event Action<CoastalVersionValidationResult> OnVersionValidated;

        public void Configure(CoastalSessionConfig config)
        {
            sessionConfig = config;
            if (Object != null && Object.IsValid)
            {
                ValidateLocalVersions();
            }
        }

        public override void Spawned()
        {
            Active = this;

            if (Object.HasStateAuthority)
            {
                InitializeIfNeeded();
                ReconcileAsMasterClient();
            }
            else
            {
                _reconciliation.SyncFromAuthoritativeRevision(ActiveSessionRevision);
            }

            RefreshObservedState(forceNotify: true);
        }

        public override void Despawned(NetworkRunner runner, bool hasState)
        {
            if (Active == this)
            {
                Active = null;
            }
        }

        public void StateAuthorityChanged()
        {
            if (Object == null || !Object.IsValid)
            {
                return;
            }

            if (Object.HasStateAuthority)
            {
                InitializeIfNeeded();
                ReconcileAsMasterClient();
            }
            else
            {
                _reconciliation.MarkAuthorityLost();
            }

            RefreshObservedState(forceNotify: true);
        }

        /// <summary>
        /// Called by CoastalSessionRunner when a Master Client transition is observed.
        /// Safely waits for actual HasStateAuthority before mutating any Networked state.
        /// </summary>
        public void NotifyMasterClientChanged()
        {
            if (Object == null || !Object.IsValid)
            {
                return;
            }

            if (!Object.HasStateAuthority)
            {
                _reconciliation.MarkAuthorityLost();
                return;
            }

            InitializeIfNeeded();
            ReconcileAsMasterClient();
            RefreshObservedState(forceNotify: true);
        }

        public override void Render()
        {
            if (Object == null || !Object.IsValid)
            {
                return;
            }

            RefreshObservedState(forceNotify: false);
        }

        public bool TryRegisterMasterAction(string actionKey)
        {
            if (Object == null || !Object.IsValid || !Object.HasStateAuthority)
            {
                return false;
            }

            return _reconciliation.TryRegisterAction(actionKey);
        }

        private void InitializeIfNeeded()
        {
            if (!Object.HasStateAuthority || IsInitialized)
            {
                return;
            }

            int seed = sessionConfig != null ? sessionConfig.worldSeed : 7319;
            string genVer = sessionConfig != null ? sessionConfig.EffectiveGeneratorVersion : "1";
            string contentVer = sessionConfig != null
                ? sessionConfig.EffectiveContentVersion
                : CoastalSessionConfig.DefaultContentVersion;

            Seed = seed;
            GeneratorVersion = genVer;
            ContentVersion = contentVer;
            IsInitialized = true;
        }

        private void ReconcileAsMasterClient()
        {
            if (!Object.HasStateAuthority || Runner == null)
            {
                return;
            }

            _reconciliation.SyncFromAuthoritativeRevision(ActiveSessionRevision);
            CollectLiveWorldKeys(_reusableWorldKeys);

            PlayerRef master = Runner.GetMasterClient();
            int masterId = master.RawEncoded;

            if (_reconciliation.TryReconcile(true, masterId, _reusableWorldKeys))
            {
                ActiveSessionRevision = _reconciliation.ActiveSessionRevision;
            }
        }

        private void CollectLiveWorldKeys(List<string> target)
        {
            target.Clear();
            target.Add("coastal-world-record");

            if (Runner == null)
            {
                return;
            }

            foreach (PlayerRef player in Runner.ActivePlayers)
            {
                if (Runner.TryGetPlayerObject(player, out NetworkObject playerObj) && playerObj != null)
                {
                    target.Add($"player:{player.RawEncoded}");
                }
            }
        }

        private void RefreshObservedState(bool forceNotify)
        {
            NetworkString<_32> currentGen = GeneratorVersion;
            NetworkString<_32> currentContent = ContentVersion;
            int currentRevision = ActiveSessionRevision;
            bool currentInit = IsInitialized;

            bool changed = forceNotify
                || currentRevision != _observedRevision
                || currentInit != _observedInitialized
                || !currentGen.Equals(_observedGeneratorVersion)
                || !currentContent.Equals(_observedContentVersion);

            if (!changed)
            {
                return;
            }

            _observedRevision = currentRevision;
            _observedInitialized = currentInit;
            _observedGeneratorVersion = currentGen;
            _observedContentVersion = currentContent;

            _reconciliation.SyncFromAuthoritativeRevision(currentRevision);
            ValidateLocalVersions();
            OnRecordChanged?.Invoke(this);
        }

        private void ValidateLocalVersions()
        {
            if (!IsInitialized)
            {
                LastValidationResult = new CoastalVersionValidationResult(
                    false,
                    false,
                    false,
                    "Awaiting world record initialization...");
                OnVersionValidated?.Invoke(LastValidationResult);
                return;
            }

            string localGen = sessionConfig != null ? sessionConfig.EffectiveGeneratorVersion : "1";
            string localContent = sessionConfig != null
                ? sessionConfig.EffectiveContentVersion
                : CoastalSessionConfig.DefaultContentVersion;

            LastValidationResult = CoastalVersionGate.Validate(
                localGen,
                localContent,
                GeneratorVersion.ToString(),
                ContentVersion.ToString());

            OnVersionValidated?.Invoke(LastValidationResult);
        }
    }
}
