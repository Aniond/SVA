# NPC Modern 0.9.0 visual effects

Adds three optional cosmetic effects while retaining the 345 accepted textures:

- Lighting: restrained colored glow around visible native lamps and windows, with gentle flicker for flame-style lights. Native light states stay in control.
- Shadows: soft projected silhouettes for the twelve reviewed Town exteriors, placeable buildings, and growing trees. Direction and length follow daytime; rain and snow soften them, thunderstorms suppress them. Authored Town positions apply to the native 130 by 110 layout.
- Atmospheric fog: subtle drifting mist following local weather, season and time. This is atmospheric mist, not unexplored-map concealment.

Default strengths are lighting 0.65, shadows 0.55 and fog 0.5. All are enabled. In the SMAPI console, use `modern_effects status`, `modern_effects on`, `modern_effects off`, or `modern_effects fog 0.3`. Replace fog with lighting or shadows; each also accepts on/off. Settings persist in the mod folder's visual-effects.json; `modern_effects reload` reads manual changes. The installer preserves this file.

The effects render below the interface and are skipped during cutscenes, minigames and map screenshots. Animation pauses with the game clock. Mist and solar shadows are outdoor effects; lamp glow can also appear indoors. Resources are separate for split-screen views and are disposed on return to title or settings reload.

## Validation

Implementation tests: 26 passing. Production build: zero warnings/errors. All 18 GPU checks pass, including real Town caster alignment, animation, disabled pixel identity, draw limits and graphics-state restoration. Independent visual/source review found no remaining blocking issue. The full suite passed 99 reports initially; six older screenshot exports exhausted memory and all six passed in a fresh focused process. Aggregate: 105/105 reports passed across those runs (artifacts/visual-effects/regression-0.9.0.json). All 345 artwork hashes and the registry remain unchanged. Installed 349 verified files after backup to artifacts/mod-backups/AbigailModern-20260906-024751. Normal startup with the usual installed mods passed: 345/345 textures and all 349 installed files verified. Evidence: artifacts/npc-modern/evidence/installed-0.9.0.json. The owned verification game was closed. No saved farm was loaded.

GPU fixtures use actual production rendering over native Town artwork, with explicitly synthetic light/caster positions and a demonstration night tint. They test rendering behavior without loading or modifying a saved farm. They do not replace gameplay testing of every location, split-screen setup, or other graphics mods.

## Player checks

Restart Stardew through SMAPI. In Town, compare morning and afternoon shadows, visit a lit building after dusk, and check mist during a rainy morning. Try `modern_effects off` and `modern_effects on` to compare. Check your preferred zoom and indoor lighting; tune each strength to taste. Existing native shadows and light behavior remain present underneath these additions.
