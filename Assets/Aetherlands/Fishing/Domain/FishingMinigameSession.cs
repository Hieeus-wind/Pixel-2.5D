using System;

namespace Aetherlands.Fishing
{
    /// <summary>
    /// Pure C# Stardew Valley-style vertical reel minigame simulation.
    /// Runs deterministically without Unity scene, MonoBehaviour, or runner dependencies.
    /// </summary>
    public sealed class FishingMinigameSession
    {
        readonly FishingRules rules;
        readonly IFishingRandom rng;

        float fishTarget;
        float fishVelocity;
        float retargetTimer;
        float dartPauseTimer;

        public FishSpec Fish { get; }
        public float BarSize => rules.CatchBarSize;
        public float BarBottom { get; private set; }
        public float BarTop => BarBottom + BarSize;
        public float BarCenter => BarBottom + BarSize * 0.5f;
        public float BarVelocity { get; private set; }

        public float FishPosition { get; private set; }
        public float FishTarget => fishTarget;
        public float FishVelocity => fishVelocity;
        public float FishRadius => rules.FishRadius;

        public float CatchProgress { get; private set; }
        public bool IsOverlapping { get; private set; }
        public bool IsPerfect { get; private set; }
        public bool IsCompleted { get; private set; }
        public bool IsCaught { get; private set; }
        public bool IsEscaped { get; private set; }
        public float ElapsedSeconds { get; private set; }

        public FishingMinigameSession(FishSpec fish, FishingRules rules, IFishingRandom rng)
        {
            Fish = fish;
            this.rules = rules.CatchBarSize > 0f ? rules : FishingRules.Default;
            this.rng = rng ?? new SeededFishingRandom(7319);

            // Start bar near bottom and fish centered inside the bar so perfect catch is achievable.
            BarBottom = 0f;
            BarVelocity = 0f;
            FishPosition = Math.Clamp(BarSize * 0.5f, this.rules.FishRadius, 1f - this.rules.FishRadius);
            fishTarget = FishPosition;
            fishVelocity = 0f;
            retargetTimer = 0.25f;
            dartPauseTimer = 0f;

            CatchProgress = this.rules.InitialProgress;
            IsOverlapping = true;
            IsPerfect = true;
            IsCompleted = false;
            IsCaught = false;
            IsEscaped = false;
            ElapsedSeconds = 0f;
        }

        public void Step(bool isHoldingReel, float deltaSeconds)
        {
            if (IsCompleted || deltaSeconds <= 0f) return;

            // Sub-step large deltas for numerical stability and deterministic replay.
            const float maxStep = 1f / 60f;
            float remaining = deltaSeconds;
            while (remaining > 0f && !IsCompleted)
            {
                float dt = remaining > maxStep ? maxStep : remaining;
                remaining -= dt;
                StepFixed(isHoldingReel, dt);
            }
        }

        void StepFixed(bool isHoldingReel, float dt)
        {
            ElapsedSeconds += dt;
            StepBar(isHoldingReel, dt);
            StepFish(dt);
            StepProgress(dt);
        }

        void StepBar(bool isHoldingReel, float dt)
        {
            float accel = isHoldingReel ? (rules.BarThrust - rules.BarGravity) : -rules.BarGravity;
            BarVelocity += accel * dt;

            float damping = Math.Max(0f, 1f - rules.BarLinearDamping * dt);
            BarVelocity *= damping;
            BarVelocity = Math.Clamp(BarVelocity, -rules.BarMaxSpeed, rules.BarMaxSpeed);

            BarBottom += BarVelocity * dt;

            float maxBottom = 1f - BarSize;
            if (BarBottom <= 0f)
            {
                BarBottom = 0f;
                if (BarVelocity < 0f)
                {
                    if (!isHoldingReel && Math.Abs(BarVelocity) > 0.08f)
                    {
                        BarVelocity = -BarVelocity * rules.BarBottomBounce;
                    }
                    else
                    {
                        BarVelocity = 0f;
                    }
                }
            }
            else if (BarBottom >= maxBottom)
            {
                BarBottom = maxBottom;
                if (BarVelocity > 0f)
                {
                    if (Math.Abs(BarVelocity) > 0.08f)
                    {
                        BarVelocity = -BarVelocity * rules.BarTopBounce;
                    }
                    else
                    {
                        BarVelocity = 0f;
                    }
                }
            }
        }

