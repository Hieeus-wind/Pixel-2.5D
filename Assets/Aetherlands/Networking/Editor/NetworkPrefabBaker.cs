using System;
using System.Collections.Generic;
using System.IO;
using Aetherlands.Coast;
using Aetherlands.Fishing;
using Aetherlands.Integration;
using Fusion;
using Fusion.Editor;
using UnityEditor;
using UnityEngine;

namespace Aetherlands.Networking.Editor
{
    public readonly struct NetworkPrefabBakeReport
    {
        public readonly string SessionConfigPath;
        public readonly string TravelerPrefabPath;
        public readonly string WorldRecordPrefabPath;
        public readonly bool TravelerRegistered;
        public readonly bool WorldRecordRegistered;
        public readonly bool AssemblyWeavingConfigured;

        public NetworkPrefabBakeReport(
            string sessionConfigPath,
            string travelerPrefabPath,
            string worldRecordPrefabPath,
            bool travelerRegistered,
            bool worldRecordRegistered,
            bool assemblyWeavingConfigured)
        {
            SessionConfigPath = sessionConfigPath;
            TravelerPrefabPath = travelerPrefabPath;
            WorldRecordPrefabPath = worldRecordPrefabPath;
            TravelerRegistered = travelerRegistered;
            WorldRecordRegistered = worldRecordRegistered;
            AssemblyWeavingConfigured = assemblyWeavingConfigured;
        }
    }

    /// <summary>
    /// Deterministically creates or updates Fusion 2 Shared Mode network prefabs under
    /// Assets/Aetherlands/Generated/Networking/ and registers them in Fusion's NetworkProjectConfig
    /// using supported Fusion Editor APIs.
    /// </summary>
    public static class NetworkPrefabBaker
    {
        public const string OutputDirectory = "Assets/Aetherlands/Generated/Networking";
        public const string SessionConfigPath = OutputDirectory + "/CoastalSessionConfig.asset";
        public const string TravelerPrefabPath = OutputDirectory + "/NetworkedTraveler.prefab";
        public const string WorldRecordPrefabPath = OutputDirectory + "/CoastalWorldRecord.prefab";

        private const string RecipeAssetPath = "Assets/Aetherlands/Recipes/CoastalCrossing.asset";
        private const string FishingConfigAssetPath = "Assets/Aetherlands/Generated/Fishing/FishingConfig.asset";
        private const string GeneratedAssetsDirectory = "Assets/Aetherlands/Generated";
        private const string NetworkingAssemblyName = "Aetherlands.Networking";

        [MenuItem("Tools/Aetherlands/Bake Network Prefabs", priority = 20)]
        public static void BakeNetworkPrefabsMenu()
        {
            NetworkPrefabBakeReport report = BakeAll();
            Debug.Log(
                $"[Aetherlands] Baked network prefabs:\n" +
                $"- Config: {report.SessionConfigPath}\n" +
                $"- Traveler: {report.TravelerPrefabPath} (Registered: {report.TravelerRegistered})\n" +
                $"- WorldRecord: {report.WorldRecordPrefabPath} (Registered: {report.WorldRecordRegistered})\n" +
                $"- AssembliesToWeave includes '{NetworkingAssemblyName}': {report.AssemblyWeavingConfigured}");
        }

        public static NetworkPrefabBakeReport BakeAll()
        {
            EnsureOutputDirectory();

            CoastalSessionConfig sessionConfig = EnsureSessionConfig();
            CoastalRecipe recipe = AssetDatabase.LoadAssetAtPath<CoastalRecipe>(RecipeAssetPath);
            FishingConfig fishingConfig = AssetDatabase.LoadAssetAtPath<FishingConfig>(FishingConfigAssetPath);
            Sprite[] travelerFrames = LoadTravelerSprites();
            Sprite footShadow = AssetDatabase.LoadAssetAtPath<Sprite>(GeneratedAssetsDirectory + "/FootShadow.png");

            GameObject travelerPrefab = BakeTravelerPrefab(sessionConfig, recipe, fishingConfig, travelerFrames, footShadow);
            GameObject worldRecordPrefab = BakeWorldRecordPrefab(sessionConfig);

            EnsureFusionPrefabLabel(travelerPrefab);
            EnsureFusionPrefabLabel(worldRecordPrefab);

            bool weavingConfigured = EnsureNetworkingAssemblyInWeaveList();

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            NetworkProjectConfigUtilities.RebuildPrefabTable();

            bool travelerRegistered = NetworkProjectConfigUtilities.TryGetPrefabId(TravelerPrefabPath, out _);
            bool worldRecordRegistered = NetworkProjectConfigUtilities.TryGetPrefabId(WorldRecordPrefabPath, out _);

            return new NetworkPrefabBakeReport(
                SessionConfigPath,
                TravelerPrefabPath,
                WorldRecordPrefabPath,
                travelerRegistered,
                worldRecordRegistered,
                weavingConfigured);
        }

