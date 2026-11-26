# AetherForge

AetherForge turns a recipe, palette, generator version and seed into original baked art. Store authored inputs under Assets/Aetherlands/Recipes/ and owned outputs under Assets/Aetherlands/Generated/. Keep hand-authored shaders and tooling outside the output tree.

## First bake

1. Define a versioned palette and integer PixelCanvas supporting clipped pixel, line, filled shape and layer operations. Use a specified integer PRNG with independent stable seeds for plank, cliff, plant and traveler variations. Derive seeds from explicit IDs and coordinates using a fixed algorithm.
2. Bake wood grain, cut ends, rope fibers, rock planes, foliage clusters and character layers. Configure point filtering, a common pixel scale, anchors and explicit atlas padding. Compose directional frames from aligned body and equipment layers.
3. Build bridge deck, posts, rope, cliff shelves and shore geometry in world units. Generate a simple collision surface separately from detailed visible geometry. Bind shared materials; bake common meshes, atlases and materials in the Editor.
4. Provide a preview and a repeatable bake command. Validate inputs before writing. Keep a manifest mapping recipe ID and version, seed, input digest and generator version to owned output paths. An identical bake reuses existing assets. Update changed owned assets in place to retain GUIDs. Report collisions with unowned paths and stop.
5. Generate the benchmark into a dedicated owned scene through Editor APIs. Validate content and prefab references before publishing a successful bake. On interruption mark the bake incomplete; rerunning reconciles owned outputs. Clean obsolete output only within the manifest's ownership scope.

## Lifetime and extension

Baked assets belong to the project. A preview owns and destroys its temporary meshes, textures and materials on rebuild, close or reload. A streamed chunk owns its runtime instances and releases them on unload; shared assets remain cached while referenced. Avoid per-frame mesh/texture creation. Cache keys include every recipe dependency and version.

Extend the same contract to buildings, equipment, item icons, generated UI and audio synthesis at the relevant milestones. Before networking a generated entity, bake its prefab, register it with Fusion and validate a runner spawn. Static bridge scenery uses normal local assets.
