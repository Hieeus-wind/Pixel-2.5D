# AGENTS.md

## Mandatory Project Rules

- Read Docs/PROJECT_STATE.md before modifying code.
- Read relevant architecture and feature specifications.
- Use the installed Unity and Photon Fusion versions.
- Photon Fusion 2 Shared Mode is the only supported multiplayer topology.
- Never mix PUN, NGO, Mirror, Host Mode, or Dedicated Server implementations.
- Only State Authority writes authoritative Networked properties.
- Never trust client-provided damage or reward values.
- Design for late join and Master Client changes.
- All original graphical assets must be generated from code.
- Never import third-party graphical asset packs.
- Preserve the approved Art Bible.
- Do not replace detailed HD-2D visuals with permanent primitive placeholders.

## Code Quality

- Use descriptive English identifiers.
- Use namespaces beginning with Aetherlands.
- Prefer small classes with clear responsibilities.
- Avoid giant MonoBehaviours.
- Avoid unnecessary singleton managers.
- Avoid hardcoded gameplay balancing values.
- Use ScriptableObjects for reusable game definitions.
- Avoid per-frame memory allocations where practical.
- Cache component references.
- Pool frequently spawned objects when justified.
- Unsubscribe events and release owned resources.
- Do not manually modify generated output unless the generator is also updated.

## Unity Rules

- Preserve existing .meta files.
- Keep Editor scripts outside runtime assemblies.
- Use deterministic, idempotent Editor generation tools.
- Register generated network prefabs correctly.
- Never handcraft fragile Unity YAML when a supported Editor API is available.
- Never silently modify project-wide settings unrelated to the current task.
- Do not overwrite developer-authored scenes or prefabs without explicit authorization.
- Keep generated assets inside designated folders.

## Task Completion

A task is complete only when:

- Its acceptance criteria are satisfied.
- The implementation is integrated into a playable scene where applicable.
- Relevant tests have been run or explicitly marked as unverified.
- Known issues are documented.
- PROJECT_STATE.md and TASKS.md are updated.

Never fabricate compilation, profiling, test, or multiplayer results.

Do not start the next milestone automatically.

Use [the task register](Docs/TASKS.md) to identify the active milestone and [the test plan](Docs/TEST_PLAN.md) for its evidence requirements. Consult [the architecture](Docs/ARCHITECTURE.md) before adding a module. Record actual verification results separately from planned acceptance checks.