        public static CoastalSessionConfig EnsureSessionConfig()
        {
            EnsureOutputDirectory();

            CoastalSessionConfig existing = AssetDatabase.LoadAssetAtPath<CoastalSessionConfig>(SessionConfigPath);
            if (existing != null)
            {
                return existing;
            }

            CoastalSessionConfig created = ScriptableObject.CreateInstance<CoastalSessionConfig>();
            created.defaultRoomName = CoastalSessionConfig.DefaultRoom;
            created.maxPlayers = CoastalSessionConfig.DefaultMaxPlayers;
            created.worldSeed = 7319;
            created.generatorVersion = CoastalRecipe.Revision.ToString();
            created.contentVersion = CoastalSessionConfig.DefaultContentVersion;
            created.spawnOrigin = new Vector3(0f, 0.015f, 0.9f);
            created.spawnPeerSpacing = 0.85f;
            created.remoteInterpolationRate = 18f;

            AssetDatabase.CreateAsset(created, SessionConfigPath);
            EditorUtility.SetDirty(created);
            return created;
        }

        private static GameObject BakeTravelerPrefab(
            CoastalSessionConfig sessionConfig,
            CoastalRecipe recipe,
            FishingConfig fishingConfig,
            Sprite[] frames,
            Sprite footShadow)
        {
            GameObject root = new GameObject("NetworkedTraveler");
            try
            {
                CharacterController controller = root.AddComponent<CharacterController>();
                controller.radius = 0.22f;
                controller.height = 1.35f;
                controller.center = Vector3.up * 0.675f;
                controller.skinWidth = 0.025f;
                controller.minMoveDistance = 0f;
                controller.stepOffset = 0.12f;

                Material spriteMaterial = AssetDatabase.GetBuiltinExtraResource<Material>("Sprites-Default.mat");

                GameObject picture = new GameObject("Traveler sprite");
                picture.transform.SetParent(root.transform, false);
                picture.transform.localRotation = Quaternion.Euler(37f, -9f, 0f);
                SpriteRenderer spriteRenderer = picture.AddComponent<SpriteRenderer>();
                if (frames != null && frames.Length > 0)
                {
                    spriteRenderer.sprite = frames[0];
                }
                spriteRenderer.sharedMaterial = spriteMaterial;

                GameObject shade = new GameObject("Contact shadow");
                shade.transform.SetParent(root.transform, false);
                shade.transform.localPosition = new Vector3(0f, 0.052f, -0.19f);
                shade.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
                shade.transform.localScale = new Vector3(0.78f, 0.64f, 1f);
                SpriteRenderer shadowRenderer = shade.AddComponent<SpriteRenderer>();
                shadowRenderer.sprite = footShadow;
                shadowRenderer.sharedMaterial = spriteMaterial;
                shadowRenderer.sortingOrder = -1;

                TravelerView view = root.AddComponent<TravelerView>();
                view.portrait = spriteRenderer;
                view.frames = frames ?? Array.Empty<Sprite>();

                TravelerMotor motor = root.AddComponent<TravelerMotor>();
                motor.recipe = recipe;
                motor.view = view;

                GameObject fishingVisualsGo = new GameObject("FishingWorldVisuals");
                fishingVisualsGo.transform.SetParent(root.transform, false);
                FishingWorldVisuals fishingVisuals = fishingVisualsGo.AddComponent<FishingWorldVisuals>();

                FishingController fishingController = root.AddComponent<FishingController>();
                fishingController.config = fishingConfig;
                fishingController.travelerMotor = motor;
                fishingController.worldVisuals = fishingVisuals;
                fishingController.enableLocalInput = false;
                fishingController.blockTravelerMotorWhileFishing = true;

                NetworkObject networkObject = root.AddComponent<NetworkObject>();
                networkObject.Flags = NetworkObjectFlags.V2
                    | NetworkObjectFlags.DestroyWhenStateAuthorityLeaves;

                NetworkedTraveler networkedTraveler = root.AddComponent<NetworkedTraveler>();
                SerializedObject so = new SerializedObject(networkedTraveler);
                so.FindProperty("sessionConfig").objectReferenceValue = sessionConfig;
                so.FindProperty("motor").objectReferenceValue = motor;
                so.FindProperty("view").objectReferenceValue = view;
                so.FindProperty("characterController").objectReferenceValue = controller;
                so.ApplyModifiedPropertiesWithoutUndo();

                CoastalFishingNetworkBridge fishingBridge = root.AddComponent<CoastalFishingNetworkBridge>();
                SerializedObject bridgeSo = new SerializedObject(fishingBridge);
                bridgeSo.FindProperty("networkedTraveler").objectReferenceValue = networkedTraveler;
                bridgeSo.FindProperty("fishingController").objectReferenceValue = fishingController;
                bridgeSo.FindProperty("worldVisuals").objectReferenceValue = fishingVisuals;
                bridgeSo.ApplyModifiedPropertiesWithoutUndo();

                return PrefabUtility.SaveAsPrefabAsset(root, TravelerPrefabPath);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(root);
            }
        }

