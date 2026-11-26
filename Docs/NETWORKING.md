# Fusion Shared Mode contract

Target the installed Fusion 2.1.3 build 2390. Consult Assets/Photon/Fusion/Assemblies/Fusion.Runtime.xml when implementing. Start a runner using GameMode.Shared. Give the room a capacity of eight. A session becomes playable cooperatively with two peers. Require matching content and generator versions before enabling world interaction.

| Object | Lifecycle and writer | State needed by a late joiner |
|---|---|---|
| Player | Each peer checks its existing player association, uses Runner.Spawn once for its local player and registers it with SetPlayerObject. Only its State Authority writes pose/facing. | Current pose, facing, equipped visual IDs and action state |
| World record | Place one designated room record in the network scene. Configure master-client ownership and retain it when its current owner leaves. | Seed, generator/content versions and active world records |
| Monsters, nodes, events and room rewards | Current Shared Mode Master Client manages lifecycle. Use master-client objects for retained entities. Check HasStateAuthority before every mutation. | Stable world key, health/charges/phase, cooldowns and processed action or grant records |
| Decorations, camera, input, UI | Generate or construct locally. Static rendering never requires a network spawn. | Derive from recipes and replicated state |

Use Networked properties for state a peer must reconstruct after joining. Use RPCs for transient cues and requests. Send intent, target and an action sequence; derive sender identity from the transport and derive damage/reward from definitions. Validate sender, ownership, reach, cooldown, target phase and action freshness. For player damage, route the accepted result to the player's State Authority. Shared clients can tamper with their own process. Prototype JSON and Shared Mode cannot secure a persistent economy.

## Recovery and duplicate handling

Keep stable spawn keys and completed action/grant identifiers in retained room state. On Master Client change, wait for actual State Authority, rebuild local indices from live records and reconcile missing keys before enabling spawns. A retry with the same player/action key returns its existing outcome. Define bounded per-player sequence windows and encounter-scoped grant IDs before implementing combat. Keep an outstanding grant until the recipient acknowledges it. Apply the item and its consumed grant ID together in the inventory snapshot so resends are idempotent.

On late join, read versions and seed, load local static chunks, bind existing entities, apply current state, then enable input. Store dynamic changes independently of local chunk visibility. A local unload releases visuals; it cannot reset a node or despawn a room enemy. Exercise master departure mid-action and late join after depletion in the [test plan](TEST_PLAN.md).
