using System;
using System.Collections.Generic;
using Aetherlands.Coast;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Aetherlands.Fishing
{
    /// <summary>
    /// Local gameplay controller for Stardew Valley-style coastal fishing.
    /// Attaches to the traveler (or a sibling with references wired), blocks TravelerMotor
    /// while actively fishing, drives world visuals and pixel-art UI overlay, and exposes
    /// clean read-only state and events for multiplayer synchronization.
    /// </summary>
    public sealed class FishingController : MonoBehaviour
    {
        [Header("Configuration")]
        public FishingConfig config;
        public int randomSeed = 0;

        [Header("Scene References (Optional — auto-resolved if null)")]
        public TravelerMotor travelerMotor;
        public FishingWorldVisuals worldVisuals;
        public FishingOverlayView overlayView;

        [Header("Behavior")]
        public bool enableLocalInput = true;
        public bool blockTravelerMotorWhileFishing = true;

        FishingConfig runtimeFallbackConfig;
        FishingFlowMachine machine;
        List<FishSpec> cachedSpecs;

        Vector3 facingDirection = Vector3.back;
        Vector3 previousPosition;
        Vector3 castOriginWorld;
        Vector3 castTargetWorld;
        Vector3 currentBobberWorld;
        bool canCastNow;

        bool wasHoldingAction;
        bool motorWasEnabledBeforeFishing;
        bool motorLockedByFishing;

        public FishingState State => machine != null ? machine.State : FishingState.Idle;
        public bool IsFishing => machine != null && machine.IsFishing;
        public float CastPower => machine != null ? machine.CastPower : 0f;
        public Vector3 BobberWorldPosition => currentBobberWorld;
        public string LastCaughtFishId => machine != null && machine.HasLastCatch ? machine.LastCatch.Species.Id : string.Empty;
        public int CatchCount => machine != null ? machine.CatchCount : 0;
        public bool LastCatchWasPerfect => machine != null && machine.HasLastCatch && machine.LastCatch.IsPerfect;
        public bool IsBiteCueActive => machine != null && machine.IsBiteCueActive;
        public bool CanCastNow => canCastNow;
        public Vector3 FacingDirection => facingDirection;
        public FishingMinigameSession ActiveMinigame => machine != null ? machine.ActiveMinigame : null;

        public event Action<FishingState, FishingState> OnStateChanged;
        public event Action<CaughtFishResult> OnFishCaught;
        public event Action<FishSpec> OnFishEscaped;

        FishingConfig ActiveConfig
        {
            get
            {
                if (config != null) return config;
                if (runtimeFallbackConfig == null)
                {
                    runtimeFallbackConfig = FishingConfig.CreateRuntimeDefault();
                }

                return runtimeFallbackConfig;
            }
        }

        void Awake()
        {
            if (travelerMotor == null)
            {
                travelerMotor = GetComponent<TravelerMotor>();
            }

            if (worldVisuals == null)
            {
                worldVisuals = GetComponentInChildren<FishingWorldVisuals>();
                if (worldVisuals == null)
                {
                    var visualsGo = new GameObject("FishingWorldVisuals");
                    visualsGo.transform.SetParent(transform, false);
                    worldVisuals = visualsGo.AddComponent<FishingWorldVisuals>();
                }
            }

            if (overlayView == null && enableLocalInput)
            {
                overlayView = GetComponentInChildren<FishingOverlayView>();
                if (overlayView == null)
                {
                    var overlayGo = new GameObject("FishingOverlayView");
                    overlayGo.transform.SetParent(transform, false);
                    overlayView = overlayGo.AddComponent<FishingOverlayView>();
                }
            }

            previousPosition = transform.position;
            InitializeMachine();
        }

        void InitializeMachine()
        {
            FishingConfig cfg = ActiveConfig;
            cachedSpecs = cfg.BuildSpecList();
            int seed = randomSeed != 0 ? randomSeed : Environment.TickCount ^ GetInstanceID();
            machine = new FishingFlowMachine(cfg.ToRules(), cachedSpecs, new SeededFishingRandom(seed));
            machine.OnStateChanged += HandleStateChanged;
            machine.OnFishCaught += HandleFishCaught;
            machine.OnFishEscaped += HandleFishEscaped;
        }

        void Update()
        {
            if (machine == null) return;

            UpdateFacingDirection();

            // Remote proxy travelers do not run local water detection, input, or local presentation;
            // their world visuals are driven via FishingWorldVisuals.ApplyRemoteState(...).
            if (!enableLocalInput)
            {
                return;
            }

            FishingConfig cfg = ActiveConfig;
            if (machine.State == FishingState.Idle)
            {
                canCastNow = CoastalWaterSpotDetector.TryFindWaterSpot(
                    transform.position,
                    facingDirection,
                    0.5f,
                    cfg,
                    out _);
            }

            ReadLocalInput(out bool isHolding, out bool pressedThisFrame, out bool releasedThisFrame, out bool cancelPressed);

            if (cancelPressed && machine.IsFishing)
            {
                machine.Cancel();
            }
            else
            {
                switch (machine.State)
                {
                    case FishingState.Idle:
                        if (pressedThisFrame && canCastNow)
                        {
                            machine.BeginCharge(true);
                        }
                        break;

                    case FishingState.ChargingCast:
                        if (releasedThisFrame)
                        {
                            if (CoastalWaterSpotDetector.TryFindWaterSpot(
                                    transform.position,
                                    facingDirection,
                                    machine.CastPower,
                                    cfg,
                                    out castTargetWorld))
                            {
                                castOriginWorld = GetRodTipWorldPosition();
                                currentBobberWorld = castOriginWorld;
                                machine.ReleaseCast();
                            }
                            else
                            {
                                machine.Cancel();
                            }
                        }
                        break;

                    case FishingState.WaitingForBite:
                        if (pressedThisFrame)
                        {
                            machine.PressHook();
                        }
                        break;
                }
            }

            machine.Step(isHolding, Time.deltaTime);
            UpdateBobberPosition(cfg);
            UpdatePresentation(cfg);
        }

        void ReadLocalInput(out bool isHolding, out bool pressedThisFrame, out bool releasedThisFrame, out bool cancelPressed)
        {
            isHolding = false;
            cancelPressed = false;

            var keyboard = Keyboard.current;
            if (keyboard != null)
            {
                if (keyboard.spaceKey.isPressed || keyboard.eKey.isPressed)
                {
                    isHolding = true;
                }

                if (keyboard.escapeKey.wasPressedThisFrame)
                {
                    cancelPressed = true;
                }
            }

            var mouse = Mouse.current;
            if (mouse != null)
            {
                if (mouse.leftButton.isPressed)
                {
                    isHolding = true;
                }

                if (mouse.rightButton.wasPressedThisFrame)
                {
                    cancelPressed = true;
                }
            }

            pressedThisFrame = isHolding && !wasHoldingAction;
            releasedThisFrame = !isHolding && wasHoldingAction;
            wasHoldingAction = isHolding;
        }

        void UpdateFacingDirection()
        {
            if (machine != null && machine.IsFishing)
            {
                previousPosition = transform.position;
                return;
            }

            if (enableLocalInput)
            {
                var keys = Keyboard.current;
                if (keys != null)
                {
                    float ix = 0f;
                    float iz = 0f;
                    if (keys.aKey.isPressed || keys.leftArrowKey.isPressed) ix -= 1f;
                    if (keys.dKey.isPressed || keys.rightArrowKey.isPressed) ix += 1f;
                    if (keys.sKey.isPressed || keys.downArrowKey.isPressed) iz -= 1f;
                    if (keys.wKey.isPressed || keys.upArrowKey.isPressed) iz += 1f;

                    if (ix * ix + iz * iz > 0.01f)
                    {
                        facingDirection = Mathf.Abs(ix) > Mathf.Abs(iz)
                            ? (ix > 0f ? Vector3.right : Vector3.left)
                            : (iz > 0f ? Vector3.forward : Vector3.back);
                        previousPosition = transform.position;
                        return;
                    }
                }
            }

            Vector3 delta = transform.position - previousPosition;
            delta.y = 0f;
            if (delta.sqrMagnitude > 0.0001f)
            {
                facingDirection = Mathf.Abs(delta.x) > Mathf.Abs(delta.z)
                    ? (delta.x > 0f ? Vector3.right : Vector3.left)
                    : (delta.z > 0f ? Vector3.forward : Vector3.back);
            }

            previousPosition = transform.position;
        }

        void UpdateBobberPosition(FishingConfig cfg)
        {
            switch (machine.State)
            {
                case FishingState.Casting:
                {
                    float t = machine.CastProgress;
                    Vector3 flat = Vector3.Lerp(castOriginWorld, castTargetWorld, t);
                    float arc = 4f * t * (1f - t) * cfg.castArcHeight;
                    currentBobberWorld = new Vector3(flat.x, flat.y + arc, flat.z);
                    break;
                }

                case FishingState.WaitingForBite:
                case FishingState.HookedMinigame:
                    currentBobberWorld = castTargetWorld;
                    break;

                default:
                    currentBobberWorld = GetRodTipWorldPosition();
                    break;
            }
        }

        void UpdatePresentation(FishingConfig cfg)
        {
            if (worldVisuals != null)
            {
                worldVisuals.UpdateVisuals(
                    machine.State,
                    GetRodTipWorldPosition(),
                    currentBobberWorld,
                    transform.position + Vector3.up * 1.65f,
                    cfg.waterSurfaceY,
                    machine.IsBiteCueActive,
                    Time.time);
            }

            if (overlayView != null && enableLocalInput)
            {
                overlayView.UpdatePrompt(machine.State == FishingState.Idle && canCastNow);
                overlayView.UpdateCastGauge(machine.State == FishingState.ChargingCast, machine.CastPower);

                if (machine.State == FishingState.HookedMinigame && machine.ActiveMinigame != null)
                {
                    FishSpeciesDefinition def = cfg.FindSpeciesById(machine.ActiveMinigame.Fish.Id);
                    overlayView.UpdateMinigame(machine.ActiveMinigame, def);
                }
                else
                {
                    overlayView.UpdateMinigame(null, null);
                }
            }
        }

        public Vector3 GetRodTipWorldPosition()
        {
            return transform.position + new Vector3(facingDirection.x * 0.45f, 1.05f, facingDirection.z * 0.45f);
        }

        void HandleStateChanged(FishingState previous, FishingState next)
        {
            UpdateMotorLock(next != FishingState.Idle);

            if (overlayView != null && enableLocalInput && next == FishingState.Idle)
            {
                overlayView.HideToast();
            }

            OnStateChanged?.Invoke(previous, next);
        }

        void HandleFishCaught(CaughtFishResult result)
        {
            if (overlayView != null && enableLocalInput)
            {
                FishSpeciesDefinition def = ActiveConfig.FindSpeciesById(result.Species.Id);
                overlayView.ShowCaughtToast(result, def, machine.CatchCount);
            }

            OnFishCaught?.Invoke(result);
        }

        void HandleFishEscaped(FishSpec species)
        {
            if (overlayView != null && enableLocalInput)
            {
                overlayView.ShowEscapedToast(species, machine.CatchCount);
            }

            OnFishEscaped?.Invoke(species);
        }

        void UpdateMotorLock(bool shouldLock)
        {
            if (!blockTravelerMotorWhileFishing || travelerMotor == null) return;

            if (shouldLock && !motorLockedByFishing)
            {
                motorWasEnabledBeforeFishing = travelerMotor.enabled;
                if (motorWasEnabledBeforeFishing)
                {
                    travelerMotor.enabled = false;
                }

                motorLockedByFishing = true;
            }
            else if (!shouldLock && motorLockedByFishing)
            {
                if (motorWasEnabledBeforeFishing)
                {
                    travelerMotor.enabled = true;
                }

                motorLockedByFishing = false;
            }
        }

        void OnDisable()
        {
            UpdateMotorLock(false);
            if (worldVisuals != null) worldVisuals.HideAll();
            if (overlayView != null) overlayView.HideAll();
        }

        void OnDestroy()
        {
            UpdateMotorLock(false);

            if (machine != null)
            {
                machine.OnStateChanged -= HandleStateChanged;
                machine.OnFishCaught -= HandleFishCaught;
                machine.OnFishEscaped -= HandleFishEscaped;
            }

            if (runtimeFallbackConfig != null)
            {
                if (runtimeFallbackConfig.species != null)
                {
                    for (int i = 0; i < runtimeFallbackConfig.species.Count; i++)
                    {
                        if (runtimeFallbackConfig.species[i] != null)
                        {
                            Destroy(runtimeFallbackConfig.species[i]);
                        }
                    }
                }

                Destroy(runtimeFallbackConfig);
                runtimeFallbackConfig = null;
            }
        }
    }
}