        void StepFish(float dt)
        {
            float minPos = rules.FishRadius;
            float maxPos = 1f - rules.FishRadius;
            float difficultyNorm = Math.Clamp((Fish.Difficulty - 10f) / 90f, 0f, 1f);
            float speedScale = Fish.Speed * (0.65f + difficultyNorm * 0.95f);

            retargetTimer -= dt;
            if (dartPauseTimer > 0f)
            {
                dartPauseTimer -= dt;
            }

            if (retargetTimer <= 0f)
            {
                PickNextFishTarget(minPos, maxPos, difficultyNorm);
            }

            float toTarget = fishTarget - FishPosition;
            float maxSpeed;
            float accel;
            float drag;
            float biasAccel = 0f;

            switch (Fish.Behavior)
            {
                case FishBehaviorPattern.Smooth:
                    maxSpeed = 0.36f * speedScale;
                    accel = 1.65f * speedScale;
                    drag = 3.2f;
                    break;

                case FishBehaviorPattern.Sinker:
                    maxSpeed = 0.56f * speedScale;
                    accel = (toTarget < 0f ? 4.0f : 1.9f) * speedScale;
                    drag = 2.4f;
                    biasAccel = -(0.65f + 0.45f * difficultyNorm) * Fish.Speed;
                    break;

                case FishBehaviorPattern.Floater:
                    maxSpeed = 0.56f * speedScale;
                    accel = (toTarget > 0f ? 4.0f : 1.9f) * speedScale;
                    drag = 2.4f;
                    biasAccel = (0.65f + 0.45f * difficultyNorm) * Fish.Speed;
                    break;

                case FishBehaviorPattern.Dart:
                    if (dartPauseTimer > 0f)
                    {
                        maxSpeed = 0.14f * speedScale;
                        accel = 1.2f * speedScale;
                        drag = 6.5f;
                    }
                    else
                    {
                        maxSpeed = 1.15f * speedScale;
                        accel = 7.5f * speedScale;
                        drag = 2.8f;
                    }
                    break;

                case FishBehaviorPattern.Mixed:
                default:
                    maxSpeed = 0.58f * speedScale;
                    accel = 3.0f * speedScale;
                    drag = 2.8f;
                    break;
            }

            float desiredAccel = Math.Clamp(toTarget * 5.5f, -1f, 1f) * accel + biasAccel;
            fishVelocity += desiredAccel * dt;
            fishVelocity *= Math.Max(0f, 1f - drag * dt);
            fishVelocity = Math.Clamp(fishVelocity, -maxSpeed, maxSpeed);

            FishPosition += fishVelocity * dt;
            if (FishPosition < minPos)
            {
                FishPosition = minPos;
                if (fishVelocity < 0f) fishVelocity = -fishVelocity * 0.25f;
            }
            else if (FishPosition > maxPos)
            {
                FishPosition = maxPos;
                if (fishVelocity > 0f) fishVelocity = -fishVelocity * 0.25f;
            }
        }

        void PickNextFishTarget(float minPos, float maxPos, float difficultyNorm)
        {
            float span = maxPos - minPos;
            float baseInterval = Math.Clamp(1.35f - difficultyNorm * 0.72f, 0.38f, 1.5f) / Math.Max(0.5f, Fish.Speed);

            switch (Fish.Behavior)
            {
                case FishBehaviorPattern.Smooth:
                {
                    float maxDelta = span * (0.28f + difficultyNorm * 0.22f);
                    float delta = rng.Range(-maxDelta, maxDelta);
                    fishTarget = Math.Clamp(FishPosition + delta, minPos, maxPos);
                    retargetTimer = baseInterval * rng.Range(1.05f, 1.55f);
                    break;
                }

                case FishBehaviorPattern.Sinker:
                {
                    // Bias strongly toward the lower region while occasionally surging upward.
                    float u = rng.NextFloat();
                    float biased = u < 0.80f ? (u / 0.80f) * 0.40f : 0.40f + ((u - 0.80f) / 0.20f) * 0.38f;
                    fishTarget = minPos + biased * span;
                    retargetTimer = baseInterval * rng.Range(0.75f, 1.25f);
                    break;
                }

                case FishBehaviorPattern.Floater:
                {
                    // Bias strongly toward the upper region while occasionally dipping downward.
                    float u = rng.NextFloat();
                    float biased = u < 0.80f ? 1f - (u / 0.80f) * 0.40f : 0.60f - ((u - 0.80f) / 0.20f) * 0.38f;
                    fishTarget = minPos + biased * span;
                    retargetTimer = baseInterval * rng.Range(0.75f, 1.25f);
                    break;
                }

                case FishBehaviorPattern.Dart:
                {
                    // Jump far from the current position, then pause briefly before darting again.
                    float jump = span * rng.Range(0.35f, 0.72f);
                    bool goUp = FishPosition < 0.5f ? rng.NextFloat() < 0.78f : rng.NextFloat() < 0.22f;
                    fishTarget = Math.Clamp(FishPosition + (goUp ? jump : -jump), minPos, maxPos);
                    dartPauseTimer = rng.Range(0.10f, 0.26f);
                    retargetTimer = baseInterval * rng.Range(0.55f, 1.0f) + dartPauseTimer;
                    break;
                }

                case FishBehaviorPattern.Mixed:
                default:
                {
                    float maxDelta = span * (0.35f + difficultyNorm * 0.55f);
                    float delta = rng.Range(-maxDelta, maxDelta);
                    fishTarget = Math.Clamp(FishPosition + delta, minPos, maxPos);
                    retargetTimer = baseInterval * rng.Range(0.7f, 1.3f);
                    break;
                }
            }
        }

        void StepProgress(float dt)
        {
            IsOverlapping = FishPosition + rules.FishRadius >= BarBottom &&
                            FishPosition - rules.FishRadius <= BarTop;

            if (IsOverlapping)
            {
                CatchProgress = Math.Min(1f, CatchProgress + rules.ProgressGainRate * dt);
                if (CatchProgress >= 1f)
                {
                    CatchProgress = 1f;
                    IsCompleted = true;
                    IsCaught = true;
                    IsEscaped = false;
                }
            }
            else
            {
                IsPerfect = false;
                CatchProgress = Math.Max(0f, CatchProgress - rules.ProgressDrainRate * dt);
                if (CatchProgress <= 0f)
                {
                    CatchProgress = 0f;
                    IsCompleted = true;
                    IsCaught = false;
                    IsEscaped = true;
                }
            }
        }
    }
}
