# Delivery roadmap

Each gate requires recorded evidence and a project-state update. Proceed only on request after the preceding gate.

| Milestone | Depends on | Exit criteria |
|---|---|---|
| M0 Documentation | Repository inspection and references | All eleven documents exist, agree on installed versions, boundaries and authority, and give actionable slice tests. |
| M1 Local coastal crossing | M0 | Repeatable bake yields an original bridge, cliff landing, vegetation, depth-aware water and animated traveler. Keyboard movement crosses both ways, with stable camera and collision. Pass M1 tests and visual review. |
| M2 Shared coastal crossing | M1 and configured Fusion app | Two peers cross together. Eight-player capacity tested. Late join and Master Client departure preserve world identity and control. |
| M3 Streamed village and forest | M2 and stable world keys | Stream adjacent chunks locally, return to the village without seams or duplicate entities, retain dynamic node state after unload and migration. |
| M4 Combat and gathering | M3 and versioned gameplay definitions | One melee archetype, three distinct enemies and gatherable nodes. Repeated requests and competing players yield one authoritative outcome. |
| M5 Craft, gear and progression | M4 and stable item/grant IDs | Craft, equip, save and reload. Generated UI/icons and sound communicate outcomes. Interrupted saves recover the last good state. |
| M6 Cooperative boss and MVP QA | M5 | Two to eight players can complete the village-to-boss loop. Verify join, migration, rewards, frame timings and memory on a recorded target machine. |

## M1 implementation order

Build palette, pixel canvas and bake tests first. Produce a small bridge-to-cliff composition and lock the camera scale before detailing textures. Add depth-aware water, then the traveler, collisions and follow camera. Finish with repeated bakes, walk-throughs and visual review. Planned deliverables and paths are in [the task register](TASKS.md).
