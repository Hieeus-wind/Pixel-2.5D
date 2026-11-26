# Game design

## Player experience

Approach a coastal village along a wooden bridge. From the deck the player can see submerged rocks, the face of the cliff and a route into the settlement. The first local slice tests whether movement feels grounded and that route reads clearly at gameplay zoom. Crossing the bridge is its entire playable objective.

## MVP loop

Leave the village with a party, explore a streamed forest, gather materials, fight enemies, craft or equip an upgrade and return prepared for a cooperative boss. Persist character progression between sessions. Static scenery comes from the shared world recipe. Harvested nodes, enemy health and boss progress have room-level state.

| Feature | Proposed playable rule | Acceptance observation |
|---|---|---|
| Character | One archetype, keyboard planar movement, directional idle/walk, melee and a local follow camera | Diagonal movement matches axial speed. Equipment follows the sprite on turns. |
| Exploration | Village safe starting area leads to forest across adjacent chunks | Crossing and backtracking preserve the route and depleted resources. |
| Gathering and crafting | Interact within reach of an available node. Spend recipe ingredients for one output. | Two simultaneous attempts cannot consume the same last charge. Failed craft preserves inventory. |
| Combat | Proposed forest roster includes a rushing shore crab, a ranged spore creature and a slow armored guardian | Each has a distinct telegraph and recovery window. A swing hits an eligible target once. |
| Progression | Item definitions supply stable IDs, recipes and equipment modifiers | A saved character reloads its earned items, equipment and progression. |
| Boss | Party reads a windup, evades, then attacks during recovery | One encounter completion grants one eligible reward per player, including after authority migration. |

Enemy concepts and tuning are design proposals. Define ranges, timings, costs, loot and progression curves in versioned definitions during their feature milestone. The MVP excludes guild wars, global trade, competitive PvP, mounts and persistent MMO-scale simulation.
