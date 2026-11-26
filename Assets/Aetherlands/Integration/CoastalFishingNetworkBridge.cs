using Aetherlands.Fishing;
using Aetherlands.Networking;
using UnityEngine;
using FishingStateEnum = Aetherlands.Fishing.FishingState;

namespace Aetherlands.Integration
{
    /// <summary>
    /// Lightweight runtime bridge connecting FishingController / FishingWorldVisuals
    /// with NetworkedTraveler in Photon Fusion 2 Shared Mode.
    /// Enables local fishing input only when Object.HasStateAuthority and drives
    /// remote proxy bobber/line/ripple visuals via FishingWorldVisuals.ApplyRemoteState.
    /// </summary>
    public sealed class CoastalFishingNetworkBridge : MonoBehaviour, IFishingStateProvider, IFishingStateReceiver
    {
        private const float DefaultWaterSurfaceY = -2.0f;

        [SerializeField] private NetworkedTraveler networkedTraveler;
        [SerializeField] private FishingController fishingController;
        [SerializeField] private FishingWorldVisuals worldVisuals;

        private bool _authorityResolved;
        private bool _isLocalAuthority;
        private bool _hasRemoteState;
        private byte _remoteFishingState;
        private Vector3 _remoteBobberTarget;

        public byte FishingState => fishingController != null
            ? (byte)fishingController.State
            : (byte)FishingStateEnum.Idle;

        public Vector3 BobberTarget => fishingController != null
            ? fishingController.BobberWorldPosition
            : transform.position;

        private void Awake()
        {
            CacheReferences();

            if (networkedTraveler != null)
            {
                networkedTraveler.SetFishingBridge(this, this);

                if (fishingController != null)
                {
                    fishingController.enableLocalInput = false;
                }
            }
        }

        private void Update()
        {
            if (networkedTraveler == null)
            {
                return;
            }

            if (networkedTraveler.Object == null || !networkedTraveler.Object.IsValid)
            {
                return;
            }

            bool hasAuthority = networkedTraveler.Object.HasStateAuthority;
            if (!_authorityResolved || _isLocalAuthority != hasAuthority)
            {
                _authorityResolved = true;
                _isLocalAuthority = hasAuthority;
                ApplyAuthorityConfiguration(hasAuthority);
            }

            if (!_isLocalAuthority && _hasRemoteState && IsActiveWorldVisualState(_remoteFishingState))
            {
                ApplyRemoteVisualsNow();
            }
        }

        public void ApplyRemoteFishingState(byte fishingState, Vector3 bobberTarget)
        {
            _remoteFishingState = fishingState;
            _remoteBobberTarget = bobberTarget;
            _hasRemoteState = true;

            if (_authorityResolved && _isLocalAuthority)
            {
                return;
            }

            ApplyRemoteVisualsNow();
        }

        private void OnDisable()
        {
            if (!_isLocalAuthority)
            {
                FishingWorldVisuals visuals = ResolveWorldVisuals();
                if (visuals != null)
                {
                    visuals.HideAll();
                }
            }
        }

        private void OnDestroy()
        {
            if (networkedTraveler != null)
            {
                networkedTraveler.SetFishingBridge(null, null);
            }
        }

        private void CacheReferences()
        {
            if (networkedTraveler == null)
            {
                networkedTraveler = GetComponent<NetworkedTraveler>();
            }

            if (fishingController == null)
            {
                fishingController = GetComponent<FishingController>();
            }

            if (worldVisuals == null)
            {
                worldVisuals = GetComponentInChildren<FishingWorldVisuals>();
            }
        }

        private void ApplyAuthorityConfiguration(bool hasAuthority)
        {
            if (fishingController == null)
            {
                return;
            }

            if (hasAuthority)
            {
                if (fishingController.overlayView == null)
                {
                    FishingOverlayView existingOverlay = GetComponentInChildren<FishingOverlayView>();
                    if (existingOverlay == null)
                    {
                        GameObject overlayGo = new GameObject("FishingOverlayView");
                        overlayGo.transform.SetParent(transform, false);
                        existingOverlay = overlayGo.AddComponent<FishingOverlayView>();
                    }

                    fishingController.overlayView = existingOverlay;
                }

                fishingController.enableLocalInput = true;
            }
            else
            {
                fishingController.enableLocalInput = false;
                if (fishingController.overlayView != null)
                {
                    fishingController.overlayView.HideAll();
                }

                if (_hasRemoteState)
                {
                    ApplyRemoteVisualsNow();
                }
            }
        }

        private void ApplyRemoteVisualsNow()
        {
            FishingWorldVisuals visuals = ResolveWorldVisuals();
            if (visuals == null)
            {
                return;
            }

            Vector3 rodTip = fishingController != null
                ? fishingController.GetRodTipWorldPosition()
                : transform.position + Vector3.up * 1.05f;

            float waterSurfaceY = fishingController != null && fishingController.config != null
                ? fishingController.config.waterSurfaceY
                : DefaultWaterSurfaceY;

            visuals.ApplyRemoteState(_remoteFishingState, rodTip, _remoteBobberTarget, waterSurfaceY);
        }

        private FishingWorldVisuals ResolveWorldVisuals()
        {
            if (worldVisuals != null)
            {
                return worldVisuals;
            }

            if (fishingController != null && fishingController.worldVisuals != null)
            {
                worldVisuals = fishingController.worldVisuals;
                return worldVisuals;
            }

            worldVisuals = GetComponentInChildren<FishingWorldVisuals>();
            return worldVisuals;
        }

        private static bool IsActiveWorldVisualState(byte rawState)
        {
            FishingStateEnum state = (FishingStateEnum)rawState;
            return state == FishingStateEnum.Casting
                || state == FishingStateEnum.WaitingForBite
                || state == FishingStateEnum.HookedMinigame;
        }
    }
}
