# Lighting, soft shadows and atmospheric fog

User approved all three proposed effects. Extend the existing NPC Modern SMAPI rendering integration; retain all 345 accepted artwork files and the SolaceWeather gameplay/weather system. This plan makes the approved direction concrete; no further permission is needed for local implementation, verification, backup and installation.

Design: optional cosmetic passes with independent toggles and strengths. Lighting adds restrained warm/color halos and flicker around real light sources. Outdoor tree and building shadows project with solar time and soften in poor weather. Seamless drifting atmospheric fog responds to season, time and local weather. No fog-of-war exploration rules, saves, forecasts, collision or progression changes.

Structure: `VisualFrame` immutable per-frame inputs, normalized `VisualSettings`, three independent effect renderers and pure testable policies, a root SMAPI adapter that snapshots native state and selects draw stages. Each renderer owns/disposes its generated GPU textures. Graphics primitives/noise are code-native rendering resources, not replacements for the accepted sprite art. Avoid altering native lights, global random state, graphics targets or native textures.

- [x] Implement/test lighting policy and renderer.
- [x] Implement/test solar shadow policy and renderer.
- [x] Implement/test weather fog policy and renderer.
- [x] Integrate eligible world draw stages, per-screen state, settings commands, cleanup and resource limits.
- [x] Render GPU fixtures for all three effects, disabling and weather/time cases; verify unchanged graphics/native game state.
- [x] Independently review and inspect comparisons; run existing regression audits.
- [x] Package release, back up current installation, verify normal startup, document player checks.

The current repository is an unborn branch with all source untracked. Keep this continuation in the existing workspace and preserve unrelated files. Do not create a baseline commit, modify saves/configuration belonging to other mods, or require extra user approvals already covered by this request.
