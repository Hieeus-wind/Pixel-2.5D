using System;
using System.Collections.Generic;
using NUnit.Framework;

namespace Aetherlands.Fishing.Tests
{
    public sealed class FishingTests
    {
        static FishSpec MakeFish(FishBehaviorPattern behavior, float difficulty = 45f, float speed = 1.0f)
        {
            return new FishSpec(
                "test_" + behavior.ToString().ToLowerInvariant(),
                "Test " + behavior,
                difficulty,
                speed,
                behavior,
                15f,
                40f,
                FishRarity.Uncommon,
                1f);
        }

        [Test]
        public void BarPhysics_HoldRises_ReleaseFallsAndBouncesAtBottom()
        {
            var fish = MakeFish(FishBehaviorPattern.Smooth, 15f, 0.5f);
            var session = new FishingMinigameSession(fish, FishingRules.Default, new SeededFishingRandom(101));

            // Hold reel to rise off the bottom.
            for (int i = 0; i < 30; i++)
            {
                session.Step(true, 1f / 60f);
            }

            float peakBottom = session.BarBottom;
            Assert.Greater(peakBottom, 0.15f, "Holding reel should lift the catch bar.");
            Assert.Greater(session.BarVelocity, 0f, "Bar velocity should be positive while rising.");

            // Release reel so gravity pulls bar back to bottom and triggers bounce.
            bool bounced = false;
            float previousVel = session.BarVelocity;
            for (int i = 0; i < 120 && !session.IsCompleted; i++)
            {
                session.Step(false, 1f / 60f);
                if (previousVel < -0.1f && session.BarVelocity > 0f && session.BarBottom <= 0.01f)
                {
                    bounced = true;
                    break;
                }

                previousVel = session.BarVelocity;
            }

            Assert.IsTrue(bounced, "Falling onto the bottom without holding reel should bounce upward.");
            Assert.GreaterOrEqual(session.BarBottom, 0f);
            Assert.LessOrEqual(session.BarTop, 1.0001f);
        }

        [Test]
        public void BarPhysics_ClampsAtTopBoundary()
        {
            var fish = MakeFish(FishBehaviorPattern.Floater, 20f, 0.6f);
            var session = new FishingMinigameSession(fish, FishingRules.Default, new SeededFishingRandom(202));

            for (int i = 0; i < 180 && !session.IsCompleted; i++)
            {
                session.Step(true, 1f / 60f);
                Assert.GreaterOrEqual(session.BarBottom, 0f);
                Assert.LessOrEqual(session.BarTop, 1.0001f);
            }

            Assert.AreEqual(1f - session.BarSize, session.BarBottom, 0.02f, "Holding reel continuously should keep the bar at the top clamp.");
        }

        [Test]
        public void FishAI_AllPatternsStayInBoundsAndExhibitDistinctProfiles()
        {
            var rules = new FishingRules(progressGainRate: 0.02f, progressDrainRate: 0.02f, initialProgress: 0.5f);
            var averages = new Dictionary<FishBehaviorPattern, float>();
            var peakSpeeds = new Dictionary<FishBehaviorPattern, float>();

            foreach (FishBehaviorPattern pattern in Enum.GetValues(typeof(FishBehaviorPattern)))
            {
                var fish = MakeFish(pattern, 60f, 1.15f);
                var session = new FishingMinigameSession(fish, rules, new SeededFishingRandom(7319));

                float sumPos = 0f;
                float maxAbsVel = 0f;
                int samples = 0;

                for (int i = 0; i < 420 && !session.IsCompleted; i++)
                {
                    // Track fish roughly with bar so the session runs long enough to sample AI.
                    bool hold = session.BarCenter < session.FishPosition;
                    session.Step(hold, 1f / 60f);

                    Assert.GreaterOrEqual(session.FishPosition, session.FishRadius - 0.0001f, pattern + " fell below track min.");
                    Assert.LessOrEqual(session.FishPosition, 1f - session.FishRadius + 0.0001f, pattern + " exceeded track max.");

                    sumPos += session.FishPosition;
                    maxAbsVel = Math.Max(maxAbsVel, Math.Abs(session.FishVelocity));
                    samples++;
                }

                Assert.Greater(samples, 120, pattern + " should stay active for sampling.");
                averages[pattern] = sumPos / samples;
                peakSpeeds[pattern] = maxAbsVel;
            }

            Assert.Greater(averages[FishBehaviorPattern.Floater], averages[FishBehaviorPattern.Sinker] + 0.12f,
                "Floater should average noticeably higher on the track than Sinker.");
            Assert.Greater(peakSpeeds[FishBehaviorPattern.Dart], peakSpeeds[FishBehaviorPattern.Smooth] * 1.4f,
                "Dart fish should reach noticeably higher peak velocity than Smooth fish.");
        }

