using System;
using System.Collections.Generic;

namespace Aetherlands.Fishing
{
    public readonly struct CaughtFishResult
    {
        public readonly FishSpec Species;
        public readonly float SizeCm;
        public readonly bool IsPerfect;

        public CaughtFishResult(FishSpec species, float sizeCm, bool isPerfect)
        {
            Species = species;
            SizeCm = sizeCm;
            IsPerfect = isPerfect;
        }
    }

    /// <summary>
    /// Pure C# state machine orchestrating the full fishing lifecycle:
    /// Idle -> ChargingCast -> Casting -> WaitingForBite -> HookedMinigame -> Caught/Escaped -> Idle.
    /// </summary>
    public sealed class FishingFlowMachine
    {
        readonly FishingRules rules;
        readonly IFishingRandom rng;
        readonly IReadOnlyList<FishSpec> catalog;

        float chargeDirection = 1f;
        float stateTimer;
        float biteWaitDuration;
        bool biteCueActive;
        FishSpec hookedSpecies;

        public FishingState State { get; private set; }
        public bool IsFishing => State != FishingState.Idle;
        public float CastPower { get; private set; }
        public float CastProgress { get; private set; }
        public bool IsBiteCueActive => State == FishingState.WaitingForBite && biteCueActive;
        public FishingMinigameSession ActiveMinigame { get; private set; }
        public CaughtFishResult LastCatch { get; private set; }
        public bool HasLastCatch { get; private set; }
        public int CatchCount { get; private set; }

        public event Action<FishingState, FishingState> OnStateChanged;
        public event Action<CaughtFishResult> OnFishCaught;
        public event Action<FishSpec> OnFishEscaped;

        public FishingFlowMachine(FishingRules rules, IReadOnlyList<FishSpec> catalog, IFishingRandom rng)
        {
            this.rules = rules.CatchBarSize > 0f ? rules : FishingRules.Default;
            this.catalog = catalog;
            this.rng = rng ?? new SeededFishingRandom(7319);
            State = FishingState.Idle;
        }

        public bool BeginCharge(bool canCastToWater)
        {
            if (State != FishingState.Idle || !canCastToWater) return false;

            CastPower = 0f;
            CastProgress = 0f;
            chargeDirection = 1f;
            biteCueActive = false;
            ActiveMinigame = null;
            TransitionTo(FishingState.ChargingCast);
            return true;
        }

        public bool ReleaseCast()
        {
            if (State != FishingState.ChargingCast) return false;

            CastPower = Math.Clamp(CastPower, 0.12f, 1f);
            CastProgress = 0f;
            stateTimer = 0f;
            TransitionTo(FishingState.Casting);
            return true;
        }

        public bool PressHook()
        {
            if (State != FishingState.WaitingForBite) return false;

            if (biteCueActive)
            {
                biteCueActive = false;
                hookedSpecies = RollSpecies();
                ActiveMinigame = new FishingMinigameSession(hookedSpecies, rules, rng);
                TransitionTo(FishingState.HookedMinigame);
                return true;
            }

            // Reeling in early before a bite cancels cleanly back to Idle.
            Cancel();
            return false;
        }

        public void Cancel()
        {
            if (State == FishingState.Idle) return;

            biteCueActive = false;
            ActiveMinigame = null;
            CastProgress = 0f;
            stateTimer = 0f;
            TransitionTo(FishingState.Idle);
        }

