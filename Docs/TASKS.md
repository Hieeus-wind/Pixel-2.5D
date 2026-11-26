# Task register

| ID | Status | Deliverable and dependency |
|---|---|---|
| M0-01 | Complete | Inspect Unity root, versions, sample scenes and Git boundary. Record in PROJECT_STATE.md. |
| M0-02 | Complete | Document design, art direction, modules, generation, networking, roadmap and verification. |
| M1-01 | Implemented | Create palette, metrics, movement and camera settings in Assets/Aetherlands/Runtime/CoastalRecipe.cs and Recipes/. Build Editor/PixelCanvas.cs and deterministic recipe primitives. Depends on M0. |
| M1-02 | Implemented | Build Editor/ bridge, cliff, foliage and traveler recipes plus Editor/CoastalBenchmarkBaker.cs. Emit owned meshes, materials and point-filtered sprite atlases under Generated/. Depends on M1-01. |
| M1-03 | Implemented | Add Presentation/Shaders/CoastalWater.shader and generate Generated/Scenes/CoastalBenchmark.unity with dedicated camera, lighting, shore and collision. Depends on M1-02. |
| M1-04 | Implemented | Add Runtime/TravelerMotor.cs, keyboard input adapter, Runtime/TravelerView.cs and follow camera. Wire local spawn and directional frames into the benchmark. Depends on M1-03. |
| M1-05 | Implemented | Add relevant EditMode and PlayMode coverage in Tests/, run the M1 procedures, record captures and review issues. Depends on M1-01 through M1-04. |
| M2-01 | Implemented (Editor-verified; live multi-client unverified) | Implement Photon Fusion 2 Shared Mode multiplayer (`Networking/`, `CoastalSessionRunner`, `NetworkedTraveler`, `CoastalWorldRecord`, `MultiplayerHud`, `NetworkPrefabBaker`) with 8-player room capacity, version gate, late join, and Master Client reconciliation. Depends on M1. |
| M2-02 | Implemented | Implement Stardew Valley-style coastal fishing (`Fishing/`, `FishingFlowMachine`, `FishingMinigameSession`, 6 procedural coastal species, `FishingWorldVisuals`, `FishingOverlayView`, `FishingController`, `FishingCatalogBaker`) with 100% code-generated pixel art. Depends on M1. |
| M2-03 | Implemented (Editor-verified; live multi-client unverified) | Integrate fishing and multiplayer via `Integration/CoastalFishingNetworkBridge.cs`, wire `NetworkedTraveler.prefab` and `CoastalBenchmark.unity` (`CoastalSceneRig`, `CoastalBenchmarkBaker`), and verify 24/24 EditMode + 3/3 PlayMode tests in Unity 6000.3.6f1. Depends on M2-01, M2-02. |

Paths above are relative to Assets/Aetherlands/ unless given in full. M1 is playable and verified in the Editor; M2 is implemented and Editor-verified (live multi-client Photon tests and human playtesting remain unverified). See PROJECT_STATE.md for the delivery record. M3–M6 are gated in [the roadmap](ROADMAP.md). Update this register and project state together at each handoff.
