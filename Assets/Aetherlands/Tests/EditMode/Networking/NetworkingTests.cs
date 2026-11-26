using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

namespace Aetherlands.Networking.Tests
{
    public sealed class NetworkingTests
    {
        [Test]
        public void VersionGate_AcceptsMatchingGeneratorAndContentVersions()
        {
            CoastalVersionValidationResult result = CoastalVersionGate.Validate("1", "m2-coast-1", "1", "m2-coast-1");

            Assert.IsTrue(result.IsCompatible);
            Assert.IsTrue(result.GeneratorMatches);
            Assert.IsTrue(result.ContentMatches);
        }

        [Test]
        public void VersionGate_RejectsGeneratorMismatch()
        {
            CoastalVersionValidationResult result = CoastalVersionGate.Validate("1", "m2-coast-1", "2", "m2-coast-1");

            Assert.IsFalse(result.IsCompatible);
            Assert.IsFalse(result.GeneratorMatches);
            Assert.IsTrue(result.ContentMatches);
        }

        [Test]
        public void VersionGate_RejectsContentMismatch()
        {
            CoastalVersionValidationResult result = CoastalVersionGate.Validate("1", "m2-coast-1", "1", "m2-coast-2");

            Assert.IsFalse(result.IsCompatible);
            Assert.IsTrue(result.GeneratorMatches);
            Assert.IsFalse(result.ContentMatches);
        }

        [Test]
        public void VersionGate_RejectsEmptyVersions()
        {
            CoastalVersionValidationResult missingGen = CoastalVersionGate.Validate("", "m2-coast-1", "1", "m2-coast-1");
            CoastalVersionValidationResult missingContent = CoastalVersionGate.Validate("1", "   ", "1", "m2-coast-1");

            Assert.IsFalse(missingGen.IsCompatible);
            Assert.IsFalse(missingContent.IsCompatible);
        }

        [Test]
        public void SpawnPolicy_AllowsOnlyLocalPlayerWithoutExistingObject()
        {
            Assert.IsTrue(CoastalSpawnPolicy.ShouldSpawnForJoinedPlayer(joinedPlayerId: 3, localPlayerId: 3, hasExistingPlayerObject: false));
            Assert.IsFalse(CoastalSpawnPolicy.ShouldSpawnForJoinedPlayer(joinedPlayerId: 3, localPlayerId: 3, hasExistingPlayerObject: true));
            Assert.IsFalse(CoastalSpawnPolicy.ShouldSpawnForJoinedPlayer(joinedPlayerId: 4, localPlayerId: 3, hasExistingPlayerObject: false));
        }

        [Test]
        public void SpawnPolicy_ComputesDistinctDeckSafePositionsForEightPeers()
        {
            Vector3 origin = new Vector3(0f, 0.015f, 0.9f);
            HashSet<Vector3> uniquePositions = new HashSet<Vector3>();

            for (int slot = 0; slot < CoastalSessionConfig.DefaultMaxPlayers; slot++)
            {
                Vector3 pos = CoastalSpawnPolicy.ComputeSpawnPosition(origin, slot, 0.85f);
                Assert.IsTrue(uniquePositions.Add(pos), $"Duplicate spawn position at peer slot {slot}.");
                Assert.LessOrEqual(Mathf.Abs(pos.x - origin.x), 1.1f + 0.0001f, $"Peer slot {slot} exceeds deck railing width.");
                Assert.AreEqual(origin.y, pos.y, 0.0001f);
            }
        }

        [Test]
        public void MasterClientReconciliation_WaitsForStateAuthorityAndIsIdempotent()
        {
            MasterClientReconciliationState state = new MasterClientReconciliationState();

            bool withoutAuthority = state.TryReconcile(
                hasStateAuthority: false,
                currentMasterPlayerId: 1,
                liveWorldKeys: new[] { "coastal-world-record", "player:1" });

            Assert.IsFalse(withoutAuthority);
            Assert.IsFalse(state.IsReconciled);
            Assert.AreEqual(0, state.ActiveSessionRevision);

            bool firstReconcile = state.TryReconcile(
                hasStateAuthority: true,
                currentMasterPlayerId: 1,
                liveWorldKeys: new[] { "coastal-world-record", "player:1" });

            Assert.IsTrue(firstReconcile);
            Assert.IsTrue(state.IsReconciled);
            Assert.AreEqual(1, state.ActiveSessionRevision);
            Assert.AreEqual(2, state.ActiveWorldKeys.Count);

            bool repeatSameMaster = state.TryReconcile(
                hasStateAuthority: true,
                currentMasterPlayerId: 1,
                liveWorldKeys: new[] { "coastal-world-record", "player:1", "player:2" });

            Assert.IsFalse(repeatSameMaster);
            Assert.AreEqual(1, state.ActiveSessionRevision);
            Assert.AreEqual(3, state.ActiveWorldKeys.Count);
        }

        [Test]
        public void MasterClientReconciliation_HandoffRebuildsIndicesAndDeduplicatesActions()
        {
            MasterClientReconciliationState state = new MasterClientReconciliationState();
            state.SyncFromAuthoritativeRevision(4);

            bool handoff = state.TryReconcile(
                hasStateAuthority: true,
                currentMasterPlayerId: 2,
                liveWorldKeys: new[] { "coastal-world-record", "player:2" });

            Assert.IsTrue(handoff);
            Assert.AreEqual(5, state.ActiveSessionRevision);
            Assert.AreEqual(2, state.LastReconciledMasterId);
            CollectionAssert.AreEquivalent(new[] { "coastal-world-record", "player:2" }, state.ActiveWorldKeys);

            Assert.IsTrue(state.TryRegisterAction("p2:cast:001"));
            Assert.IsFalse(state.TryRegisterAction("p2:cast:001"));
            Assert.IsTrue(state.HasProcessedAction("p2:cast:001"));
        }

        [Test]
        public void NetworkedTraveler_FacingEncodeDecodeRoundTripsAllFourDirections()
        {
            Assert.AreEqual(0, NetworkedTraveler.EncodeFacing(Vector2.down));
            Assert.AreEqual(1, NetworkedTraveler.EncodeFacing(Vector2.up));
            Assert.AreEqual(2, NetworkedTraveler.EncodeFacing(Vector2.right));
            Assert.AreEqual(3, NetworkedTraveler.EncodeFacing(Vector2.left));

            Assert.AreEqual(Vector2.down, NetworkedTraveler.DecodeFacingIntent(0));
            Assert.AreEqual(Vector2.up, NetworkedTraveler.DecodeFacingIntent(1));
            Assert.AreEqual(Vector2.right, NetworkedTraveler.DecodeFacingIntent(2));
            Assert.AreEqual(Vector2.left, NetworkedTraveler.DecodeFacingIntent(3));
        }
    }
}