        public void Step(bool isHoldingAction, float deltaSeconds)
        {
            if (deltaSeconds <= 0f) return;

            switch (State)
            {
                case FishingState.ChargingCast:
                {
                    CastPower += chargeDirection * rules.ChargeRate * deltaSeconds;
                    if (CastPower >= 1f)
                    {
                        CastPower = 1f;
                        chargeDirection = -1f;
                    }
                    else if (CastPower <= 0f)
                    {
                        CastPower = 0f;
                        chargeDirection = 1f;
                    }
                    break;
                }

                case FishingState.Casting:
                {
                    stateTimer += deltaSeconds;
                    CastProgress = Math.Clamp(stateTimer / rules.CastTravelDuration, 0f, 1f);
                    if (CastProgress >= 1f)
                    {
                        stateTimer = 0f;
                        biteCueActive = false;
                        // Higher cast power slightly shortens bite wait time as a subtle reward.
                        float castBonus = 1f - CastPower * 0.2f;
                        biteWaitDuration = rng.Range(rules.MinBiteWaitSeconds, rules.MaxBiteWaitSeconds) * castBonus;
                        TransitionTo(FishingState.WaitingForBite);
                    }
                    break;
                }

                case FishingState.WaitingForBite:
                {
                    stateTimer += deltaSeconds;
                    if (!biteCueActive)
                    {
                        if (stateTimer >= biteWaitDuration)
                        {
                            biteCueActive = true;
                            stateTimer = 0f;
                        }
                    }
                    else if (stateTimer >= rules.BiteReactionWindowSeconds)
                    {
                        biteCueActive = false;
                        stateTimer = 0f;
                        hookedSpecies = RollSpecies();
                        TransitionTo(FishingState.Escaped);
                        OnFishEscaped?.Invoke(hookedSpecies);
                    }
                    break;
                }

                case FishingState.HookedMinigame:
                {
                    if (ActiveMinigame == null)
                    {
                        TransitionTo(FishingState.Idle);
                        break;
                    }

                    ActiveMinigame.Step(isHoldingAction, deltaSeconds);
                    if (ActiveMinigame.IsCompleted)
                    {
                        stateTimer = 0f;
                        if (ActiveMinigame.IsCaught)
                        {
                            float sizeRoll = rng.NextFloat();
                            if (ActiveMinigame.IsPerfect)
                            {
                                // Perfect catch biases size toward upper half of species range.
                                sizeRoll = 0.5f + sizeRoll * 0.5f;
                            }
                            float sizeCm = hookedSpecies.MinSizeCm +
                                           (hookedSpecies.MaxSizeCm - hookedSpecies.MinSizeCm) * sizeRoll;
                            sizeCm = (float)Math.Round(sizeCm, 1);

                            LastCatch = new CaughtFishResult(hookedSpecies, sizeCm, ActiveMinigame.IsPerfect);
                            HasLastCatch = true;
                            CatchCount++;
                            TransitionTo(FishingState.Caught);
                            OnFishCaught?.Invoke(LastCatch);
                        }
                        else
                        {
                            TransitionTo(FishingState.Escaped);
                            OnFishEscaped?.Invoke(hookedSpecies);
                        }
                    }
                    break;
                }

                case FishingState.Caught:
                case FishingState.Escaped:
                {
                    stateTimer += deltaSeconds;
                    if (stateTimer >= rules.ResultDisplaySeconds)
                    {
                        ActiveMinigame = null;
                        stateTimer = 0f;
                        TransitionTo(FishingState.Idle);
                    }
                    break;
                }
            }
        }

        FishSpec RollSpecies()
        {
            if (catalog == null || catalog.Count == 0)
            {
                return new FishSpec("anchovy", "Anchovy", 24f, 0.85f, FishBehaviorPattern.Smooth, 12f, 22f, FishRarity.Common, 1f);
            }

            float totalWeight = 0f;
            for (int i = 0; i < catalog.Count; i++)
            {
                // Longer casts mildly boost rarer fish odds.
                float rarityBoost = 1f + CastPower * 0.35f * (int)catalog[i].Rarity;
                totalWeight += Math.Max(0.01f, catalog[i].SpawnWeight) * rarityBoost;
            }

            float pick = rng.Range(0f, totalWeight);
            float running = 0f;
            for (int i = 0; i < catalog.Count; i++)
            {
                float rarityBoost = 1f + CastPower * 0.35f * (int)catalog[i].Rarity;
                running += Math.Max(0.01f, catalog[i].SpawnWeight) * rarityBoost;
                if (pick <= running) return catalog[i];
            }

            return catalog[catalog.Count - 1];
        }

        void TransitionTo(FishingState next)
        {
            if (State == next) return;
            FishingState previous = State;
            State = next;
            OnStateChanged?.Invoke(previous, next);
        }
    }
}
