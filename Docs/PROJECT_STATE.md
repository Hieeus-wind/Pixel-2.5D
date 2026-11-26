# Project state

## M2 delivery (Multiplayer & Stardew Fishing) — 2026-11-25

Photon Fusion 2.1.3 Shared Mode multiplayer (capacity 8, playable with 2+ peers) and Stardew Valley-style coastal fishing are implemented and integrated into [CoastalBenchmark.unity](../Assets/Aetherlands/Generated/Scenes/CoastalBenchmark.unity). Unity 6000.3.6f1 compiled all assemblies with zero Console errors or warnings post-bake and post-PlayMode. Local solo play remains active by default; use the top-left `MultiplayerHud` to join or leave a Shared Mode room, and press `Space` / `E` / Left Click near coastal water to cast and reel.

| Delivered area | Implementation |
|---|---|
| Shared Mode networking | `Assets/Aetherlands/Networking/` (`Aetherlands.Networking.asmdef`, `CoastalSessionConfig.cs`, `CoastalNetworkingPolicies.cs`, `IFishingStateProvider.cs`, `CoastalWorldRecord.cs`, `NetworkedTraveler.cs`, `CoastalSessionRunner.cs`, `MultiplayerHud.cs`) and `Networking/Editor/NetworkPrefabBaker.cs`. `AppIdFusion` is configured in `Assets/Photon/Fusion/Resources/PhotonAppSettings.asset`. |
| Coastal fishing | `Assets/Aetherlands/Fishing/` (`Aetherlands.Fishing.asmdef`, pure C# domain `FishingFlowMachine.cs` & `FishingMinigameSession.cs`, `FishSpeciesDefinition.cs`, `FishingConfig.cs`, procedural pixel art `FishingPixelArt.cs`, `FishingWorldVisuals.cs`, `FishingOverlayView.cs`, `CoastalWaterSpotDetector.cs`, `FishingController.cs`) and `Fishing/Editor/FishingCatalogBaker.cs`. |
| Scene & prefab integration | `Assets/Aetherlands/Integration/` (`Aetherlands.Integration.asmdef`, `CoastalFishingNetworkBridge.cs`) bridges `NetworkedTraveler` with `FishingController` (local input gated on `Object.HasStateAuthority`) and `FishingWorldVisuals.ApplyRemoteState(...)` for remote peers. `CoastalBenchmarkBaker.cs` and `CoastalSceneRig.cs` deterministically wire local `FishingController` and `MultiplayerSession` (`CoastalSessionRunner` + `MultiplayerHud`) into `CoastalBenchmark.unity` (13 roots). |
| Bakes | Executed cleanly in order: `Tools > Aetherlands > Bake Fishing Assets` (`Generated/Fishing/FishingConfig.asset` + 6 species under `Species/`), `Tools > Aetherlands > Bake Network Prefabs` (`Generated/Networking/CoastalSessionConfig.asset`, `NetworkedTraveler.prefab` Registered: True, `CoastalWorldRecord.prefab` Registered: True, `Aetherlands.Networking` in `AssembliesToWeave`: True), and `Tools > Aetherlands > Bake Coastal Benchmark`. |

**Editor & PlayMode test results.**
- **EditMode (24/24 passed, 0 failed, 0 skipped, 3.45s):**
  - `Aetherlands.Coast.EditTests`: 6/6 passed (including repeated identical-input bake and timber-only rebake).
  - `Aetherlands.Fishing.EditTests`: 9/9 passed (bar physics, all 5 fish AI patterns, catch/escape, determinism, lifecycle, water spot detection).
  - `Aetherlands.Networking.EditTests`: 9/9 passed (version gate, 8-player spawn policy, Master Client reconciliation, facing encode/decode).
- **PlayMode (`Aetherlands.Coast.PlayTests`, 3/3 passed, 0 failed, 0 skipped, 4.66s):** passed on the updated 13-root `CoastalBenchmark.unity` scene.
- **Console:** 0 errors, 0 warnings post-bake and post-PlayMode.

**Issues encountered & resolved.**
- `FishingPixelArt.cs` was initially missed by Unity's AssetDatabase scan until refreshed/re-imported.
- `Aetherlands.Networking.EditTests.asmdef` required explicit `Fusion.Runtime.dll`, `Fusion.Common.dll`, and `Fusion.Realtime.dll` entries in `precompiledReferences` (due to `overrideReferences: true`) to resolve `CS0012` on `NetworkBehaviour`.
- `FishingRules.Default` initially invoked C#'s zero-initializing parameterless struct constructor, caught by Fishing EditMode tests and fixed in `FishSpec.cs` (along with SplitMix32 seed avalanche and stronger Floater/Sinker bias in `FishingMinigameSession.cs`).

**Unverified (requires live multi-client & human playtesting).**
- Live 2-to-8 peer Photon Fusion Shared Mode connectivity, replication, late join, and room leave/rejoin (`NET-01`, `NET-02`, `NET-03`).
- Live Master Client departure and `CoastalWorldRecord` authority transfer with two real clients.
- Human playtesting of the Stardew fishing minigame controls, feel, and visual polish.

## M1 delivery — 2026-11-25

The local coastal crossing is playable in [CoastalBenchmark.unity](../Assets/Aetherlands/Generated/Scenes/CoastalBenchmark.unity). Unity 6000.3.6f1 compiled the assemblies and opened the baked scene. Press Play, focus Game view and walk with WASD or arrows. The traveler starts on the deck facing the camera. The northern landing has a coastal station and a return route. M2 has not started.

| Delivered area | Implementation |
|---|---|
| Bake and ownership | Editor/CoastalBenchmarkBaker.cs, CoastalMesh.cs, PixelCanvas.cs, CoastalPixels.cs, CoastalLandscape.cs and CoastalSceneRig.cs under Assets/Aetherlands. Recipes/CoastalCrossing.asset selects seed 7319, timber color, speed and camera scale. Generated/CoastalManifest.json records revision 1 and owned outputs. |
| Presentation | Baked surfaces include segmented planks, bound posts, rope lattice, lanterns, irregular cliff terraces, shoals, palms, vegetation and a station with a tiled roof and canvas awning. URP depth sampling controls water opacity and shoreline foam. Twelve original sprite frames share an atlas. |
| Local play | Runtime/ contains the keyboard adapter, character motor, four-direction presentation and orthographic follow camera. Collision covers the deck, rails, cliff edges, station, barrels, palms and noticeboard. |
| Verification | Tests/EditMode and Tests/PlayMode exercise the generator and the actual baked scene. The final scene was active and clean in Edit Mode, with 12 roots. |

**Editor results.** Six EditMode cases passed. The identical-input bake retained every output byte and GUID. Changing timber color changed the wood texture and manifest only; the test restored the recipe. Three PlayMode cases passed in two consecutive runs. A virtual keyboard drove the normal input path across the deck and back, including stop, ground height and follow checks. The motor test measured diagonal distance and collision at both rails. The performance test sampled 90 warm frames and captured a moving traveler.

**Visual review.** Checked the entrance, midspan and landing at 1280 × 720. The timber edges, flag line, station and traveler remain identifiable; the shallow shelf and submerged rocks show through the water. The current cliff terraces and shoal outlines are visibly angular and the station is simpler than the references. Art approval remains with the developer. [Entrance](Captures/M1/Entrance.png) · [Midspan](Captures/M1/Midspan.png) · [Landing](Captures/M1/Landing.png) · [Movement](Captures/M1/Walk.gif).

**Profile and scope.** The last Editor sample on a Ryzen 7 7735HS and RTX 4060 Laptop GPU recorded 6.06 ms median and 7.13 ms p95. Mesh and Texture2D counts were unchanged across the sample; the whole Editor reported 14,656 GC bytes/frame. These are Editor-wide measurements. Standalone performance and physical-keyboard human playtesting are unverified. [Profile](Captures/M1/EditorProfile.txt).

**Issues encountered.** The initial water shader failed to compile; the final shader tests and renders passed after correcting the include and depth pass. Early keyboard tests intermittently lost virtual input when Game view lacked focus. The tests now use a temporary Input System configuration and restore it afterward. Package Manager logged three singleton errors during a domain reload, and MCP emitted a transport warning on reconnect. The final post-Play Console query returned zero errors and warnings.

## Baseline on 2026-11-25

M0 documentation delivered. No Aetherlands gameplay, benchmark scene, generator or multiplayer session was implemented in this milestone. Inspect this file and the relevant feature specifications before changing code.

| Component | Observed evidence |
|---|---|
| Unity | ProjectSettings/ProjectVersion.txt pins 6000.3.6f1, revision bbb010bdb8a3. The matching Hub editor is installed. |
| Rendering | Packages/packages-lock.json resolves URP 17.3.0. GraphicsSettings references Assets/Settings/PC_RPAsset.asset. PC and Mobile renderer assets exist. |
| Input and testing | The lockfile resolves Input System 1.18.0 and Unity Test Framework 1.6.0. Assets/InputSystem_Actions.inputactions exists. |
| Networking | Assets/Photon/Fusion/build_info.txt reports Fusion 2.1.3 Stable build 2390. The runtime DLL file version is 2.1.3.2390. NetworkProjectConfig.fusion and the vendor sample menu are present. |
| Scenes and ownership | Assets/Scenes/SampleScene.unity is present. Preserve the existing scene, input asset, vendor files and GUIDs. New game content belongs under Assets/Aetherlands/. |
| Source control | The Unity project root is outside a Git working tree. Its “2.5D game” child has a separate .git directory and ignore/attributes files. Changes at the Unity root are outside that child repository. Confirm the intended repository root before committing. |

## Verification and handoff

Inspected project files, installed editor directory, SDK metadata and local API documentation. Unity compilation, Console status, imports, play tests, visual quality, network connectivity and performance remain unverified. Photon credentials and region connectivity have not been validated. See [test procedures](TEST_PLAN.md) for the evidence required at each gate.

This baseline predates the M1 delivery above. M2 requires a separate request following visual acceptance.