        private static GameObject BakeWorldRecordPrefab(CoastalSessionConfig sessionConfig)
        {
            GameObject root = new GameObject("CoastalWorldRecord");
            try
            {
                NetworkObject networkObject = root.AddComponent<NetworkObject>();
                networkObject.Flags = NetworkObjectFlags.V2
                    | NetworkObjectFlags.MasterClientObject
                    | NetworkObjectFlags.AllowStateAuthorityOverride;

                CoastalWorldRecord record = root.AddComponent<CoastalWorldRecord>();
                SerializedObject so = new SerializedObject(record);
                so.FindProperty("sessionConfig").objectReferenceValue = sessionConfig;
                so.ApplyModifiedPropertiesWithoutUndo();

                return PrefabUtility.SaveAsPrefabAsset(root, WorldRecordPrefabPath);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(root);
            }
        }

        private static void EnsureFusionPrefabLabel(GameObject prefabAsset)
        {
            if (prefabAsset == null)
            {
                return;
            }

            string[] existing = AssetDatabase.GetLabels(prefabAsset);
            for (int i = 0; i < existing.Length; i++)
            {
                if (string.Equals(existing[i], NetworkProjectConfigImporter.FusionPrefabTag, StringComparison.Ordinal))
                {
                    return;
                }
            }

            List<string> updated = new List<string>(existing.Length + 1);
            updated.AddRange(existing);
            updated.Add(NetworkProjectConfigImporter.FusionPrefabTag);
            AssetDatabase.SetLabels(prefabAsset, updated.ToArray());
        }

        private static bool EnsureNetworkingAssemblyInWeaveList()
        {
            if (!NetworkProjectConfigAsset.TryGetGlobal(out NetworkProjectConfigAsset globalAsset)
                || globalAsset == null
                || globalAsset.Config == null)
            {
                return false;
            }

            NetworkProjectConfig config = globalAsset.Config;
            string[] existing = config.AssembliesToWeave ?? Array.Empty<string>();
            for (int i = 0; i < existing.Length; i++)
            {
                if (string.Equals(existing[i], NetworkingAssemblyName, StringComparison.Ordinal))
                {
                    return true;
                }
            }

            string[] updated = new string[existing.Length + 1];
            Array.Copy(existing, updated, existing.Length);
            updated[existing.Length] = NetworkingAssemblyName;
            config.AssembliesToWeave = updated;

            NetworkProjectConfigUtilities.SaveGlobalConfig(config);
            return true;
        }

        private static Sprite[] LoadTravelerSprites()
        {
            Sprite[] sprites = new Sprite[12];
            for (int i = 0; i < sprites.Length; i++)
            {
                string path = $"{GeneratedAssetsDirectory}/Traveler{i}.png";
                sprites[i] = AssetDatabase.LoadAssetAtPath<Sprite>(path);
            }

            return sprites;
        }

        private static void EnsureOutputDirectory()
        {
            if (AssetDatabase.IsValidFolder(OutputDirectory))
            {
                return;
            }

            string parent = Path.GetDirectoryName(OutputDirectory)?.Replace('\\', '/');
            string folderName = Path.GetFileName(OutputDirectory);
            if (!string.IsNullOrEmpty(parent) && !AssetDatabase.IsValidFolder(parent))
            {
                Directory.CreateDirectory(parent);
                AssetDatabase.Refresh();
            }

            AssetDatabase.CreateFolder(parent, folderName);
        }
    }
}
