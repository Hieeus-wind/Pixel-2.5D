# Verification plan

M0 checks are document and source inspections. M1 results are recorded in PROJECT_STATE.md. Multiplayer procedures are planned for later milestones. Record the Unity version, scene, recipe revision, seed, resolution, peers, expected/actual outcome and evidence for each run. Mark unavailable checks unverified.

## M1 coastal crossing

| ID | Procedure | Expected result |
|---|---|---|
| GEN-01 | Run EditMode tests for fixed-seed pixel and mesh digests. Bake the same recipe twice, compare the output manifest, asset GUIDs, counts and scene children. Change one wood recipe and rebake. | Identical input produces identical content without duplicates. Only dependent outputs change; existing references resolve. |
| PLAY-01 | Open the generated CoastalBenchmark scene, enter Play Mode, walk from the entrance onto the landing and return. Press into each rail, release movement and try a diagonal approach. | Feet stay grounded, rails block exits, input release stops travel, diagonal speed is normalized and the camera follows continuously. |
| ART-01 | At 1280 × 720 capture entrance, midspan and landing. Walk slowly sideways and diagonally. Compare stills and motion with ART_BIBLE.md. | Wood joins, rope and strata read at gameplay scale. Nearshore rock shows through water, foam follows the shore, sprite stays crisp and rail occlusion is consistent. |
| QA-01 | Reopen the generated scene after import, enter/exit Play twice, run EditMode and PlayMode suites in Window > General > Test Runner and inspect Console. Profile a repeated crossing after warm-up. | Scene loads with intact references, no new errors, no steady per-frame texture/mesh creation. Record test totals, frame timings, allocations and hardware. |

Bake menu: Tools > Aetherlands > Bake Coastal Benchmark. Scene path: Assets/Aetherlands/Generated/Scenes/CoastalBenchmark.unity. In Test Runner, run Aetherlands.Coast.EditTests and Aetherlands.Coast.PlayTests. The PlayMode suite captures three stills in Docs/Captures/M1 and movement frames in Temp/M1Motion.

## Multiplayer and later gates

| ID | Procedure | Expected result |
|---|---|---|
| NET-01 | Join the same room on two independent clients. Move each in turn; join additional clients through eight. Leave and rejoin. | One avatar per connected peer, local-only input/camera, coherent remote pose and eight concurrent players. |
| NET-02 | Move away from spawn, deplete a node, then join from a fresh client. Repeat across a local chunk unload/reload. | Versions, terrain, current pose and remaining charges agree without replaying transient events. |
| NET-03 | Have the Master Client leave while a second peer remains, first during an encounter and then during a pending grant. Retry the request and join another peer. | Existing state survives. New master resumes each entity once; recipient inventory shows one credited grant. |
| RULE-01 | Submit repeated swing/gather intents, out-of-range interactions and simultaneous last-charge attempts. | Authority derives outcomes, rejects invalid actions, and commits each valid result once. |
| SAVE-01 | Craft, equip, save and restart. Interrupt a subsequent write and load again. | Last complete snapshot resolves item IDs, counts, progression and gear. No partial item grant or loss. |
| MVP-01 | With a party, travel village to forest, fight each enemy, gather, craft, defeat the boss and return. Record visuals and synthesized feedback. | The complete loop and each milestone gate can be reproduced. Attach failures and profiling evidence before sign-off. |
