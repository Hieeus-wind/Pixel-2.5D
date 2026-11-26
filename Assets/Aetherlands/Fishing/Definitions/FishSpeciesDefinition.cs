using UnityEngine;

namespace Aetherlands.Fishing
{
    public enum FishSilhouetteStyle : byte
    {
        Slender = 0,
        Bream = 1,
        Snapper = 2,
        Tuna = 3
    }

    [CreateAssetMenu(menuName = "Aetherlands/Fishing/Fish Species Definition", fileName = "FishSpecies")]
    public sealed class FishSpeciesDefinition : ScriptableObject
    {
        [Header("Identity")]
        public string id = "anchovy";
        public string displayName = "Anchovy";
        public FishRarity rarity = FishRarity.Common;
        [Min(0.05f)] public float spawnWeight = 3.0f;

        [Header("Minigame Behavior")]
        [Range(10f, 95f)] public float difficulty = 25f;
        [Range(0.4f, 2.5f)] public float speed = 0.85f;
        public FishBehaviorPattern behavior = FishBehaviorPattern.Smooth;

        [Header("Size (cm)")]
        [Min(1f)] public float minSizeCm = 12f;
        [Min(1f)] public float maxSizeCm = 24f;

        [Header("Procedural Pixel Art Palette")]
        public FishSilhouetteStyle silhouette = FishSilhouetteStyle.Slender;
        public Color bodyColor = new Color32(74, 134, 152, 255);
        public Color bellyColor = new Color32(198, 219, 214, 255);
        public Color finColor = new Color32(52, 77, 99, 255);
        public Color accentColor = new Color32(131, 199, 170, 255);

        public FishSpec ToSpec()
        {
            return new FishSpec(
                id,
                displayName,
                difficulty,
                speed,
                behavior,
                minSizeCm,
                maxSizeCm,
                rarity,
                spawnWeight);
        }

        public static FishSpeciesDefinition CreateRuntime(
            string id,
            string displayName,
            FishRarity rarity,
            float spawnWeight,
            float difficulty,
            float speed,
            FishBehaviorPattern behavior,
            float minSizeCm,
            float maxSizeCm,
            FishSilhouetteStyle silhouette,
            Color bodyColor,
            Color bellyColor,
            Color finColor,
            Color accentColor)
        {
            var asset = CreateInstance<FishSpeciesDefinition>();
            asset.name = displayName;
            asset.id = id;
            asset.displayName = displayName;
            asset.rarity = rarity;
            asset.spawnWeight = spawnWeight;
            asset.difficulty = difficulty;
            asset.speed = speed;
            asset.behavior = behavior;
            asset.minSizeCm = minSizeCm;
            asset.maxSizeCm = maxSizeCm;
            asset.silhouette = silhouette;
            asset.bodyColor = bodyColor;
            asset.bellyColor = bellyColor;
            asset.finColor = finColor;
            asset.accentColor = accentColor;
            return asset;
        }
    }
}
