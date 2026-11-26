# Architecture

Use a feature-based modular monolith. Start with the assemblies needed by the current milestone. The following paths are proposed beneath Assets/Aetherlands/. Introduce feature folders as their behavior arrives.

| Boundary | Responsibility | Allowed dependencies |
|---|---|---|
| Domain/ | Pure movement constraints, combat, inventories, recipes, progression and stable IDs | C# value types and domain rules |
| Definitions/ | ScriptableObject catalogs and strongly typed settings; validate unique IDs and resolve references at load | Domain and Unity |
| Gameplay/ | Local movement and interaction controllers; coordinate world, characters and later combat features | Domain, definitions, Unity physics and input adapter |
| Presentation/ | Sprite layers, camera, animation, shaders, UI and sound | Read-only feature state, Unity and URP |
| AetherForge/ | Deterministic recipes and pixel/mesh construction | Definitions, domain and Unity data types as needed |
| Networking/ | Fusion lifecycle, spawn policies, snapshots and request validation | Fusion, domain and definitions; adapt results to gameplay |
| Persistence/ | Versioned JSON DTOs, migrations and save/load at safe points | Domain and stable definition IDs |
| Editor/ | Baking, validation and scene assembly through supported Editor APIs | Runtime modules and UnityEditor |
| Tests/ | EditMode domain/generation tests and PlayMode scene/network checks | Modules under test; test-only assembly references |

A scene composition root wires dependencies. Local input produces a movement intent. Gameplay consumes it locally in M1. In M2 the owning player adapter consumes intent during network simulation and publishes pose/facing. Presentation reads that state for both local and remote characters. Keep the camera and input local. Domain rules must run without a runner, scene or editor. Keep editor assemblies platform-restricted and dependency direction acyclic.

Use English identifiers under Aetherlands namespaces. Keep balancing data in reusable definitions. Cache references, unsubscribe listeners on teardown and measure repeated allocation before adding pooling. Use content IDs for saves and world keys for persistent entities; Fusion object IDs identify live instances only. Validate content versions before joining a world. For JSON saves write a validated temporary snapshot then replace the last good save, retain a recovery copy and reject unsupported schemas explicitly.
