# Moonlight and sky stars — NPC Modern 0.9.1

Soft blue moonlight fades in outdoors between 8 p.m. and 11 p.m. and stays through late night. Rain reduces it, snow softens it, and thunderstorms/green rain suppress it. Warm local lights render afterward. The tint does not change native light or weather state.

Clear-night stars twinkle in the Summit's visible sky. A conservative boundary keeps them above native foreground and distant mountain artwork as the camera moves. Ordinary overhead Town/farm maps have no visible sky, so stars do not appear over the ground. Rain, snow and storms hide them. Both additions follow the existing cutscene/minigame/screenshot exclusions and pause animation with the clock.

Both start enabled at strength0.65. SMAPI console controls:

- `modern_effects moonlight 0.65`
- `modern_effects stars 0.65`
- `modern_effects moonlight off` or `modern_effects stars off`
- `modern_effects status`

Values range0to1 and save to the mod's visual-effects.json. Existing settings remain valid; missing new fields get the defaults. Both effects own a shared1pixel texture per screen; at most1moonlight draw and96star draws. They never change render targets or native textures.

Validation:33behavior tests pass, productionbuild has0warnings/errors. All23GPUchecks pass, including the18previous effects checks and5moonlight/star checks. Isolated runtime verified345/345textures and349packagefiles. Evidence: artifacts/npc-modern/runtime-audits/0.9.1-20260906-025728-339. Installed0.9.1after backup to artifacts/mod-backups/AbigailModern-20260906-025941; normalstartup verification passed345/345textures and349installedfiles with the usual mods. Evidence: artifacts/npc-modern/evidence/installed-0.9.1.json. The owned test game was closed. Independent source/image review found no remaining blockers. GPU moonlight cases use native Town artwork with an illustrative night tint. Star cases use a synthetic dark sky to check clipping/twinkle, not live Summit scene playback. Four cutoff tests cover native mountain-layer geometry and camera offsets.

Player check: restart through SMAPI, compare Town at8p.m. and11p.m.; visit the Summit on a clear night, pan the camera, and compare rainy-night behavior. Try individual toggles and preferred zoom. Full saved-game and split-screen playtesting remains a manual check.
