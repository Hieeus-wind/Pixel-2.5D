# Aetherlands Online

A cooperative coastal fantasy action RPG for two to eight players. Original pixel-art characters inhabit a three-dimensional world of timber bridges, terraced cliffs and shallow turquoise water. The local M1 benchmark is playable in Unity Editor.

## Open the project

1. In Unity Hub, add the folder containing this README, Assets, Packages and ProjectSettings.
2. Open with Unity 6000.3.6f1. Allow package resolution and project import to finish.
3. Open [CoastalBenchmark.unity](Assets/Aetherlands/Generated/Scenes/CoastalBenchmark.unity), press Play, click Game view and use WASD or the arrow keys. Walk along the bridge to the station, then return to the southern landing. Exit Play Mode to edit.

Rebuild using **Tools > Aetherlands > Bake Coastal Benchmark**. The generator reads the CoastalCrossing recipe under Assets/Aetherlands/Recipes. Review [the captured crossing](Docs/Captures/M1/Midspan.png) and [the recorded walk](Docs/Captures/M1/Walk.gif).

## Developer reading order

Start with [project state](Docs/PROJECT_STATE.md) and [working rules](AGENTS.md). The [game design](Docs/GDD.md) defines the playable loop. The [art bible](Docs/ART_BIBLE.md) sets visual review criteria. Consult the [architecture](Docs/ARCHITECTURE.md), [networking contract](Docs/NETWORKING.md) and [AetherForge specification](Docs/PROCEDURAL_ENGINE.md) before editing the relevant systems.

Use the [roadmap](Docs/ROADMAP.md) for milestone gates, the [task register](Docs/TASKS.md) for current scope and the [test plan](Docs/TEST_PLAN.md) for verification procedures.
