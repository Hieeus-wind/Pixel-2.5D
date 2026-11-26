using System;

namespace Aetherlands.Fishing
{
    public readonly struct FishSpec
    {
        public readonly string Id;
        public readonly string DisplayName;
        public readonly float Difficulty;
        public readonly float Speed;
        public readonly FishBehaviorPattern Behavior;
        public readonly float MinSizeCm;
        public readonly float MaxSizeCm;
        public readonly FishRarity Rarity;
        public readonly float SpawnWeight;

        public FishSpec(
            string id,
            string displayName,
            float difficulty,
            float speed,
            FishBehaviorPattern behavior,
            float minSizeCm,
            float maxSizeCm,
            FishRarity rarity,
            float spawnWeight)
        {
            Id = string.IsNullOrEmpty(id) ? "coastal_fish" : id;
            DisplayName = string.IsNullOrEmpty(displayName) ? Id : displayName;
            Difficulty = Math.Clamp(difficulty, 5f, 100f);
            Speed = Math.Clamp(speed, 0.25f, 3f);
            Behavior = behavior;
            MinSizeCm = Math.Max(1f, minSizeCm);
            MaxSizeCm = Math.Max(MinSizeCm, maxSizeCm);
            Rarity = rarity;
            SpawnWeight = Math.Max(0.01f, spawnWeight);
        }
    }

    public readonly struct FishingRules
    {
        public readonly float ChargeRate;
        public readonly float CastTravelDuration;
        public readonly float MinBiteWaitSeconds;
        public readonly float MaxBiteWaitSeconds;
        public readonly float BiteReactionWindowSeconds;
        public readonly float ResultDisplaySeconds;

        public readonly float CatchBarSize;
        public readonly float BarGravity;
        public readonly float BarThrust;
        public readonly float BarMaxSpeed;
        public readonly float BarBottomBounce;
        public readonly float BarTopBounce;
        public readonly float BarLinearDamping;

        public readonly float InitialProgress;
        public readonly float ProgressGainRate;
        public readonly float ProgressDrainRate;
        public readonly float FishRadius;

        public FishingRules(
            float chargeRate = 1.15f,
            float castTravelDuration = 0.45f,
            float minBiteWaitSeconds = 1.8f,
            float maxBiteWaitSeconds = 4.5f,
            float biteReactionWindowSeconds = 1.35f,
            float resultDisplaySeconds = 2.4f,
            float catchBarSize = 0.24f,
            float barGravity = 1.35f,
            float barThrust = 2.75f,
            float barMaxSpeed = 1.85f,
            float barBottomBounce = 0.42f,
            float barTopBounce = 0.15f,
            float barLinearDamping = 0.35f,
            float initialProgress = 0.28f,
            float progressGainRate = 0.22f,
            float progressDrainRate = 0.17f,
            float fishRadius = 0.045f)
        {
            ChargeRate = Math.Max(0.1f, chargeRate);
            CastTravelDuration = Math.Max(0.05f, castTravelDuration);
            MinBiteWaitSeconds = Math.Max(0.1f, minBiteWaitSeconds);
            MaxBiteWaitSeconds = Math.Max(MinBiteWaitSeconds, maxBiteWaitSeconds);
            BiteReactionWindowSeconds = Math.Max(0.2f, biteReactionWindowSeconds);
            ResultDisplaySeconds = Math.Max(0.2f, resultDisplaySeconds);

            CatchBarSize = Math.Clamp(catchBarSize, 0.08f, 0.6f);
            BarGravity = Math.Max(0.1f, barGravity);
            BarThrust = Math.Max(BarGravity * 1.1f, barThrust);
            BarMaxSpeed = Math.Max(0.2f, barMaxSpeed);
            BarBottomBounce = Math.Clamp(barBottomBounce, 0f, 0.9f);
            BarTopBounce = Math.Clamp(barTopBounce, 0f, 0.9f);
            BarLinearDamping = Math.Clamp(barLinearDamping, 0f, 5f);

            InitialProgress = Math.Clamp(initialProgress, 0.05f, 0.8f);
            ProgressGainRate = Math.Max(0.02f, progressGainRate);
            ProgressDrainRate = Math.Max(0.02f, progressDrainRate);
            FishRadius = Math.Clamp(fishRadius, 0.01f, 0.15f);
        }

        // Pass a named argument so C# invokes the parameterized constructor instead of zero-initializing the struct.
        public static FishingRules Default => new FishingRules(chargeRate: 1.15f);
    }
}
