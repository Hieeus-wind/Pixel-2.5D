using System;
using System.Collections.Generic;
using UnityEngine;

namespace Aetherlands.Fishing
{
    [CreateAssetMenu(menuName = "Aetherlands/Fishing/Fishing Config", fileName = "FishingConfig")]
    public sealed class FishingConfig : ScriptableObject
    {
        public const int Revision = 1;

        [Header("Coastal Water & Cast Geometry")]
        public float waterSurfaceY = -2.0f;
        [Min(1f)] public float minCastDistance = 2.4f;
        [Min(2f)] public float maxCastDistance = 5.4f;
        [Min(0.5f)] public float castArcHeight = 2.1f;
        [Min(0.2f)] public float waterSearchStep = 0.45f;

        [Header("Cast & Bite Timings")]
        [Min(0.2f)] public float chargeRate = 1.15f;
        [Min(0.1f)] public float castTravelDuration = 0.45f;
        [Min(0.3f)] public float minBiteWaitSeconds = 1.8f;
        [Min(0.5f)] public float maxBiteWaitSeconds = 4.4f;
        [Min(0.3f)] public float biteReactionWindowSeconds = 1.35f;
        [Min(0.4f)] public float resultDisplaySeconds = 2.4f;

        [Header("Reel Minigame Physics")]
        [Range(0.10f, 0.45f)] public float catchBarSize = 0.24f;
        [Min(0.2f)] public float barGravity = 1.35f;
        [Min(0.5f)] public float barThrust = 2.75f;
        [Min(0.4f)] public float barMaxSpeed = 1.85f;
        [Range(0f, 0.85f)] public float barBottomBounce = 0.42f;
        [Range(0f, 0.85f)] public float barTopBounce = 0.15f;
        [Range(0f, 3f)] public float barLinearDamping = 0.35f;

        [Header("Catch Progress")]
        [Range(0.1f, 0.6f)] public float initialProgress = 0.28f;
        [Min(0.05f)] public float progressGainRate = 0.22f;
        [Min(0.05f)] public float progressDrainRate = 0.17f;
        [Range(0.02f, 0.10f)] public float fishRadius = 0.045f;

        [Header("Coastal Fish Catalog")]
        public List<FishSpeciesDefinition> species = new List<FishSpeciesDefinition>();

        public FishingRules ToRules()
        {
            return new FishingRules(
                chargeRate,
                castTravelDuration,
                minBiteWaitSeconds,
                maxBiteWaitSeconds,
                biteReactionWindowSeconds,
                resultDisplaySeconds,
                catchBarSize,
                barGravity,
                barThrust,
                barMaxSpeed,
                barBottomBounce,
                barTopBounce,
                barLinearDamping,
                initialProgress,
                progressGainRate,
                progressDrainRate,
                fishRadius);
        }

        public List<FishSpec> BuildSpecList()
        {
            var list = new List<FishSpec>(species != null ? species.Count : 0);
            if (species != null)
            {
                for (int i = 0; i < species.Count; i++)
                {
                    if (species[i] != null)
                    {
                        list.Add(species[i].ToSpec());
                    }
                }
            }

            return list;
        }

        public FishSpeciesDefinition FindSpeciesById(string id)
        {
            if (string.IsNullOrEmpty(id) || species == null) return null;
            for (int i = 0; i < species.Count; i++)
            {
                var item = species[i];
                if (item != null && string.Equals(item.id, id, StringComparison.Ordinal))
                {
                    return item;
                }
            }

            return null;
        }

        /// <summary>
        /// Creates an in-memory default coastal fishing config with 6 Art Bible coastal species
        /// when no baked ScriptableObject asset has been assigned.
        /// </summary>
        public static FishingConfig CreateRuntimeDefault()
        {
            var config = CreateInstance<FishingConfig>();
            config.name = "RuntimeFishingConfig";
            config.species = CreateDefaultSpeciesList();
            return config;
        }

        public static List<FishSpeciesDefinition> CreateDefaultSpeciesList()
        {
            return new List<FishSpeciesDefinition>
            {
                FishSpeciesDefinition.CreateRuntime(
                    "anchovy", "Anchovy", FishRarity.Common, 3.6f,
                    22f, 0.85f, FishBehaviorPattern.Smooth,
                    11f, 22f, FishSilhouetteStyle.Slender,
                    new Color32(72, 126, 146, 255),
                    new Color32(204, 224, 219, 255),
                    new Color32(52, 77, 99, 255),
                    new Color32(131, 199, 170, 255)),

                FishSpeciesDefinition.CreateRuntime(
                    "sardine", "Sardine", FishRarity.Common, 3.2f,
                    30f, 0.95f, FishBehaviorPattern.Mixed,
                    14f, 28f, FishSilhouetteStyle.Slender,
                    new Color32(52, 139, 137, 255),
                    new Color32(226, 232, 212, 255),
                    new Color32(52, 77, 99, 255),
                    new Color32(240, 187, 102, 255)),

                FishSpeciesDefinition.CreateRuntime(
                    "coastal_bream", "Coastal Bream", FishRarity.Uncommon, 2.3f,
                    44f, 1.05f, FishBehaviorPattern.Sinker,
                    24f, 44f, FishSilhouetteStyle.Bream,
                    new Color32(142, 166, 152, 255),
                    new Color32(238, 216, 160, 255),
                    new Color32(134, 89, 68, 255),
                    new Color32(114, 166, 79, 255)),

                FishSpeciesDefinition.CreateRuntime(
                    "coral_snapper", "Coral Snapper", FishRarity.Uncommon, 1.9f,
                    54f, 1.15f, FishBehaviorPattern.Floater,
                    30f, 56f, FishSilhouetteStyle.Snapper,
                    new Color32(191, 91, 67, 255),
                    new Color32(241, 211, 170, 255),
                    new Color32(116, 62, 58, 255),
                    new Color32(240, 187, 102, 255)),

                FishSpeciesDefinition.CreateRuntime(
                    "lantern_bass", "Lantern Bass", FishRarity.Rare, 1.1f,
                    66f, 1.25f, FishBehaviorPattern.Dart,
                    38f, 68f, FishSilhouetteStyle.Snapper,
                    new Color32(52, 77, 99, 255),
                    new Color32(131, 199, 170, 255),
                    new Color32(191, 91, 67, 255),
                    new Color32(255, 201, 117, 255)),

                FishSpeciesDefinition.CreateRuntime(
                    "aether_tuna", "Aether Tuna", FishRarity.Epic, 0.55f,
                    80f, 1.45f, FishBehaviorPattern.Dart,
                    64f, 118f, FishSilhouetteStyle.Tuna,
                    new Color32(42, 98, 128, 255),
                    new Color32(180, 224, 218, 255),
                    new Color32(240, 187, 102, 255),
                    new Color32(131, 199, 170, 255))
            };
        }
    }
}
