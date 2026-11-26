using Aetherlands.Coast;
using UnityEngine;

namespace Aetherlands.Networking
{
    [CreateAssetMenu(menuName = "Aetherlands/Networking/Coastal Session Config", fileName = "CoastalSessionConfig")]
    public sealed class CoastalSessionConfig : ScriptableObject
    {
        public const int DefaultMaxPlayers = 8;
        public const string DefaultRoom = "Aetherlands-Coast";
        public const string DefaultContentVersion = "m2-coast-1";

        [Header("Session")]
        public string defaultRoomName = DefaultRoom;
        [Range(2, DefaultMaxPlayers)] public int maxPlayers = DefaultMaxPlayers;

        [Header("World Version Gate")]
        public int worldSeed = 7319;
        public string generatorVersion = "1";
        public string contentVersion = DefaultContentVersion;

        [Header("Spawning & Smoothing")]
        public Vector3 spawnOrigin = new Vector3(0f, 0.015f, 0.9f);
        [Min(0.25f)] public float spawnPeerSpacing = 0.85f;
        [Min(1f)] public float remoteInterpolationRate = 18f;

        public string EffectiveGeneratorVersion =>
            string.IsNullOrWhiteSpace(generatorVersion)
                ? CoastalRecipe.Revision.ToString()
                : generatorVersion.Trim();

        public string EffectiveContentVersion =>
            string.IsNullOrWhiteSpace(contentVersion)
                ? DefaultContentVersion
                : contentVersion.Trim();

        public string EffectiveRoomName =>
            string.IsNullOrWhiteSpace(defaultRoomName)
                ? DefaultRoom
                : defaultRoomName.Trim();

        public int EffectiveMaxPlayers => Mathf.Clamp(maxPlayers, 2, DefaultMaxPlayers);

        public Vector3 ComputeSpawnPosition(int peerSlot)
        {
            return CoastalSpawnPolicy.ComputeSpawnPosition(spawnOrigin, peerSlot, spawnPeerSpacing);
        }
    }
}
