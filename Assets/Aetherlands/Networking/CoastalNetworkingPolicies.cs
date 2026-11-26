using System;
using System.Collections.Generic;
using UnityEngine;

namespace Aetherlands.Networking
{
    public readonly struct CoastalVersionValidationResult
    {
        public readonly bool IsCompatible;
        public readonly bool GeneratorMatches;
        public readonly bool ContentMatches;
        public readonly string Summary;

        public CoastalVersionValidationResult(
            bool isCompatible,
            bool generatorMatches,
            bool contentMatches,
            string summary)
        {
            IsCompatible = isCompatible;
            GeneratorMatches = generatorMatches;
            ContentMatches = contentMatches;
            Summary = summary;
        }
    }

    public static class CoastalVersionGate
    {
        public static CoastalVersionValidationResult Validate(
            string localGeneratorVersion,
            string localContentVersion,
            string remoteGeneratorVersion,
            string remoteContentVersion)
        {
            string localGen = Normalize(localGeneratorVersion);
            string localContent = Normalize(localContentVersion);
            string remoteGen = Normalize(remoteGeneratorVersion);
            string remoteContent = Normalize(remoteContentVersion);

            if (string.IsNullOrEmpty(localGen) || string.IsNullOrEmpty(remoteGen))
            {
                return new CoastalVersionValidationResult(
                    false,
                    false,
                    string.Equals(localContent, remoteContent, StringComparison.Ordinal) && !string.IsNullOrEmpty(localContent),
                    "Generator version is missing.");
            }

            if (string.IsNullOrEmpty(localContent) || string.IsNullOrEmpty(remoteContent))
            {
                return new CoastalVersionValidationResult(
                    false,
                    string.Equals(localGen, remoteGen, StringComparison.Ordinal),
                    false,
                    "Content version is missing.");
            }

            bool generatorMatches = string.Equals(localGen, remoteGen, StringComparison.Ordinal);
            bool contentMatches = string.Equals(localContent, remoteContent, StringComparison.Ordinal);
            bool isCompatible = generatorMatches && contentMatches;

            if (isCompatible)
            {
                return new CoastalVersionValidationResult(
                    true,
                    true,
                    true,
                    $"Matched (gen {localGen}, content {localContent})");
            }

            if (!generatorMatches && !contentMatches)
            {
                return new CoastalVersionValidationResult(
                    false,
                    false,
                    false,
                    $"Generator ({localGen} != {remoteGen}) and content ({localContent} != {remoteContent}) mismatch.");
            }

            if (!generatorMatches)
            {
                return new CoastalVersionValidationResult(
                    false,
                    false,
                    true,
                    $"Generator version mismatch (local {localGen}, room {remoteGen}).");
            }

            return new CoastalVersionValidationResult(
                false,
                true,
                false,
                $"Content version mismatch (local {localContent}, room {remoteContent}).");
        }

        private static string Normalize(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
        }
    }

    public static class CoastalSpawnPolicy
    {
        private const float MaxDeckHalfWidth = 1.1f;

        public static bool ShouldSpawnForJoinedPlayer(
            int joinedPlayerId,
            int localPlayerId,
            bool hasExistingPlayerObject)
        {
            if (joinedPlayerId != localPlayerId)
            {
                return false;
            }

            return !hasExistingPlayerObject;
        }

        public static Vector3 ComputeSpawnPosition(Vector3 origin, int peerSlot, float spacing)
        {
            int safeSlot = Mathf.Max(0, peerSlot);
            float safeSpacing = Mathf.Max(0.25f, spacing);

            if (safeSlot == 0)
            {
                return origin;
            }

            int row = (safeSlot + 1) / 2;
            float side = (safeSlot % 2 == 1) ? -1f : 1f;
            float lateral = Mathf.Clamp(side * (safeSpacing * 0.65f), -MaxDeckHalfWidth, MaxDeckHalfWidth);
            float longitudinal = -row * safeSpacing;

            return new Vector3(
                origin.x + lateral,
                origin.y,
                origin.z + longitudinal);
        }
    }

    public sealed class MasterClientReconciliationState
    {
        private readonly HashSet<string> _activeWorldKeys = new HashSet<string>(StringComparer.Ordinal);
        private readonly HashSet<string> _processedActionKeys = new HashSet<string>(StringComparer.Ordinal);

        public int ActiveSessionRevision { get; private set; }
        public int LastReconciledMasterId { get; private set; } = -1;
        public bool IsReconciled { get; private set; }
        public IReadOnlyCollection<string> ActiveWorldKeys => _activeWorldKeys;

        public void SyncFromAuthoritativeRevision(int authoritativeRevision)
        {
            if (authoritativeRevision > ActiveSessionRevision)
            {
                ActiveSessionRevision = authoritativeRevision;
            }
        }

        public bool TryReconcile(
            bool hasStateAuthority,
            int currentMasterPlayerId,
            IEnumerable<string> liveWorldKeys)
        {
            if (!hasStateAuthority)
            {
                return false;
            }

            RebuildWorldKeyIndex(liveWorldKeys);

            if (IsReconciled && LastReconciledMasterId == currentMasterPlayerId)
            {
                return false;
            }

            ActiveSessionRevision++;
            LastReconciledMasterId = currentMasterPlayerId;
            IsReconciled = true;
            return true;
        }

        public void MarkAuthorityLost()
        {
            IsReconciled = false;
        }

        public bool TryRegisterAction(string actionKey)
        {
            if (string.IsNullOrWhiteSpace(actionKey))
            {
                return false;
            }

            return _processedActionKeys.Add(actionKey.Trim());
        }

        public bool HasProcessedAction(string actionKey)
        {
            if (string.IsNullOrWhiteSpace(actionKey))
            {
                return false;
            }

            return _processedActionKeys.Contains(actionKey.Trim());
        }

        private void RebuildWorldKeyIndex(IEnumerable<string> liveWorldKeys)
        {
            _activeWorldKeys.Clear();
            if (liveWorldKeys == null)
            {
                return;
            }

            foreach (string key in liveWorldKeys)
            {
                if (!string.IsNullOrWhiteSpace(key))
                {
                    _activeWorldKeys.Add(key.Trim());
                }
            }
        }
    }
}
