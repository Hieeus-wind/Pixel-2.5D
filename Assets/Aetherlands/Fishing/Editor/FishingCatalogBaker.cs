using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace Aetherlands.Fishing.Editor
{
    /// <summary>
    /// Deterministic, idempotent Editor baker for default coastal FishingConfig
    /// and FishSpeciesDefinition assets under Assets/Aetherlands/Generated/Fishing/.
    /// Preserves asset GUIDs when re-baked.
    /// </summary>
    public static class FishingCatalogBaker
    {
        public const string RootFolder = "Assets/Aetherlands/Generated/Fishing";
        public const string SpeciesFolder = RootFolder + "/Species";
        public const string ConfigAssetPath = RootFolder + "/FishingConfig.asset";

        readonly struct SpeciesPreset
        {
            public readonly string FileName;
            public readonly string Id;
            public readonly string DisplayName;
            public readonly FishRarity Rarity;
            public readonly float SpawnWeight;
            public readonly float Difficulty;
            public readonly float Speed;
            public readonly FishBehaviorPattern Behavior;
            public readonly float MinSizeCm;
            public readonly float MaxSizeCm;
            public readonly FishSilhouetteStyle Silhouette;
            public readonly Color32 Body;
            public readonly Color32 Belly;
            public readonly Color32 Fin;
            public readonly Color32 Accent;

            public SpeciesPreset(
                string fileName,
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
                Color32 body,
                Color32 belly,
                Color32 fin,
                Color32 accent)
            {
                FileName = fileName;
                Id = id;
                DisplayName = displayName;
                Rarity = rarity;
                SpawnWeight = spawnWeight;
                Difficulty = difficulty;
                Speed = speed;
                Behavior = behavior;
                MinSizeCm = minSizeCm;
                MaxSizeCm = maxSizeCm;
                Silhouette = silhouette;
                Body = body;
                Belly = belly;
                Fin = fin;
                Accent = accent;
            }
        }

        static readonly SpeciesPreset[] DefaultPresets =
        {
            new SpeciesPreset(
                "Anchovy.asset", "anchovy", "Anchovy", FishRarity.Common, 3.6f,
                22f, 0.85f, FishBehaviorPattern.Smooth, 11f, 22f, FishSilhouetteStyle.Slender,
                new Color32(72, 126, 146, 255), new Color32(204, 224, 219, 255),
                new Color32(52, 77, 99, 255), new Color32(131, 199, 170, 255)),

            new SpeciesPreset(
                "Sardine.asset", "sardine", "Sardine", FishRarity.Common, 3.2f,
                30f, 0.95f, FishBehaviorPattern.Mixed, 14f, 28f, FishSilhouetteStyle.Slender,
                new Color32(52, 139, 137, 255), new Color32(226, 232, 212, 255),
                new Color32(52, 77, 99, 255), new Color32(240, 187, 102, 255)),

            new SpeciesPreset(
                "CoastalBream.asset", "coastal_bream", "Coastal Bream", FishRarity.Uncommon, 2.3f,
                44f, 1.05f, FishBehaviorPattern.Sinker, 24f, 44f, FishSilhouetteStyle.Bream,
                new Color32(142, 166, 152, 255), new Color32(238, 216, 160, 255),
                new Color32(134, 89, 68, 255), new Color32(114, 166, 79, 255)),

            new SpeciesPreset(
                "CoralSnapper.asset", "coral_snapper", "Coral Snapper", FishRarity.Uncommon, 1.9f,
                54f, 1.15f, FishBehaviorPattern.Floater, 30f, 56f, FishSilhouetteStyle.Snapper,
                new Color32(191, 91, 67, 255), new Color32(241, 211, 170, 255),
                new Color32(116, 62, 58, 255), new Color32(240, 187, 102, 255)),

            new SpeciesPreset(
                "LanternBass.asset", "lantern_bass", "Lantern Bass", FishRarity.Rare, 1.1f,
                66f, 1.25f, FishBehaviorPattern.Dart, 38f, 68f, FishSilhouetteStyle.Snapper,
                new Color32(52, 77, 99, 255), new Color32(131, 199, 170, 255),
                new Color32(191, 91, 67, 255), new Color32(255, 201, 117, 255)),

            new SpeciesPreset(
                "AetherTuna.asset", "aether_tuna", "Aether Tuna", FishRarity.Epic, 0.55f,
                80f, 1.45f, FishBehaviorPattern.Dart, 64f, 118f, FishSilhouetteStyle.Tuna,
                new Color32(42, 98, 128, 255), new Color32(180, 224, 218, 255),
                new Color32(240, 187, 102, 255), new Color32(131, 199, 170, 255))
        };

        [MenuItem("Tools/Aetherlands/Bake Fishing Assets")]
        public static void BakeMenu()
        {
            Bake();
        }

        public static FishingConfig Bake()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                throw new InvalidOperationException("Exit Play Mode before baking fishing assets.");
            }

            EnsureFolder(RootFolder);
            EnsureFolder(SpeciesFolder);

            var bakedSpecies = new List<FishSpeciesDefinition>(DefaultPresets.Length);
            bool anyChanged = false;

            for (int i = 0; i < DefaultPresets.Length; i++)
            {
                SpeciesPreset preset = DefaultPresets[i];
                string assetPath = SpeciesFolder + "/" + preset.FileName;
                FishSpeciesDefinition asset = AssetDatabase.LoadAssetAtPath<FishSpeciesDefinition>(assetPath);
                bool created = false;

                if (asset == null)
                {
                    asset = ScriptableObject.CreateInstance<FishSpeciesDefinition>();
                    created = true;
                }

                bool modified = ApplyPreset(asset, preset);
                if (created)
                {
                    AssetDatabase.CreateAsset(asset, assetPath);
                    anyChanged = true;
                }
                else if (modified)
                {
                    EditorUtility.SetDirty(asset);
                    anyChanged = true;
                }

                bakedSpecies.Add(asset);
            }

            FishingConfig config = AssetDatabase.LoadAssetAtPath<FishingConfig>(ConfigAssetPath);
            bool configCreated = false;
            if (config == null)
            {
                config = ScriptableObject.CreateInstance<FishingConfig>();
                configCreated = true;
            }

            bool configModified = ApplyDefaultConfig(config, bakedSpecies);
            if (configCreated)
            {
                AssetDatabase.CreateAsset(config, ConfigAssetPath);
                anyChanged = true;
            }
            else if (configModified)
            {
                EditorUtility.SetDirty(config);
                anyChanged = true;
            }

            if (anyChanged)
            {
                AssetDatabase.SaveAssets();
            }

            Debug.Log("Baked fishing assets at " + ConfigAssetPath + " (" + bakedSpecies.Count + " species).");
            return config;
        }

        static bool ApplyPreset(FishSpeciesDefinition asset, SpeciesPreset p)
        {
            bool changed = false;
            changed |= Set(ref asset.id, p.Id);
            changed |= Set(ref asset.displayName, p.DisplayName);
            changed |= Set(ref asset.rarity, p.Rarity);
            changed |= Set(ref asset.spawnWeight, p.SpawnWeight);
            changed |= Set(ref asset.difficulty, p.Difficulty);
            changed |= Set(ref asset.speed, p.Speed);
            changed |= Set(ref asset.behavior, p.Behavior);
            changed |= Set(ref asset.minSizeCm, p.MinSizeCm);
            changed |= Set(ref asset.maxSizeCm, p.MaxSizeCm);
            changed |= Set(ref asset.silhouette, p.Silhouette);
            changed |= Set(ref asset.bodyColor, (Color)p.Body);
            changed |= Set(ref asset.bellyColor, (Color)p.Belly);
            changed |= Set(ref asset.finColor, (Color)p.Fin);
            changed |= Set(ref asset.accentColor, (Color)p.Accent);
            return changed;
        }

        static bool ApplyDefaultConfig(FishingConfig config, List<FishSpeciesDefinition> bakedSpecies)
        {
            bool changed = false;
            changed |= Set(ref config.waterSurfaceY, -2.0f);
            changed |= Set(ref config.minCastDistance, 2.4f);
            changed |= Set(ref config.maxCastDistance, 5.4f);
            changed |= Set(ref config.castArcHeight, 2.1f);
            changed |= Set(ref config.waterSearchStep, 0.45f);

            changed |= Set(ref config.chargeRate, 1.15f);
            changed |= Set(ref config.castTravelDuration, 0.45f);
            changed |= Set(ref config.minBiteWaitSeconds, 1.8f);
            changed |= Set(ref config.maxBiteWaitSeconds, 4.4f);
            changed |= Set(ref config.biteReactionWindowSeconds, 1.35f);
            changed |= Set(ref config.resultDisplaySeconds, 2.4f);

            changed |= Set(ref config.catchBarSize, 0.24f);
            changed |= Set(ref config.barGravity, 1.35f);
            changed |= Set(ref config.barThrust, 2.75f);
            changed |= Set(ref config.barMaxSpeed, 1.85f);
            changed |= Set(ref config.barBottomBounce, 0.42f);
            changed |= Set(ref config.barTopBounce, 0.15f);
            changed |= Set(ref config.barLinearDamping, 0.35f);

            changed |= Set(ref config.initialProgress, 0.28f);
            changed |= Set(ref config.progressGainRate, 0.22f);
            changed |= Set(ref config.progressDrainRate, 0.17f);
            changed |= Set(ref config.fishRadius, 0.045f);

            if (config.species == null)
            {
                config.species = new List<FishSpeciesDefinition>(bakedSpecies);
                changed = true;
            }
            else if (config.species.Count != bakedSpecies.Count)
            {
                config.species.Clear();
                config.species.AddRange(bakedSpecies);
                changed = true;
            }
            else
            {
                for (int i = 0; i < bakedSpecies.Count; i++)
                {
                    if (config.species[i] != bakedSpecies[i])
                    {
                        config.species[i] = bakedSpecies[i];
                        changed = true;
                    }
                }
            }

            return changed;
        }

        static bool Set<T>(ref T field, T value)
        {
            if (EqualityComparer<T>.Default.Equals(field, value)) return false;
            field = value;
            return true;
        }

        static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path)) return;
            string parent = Path.GetDirectoryName(path).Replace('\\', '/');
            EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, Path.GetFileName(path));
        }
    }
}