        [Test]
        public void CatchProgress_FillsToWinWithPerfectWhenTracked()
        {
            var fish = MakeFish(FishBehaviorPattern.Smooth, 12f, 0.45f);
            var rules = new FishingRules(catchBarSize: 0.34f, progressGainRate: 0.35f, progressDrainRate: 0.15f);
            var session = new FishingMinigameSession(fish, rules, new SeededFishingRandom(303));

            for (int i = 0; i < 600 && !session.IsCompleted; i++)
            {
                bool hold = session.BarCenter < session.FishPosition;
                session.Step(hold, 1f / 60f);
            }

            Assert.IsTrue(session.IsCompleted);
            Assert.IsTrue(session.IsCaught, "Tracking an easy smooth fish should complete as caught.");
            Assert.IsFalse(session.IsEscaped);
            Assert.AreEqual(1f, session.CatchProgress, 0.0001f);
            Assert.IsTrue(session.IsPerfect, "Never losing overlap should award a perfect catch.");
        }

        [Test]
        public void CatchProgress_DrainsToEscapeWhenNotTracked()
        {
            var fish = MakeFish(FishBehaviorPattern.Floater, 75f, 1.4f);
            var rules = new FishingRules(catchBarSize: 0.16f, initialProgress: 0.25f, progressDrainRate: 0.35f);
            var session = new FishingMinigameSession(fish, rules, new SeededFishingRandom(404));

            // Never hold reel: bar stays at bottom while floater rises away.
            for (int i = 0; i < 600 && !session.IsCompleted; i++)
            {
                session.Step(false, 1f / 60f);
            }

            Assert.IsTrue(session.IsCompleted);
            Assert.IsTrue(session.IsEscaped, "Ignoring a floater fish should drain catch progress to escaped.");
            Assert.IsFalse(session.IsCaught);
            Assert.AreEqual(0f, session.CatchProgress, 0.0001f);
            Assert.IsFalse(session.IsPerfect);
        }

        [Test]
        public void Minigame_DeterministicReplayProducesIdenticalTrajectory()
        {
            var fish = MakeFish(FishBehaviorPattern.Dart, 65f, 1.2f);
            var rules = FishingRules.Default;

            var runA = new FishingMinigameSession(fish, rules, new SeededFishingRandom(9991));
            var runB = new FishingMinigameSession(fish, rules, new SeededFishingRandom(9991));

            for (int i = 0; i < 300; i++)
            {
                bool input = (i % 17) < 9;
                runA.Step(input, 1f / 60f);
                runB.Step(input, 1f / 60f);

                Assert.AreEqual(runA.BarBottom, runB.BarBottom);
                Assert.AreEqual(runA.BarVelocity, runB.BarVelocity);
                Assert.AreEqual(runA.FishPosition, runB.FishPosition);
                Assert.AreEqual(runA.CatchProgress, runB.CatchProgress);
                Assert.AreEqual(runA.IsCompleted, runB.IsCompleted);
                Assert.AreEqual(runA.IsCaught, runB.IsCaught);
                Assert.AreEqual(runA.IsPerfect, runB.IsPerfect);
            }
        }

        [Test]
        public void FlowMachine_FullCatchLifecycleTransitionsCleanly()
        {
            var rules = new FishingRules(
                chargeRate: 2.0f,
                castTravelDuration: 0.2f,
                minBiteWaitSeconds: 0.5f,
                maxBiteWaitSeconds: 0.5f,
                biteReactionWindowSeconds: 1.0f,
                resultDisplaySeconds: 0.5f,
                catchBarSize: 0.36f,
                progressGainRate: 0.45f);

            var catalog = new List<FishSpec> { MakeFish(FishBehaviorPattern.Smooth, 12f, 0.4f) };
            var machine = new FishingFlowMachine(rules, catalog, new SeededFishingRandom(505));

            var transitions = new List<FishingState>();
            machine.OnStateChanged += (_, next) => transitions.Add(next);

            Assert.IsFalse(machine.BeginCharge(false), "Cannot begin charge when not facing valid water.");
            Assert.AreEqual(FishingState.Idle, machine.State);

            Assert.IsTrue(machine.BeginCharge(true));
            Assert.AreEqual(FishingState.ChargingCast, machine.State);

            machine.Step(true, 0.35f);
            Assert.Greater(machine.CastPower, 0.5f);

            Assert.IsTrue(machine.ReleaseCast());
            Assert.AreEqual(FishingState.Casting, machine.State);

            machine.Step(false, 0.25f);
            Assert.AreEqual(FishingState.WaitingForBite, machine.State);
            Assert.IsFalse(machine.IsBiteCueActive);

            // Advance until bite cue appears.
            for (int i = 0; i < 60 && !machine.IsBiteCueActive; i++)
            {
                machine.Step(false, 0.05f);
            }

            Assert.IsTrue(machine.IsBiteCueActive, "Bite cue should activate after wait duration.");
            Assert.IsTrue(machine.PressHook(), "Pressing hook during active bite cue should start minigame.");
            Assert.AreEqual(FishingState.HookedMinigame, machine.State);
            Assert.IsNotNull(machine.ActiveMinigame);

            // Track fish until caught.
            for (int i = 0; i < 400 && machine.State == FishingState.HookedMinigame; i++)
            {
                bool hold = machine.ActiveMinigame.BarCenter < machine.ActiveMinigame.FishPosition;
                machine.Step(hold, 1f / 60f);
            }

            Assert.AreEqual(FishingState.Caught, machine.State);
            Assert.AreEqual(1, machine.CatchCount);
            Assert.IsTrue(machine.HasLastCatch);
            Assert.AreEqual("test_smooth", machine.LastCatch.Species.Id);

            // Wait for toast duration to return to Idle.
            machine.Step(false, 0.6f);
            Assert.AreEqual(FishingState.Idle, machine.State);

            CollectionAssert.AreEqual(
                new[]
                {
                    FishingState.ChargingCast,
                    FishingState.Casting,
                    FishingState.WaitingForBite,
                    FishingState.HookedMinigame,
                    FishingState.Caught,
                    FishingState.Idle
                },
                transitions);
        }

        [Test]
        public void FlowMachine_MissedBiteWindowTransitionsToEscapedAndReturnsToIdle()
        {
            var rules = new FishingRules(
                castTravelDuration: 0.1f,
                minBiteWaitSeconds: 0.2f,
                maxBiteWaitSeconds: 0.2f,
                biteReactionWindowSeconds: 0.3f,
                resultDisplaySeconds: 0.3f);

            var machine = new FishingFlowMachine(rules, new List<FishSpec> { MakeFish(FishBehaviorPattern.Mixed) }, new SeededFishingRandom(606));
            bool escapedEventFired = false;
            machine.OnFishEscaped += _ => escapedEventFired = true;

            machine.BeginCharge(true);
            machine.Step(true, 0.2f);
            machine.ReleaseCast();
            machine.Step(false, 0.15f);
            Assert.AreEqual(FishingState.WaitingForBite, machine.State);

            // Wait past bite wait + reaction window without hooking.
            for (int i = 0; i < 40 && machine.State == FishingState.WaitingForBite; i++)
            {
                machine.Step(false, 0.05f);
            }

            Assert.AreEqual(FishingState.Escaped, machine.State);
            Assert.IsTrue(escapedEventFired);

            machine.Step(false, 0.4f);
            Assert.AreEqual(FishingState.Idle, machine.State);
        }

        [Test]
        public void CoastalWaterSpotDetector_DistinguishesDeckIslandsAndOpenWater()
        {
            Assert.IsTrue(CoastalWaterSpotDetector.IsOverCoastalLand(0f, 0f), "Bridge center is land.");
            Assert.IsTrue(CoastalWaterSpotDetector.IsOverCoastalLand(0f, 8f), "North landing center is land.");
            Assert.IsTrue(CoastalWaterSpotDetector.IsOverCoastalLand(0f, -12f), "South landing center is land.");
            Assert.IsFalse(CoastalWaterSpotDetector.IsOverCoastalLand(4.2f, 0f), "East of bridge deck is open water.");
            Assert.IsFalse(CoastalWaterSpotDetector.IsOverCoastalLand(-4.2f, -2f), "West of bridge deck is open water.");
        }
    }
}
