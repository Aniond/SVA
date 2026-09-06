# Weather particle upgrade

Style: the approved Polished Stardew direction, with familiar colors and clearer small details. User selected occasional thunderstorm hail, visual only.

This batch covers ordinary rain and splash frames, green rain, snowfall, seasonal wind petals/leaves and snow flecks, lightning bolts, and a new four-frame hail particle. Native rain/green-rain timing, snow transparency preference, wind motion, lightning strike behavior and screen-flash preference remain controlled by the game.

Green rain uses its native green tint and doubled draw. The new source art stays neutral so that tint works correctly. Lightning retains the native bolt segment endpoints for stacked, mirrored strikes. Hail has no native weather type in the current source or SolaceWeather weather enum, so its implementation is a visual layer during eligible storms.

Working assets, prompts, preparation scripts and coverage: `artifacts/weather-modern/`. The hail sheet was generated with the built-in image tool and prepared at 64×16 pixels. Rain/lightning and snow/wind are prepared separately and merged into the existing texture registry; shared atlas edits must preserve all previously installed NPC, terrain and prop artwork.

Version 0.8.3 is installed and verified through a normal SMAPI startup with the other installed mods: 345/345 textures and 349 package files passed. Evidence: `artifacts/npc-modern/evidence/installed-0.8.3.json`. Installed startup session: `artifacts/npc-modern/installed-audits/0.8.3-20260905-214735-592`. Both owned test processes were stopped after their checks.

Coverage:

- Rain and green rain: eight 16×16 frames, tapered streak, impact, ring and crown splash. Green coloring and duplicate drawing remain native.
- Snowfall: sixteen 75 ms frames, preserving native particle origins, the 1.2-second loop and alpha-50 background veil. One generated crystal stamp follows the original wrapped trajectories.
- Wind: eleven pink-petal, eleven green-leaf and eleven autumn-leaf frames, plus five tiny snow variants. Shared Woods/IslandHut leaf effects also receive these texture updates.
- Lightning: the native 37×57 bolt region, with original segment endpoints retained for stacking and random mirroring. Screen flashes, preference controls, strike targeting and damage are unchanged.
- Hail: four new 16×16 frames drawn at 1.5× scale. A deterministic twelve-game-minute window in each two-hour period allows brief hail during eligible local storms. At the default clock this is roughly eight seconds per eighty-four seconds. A maximum of 24 particles, private random state, and local weather checks limit the effect. Indoors, green rain, snow, events, menus, paused time, screenshots and Summit exclude hail. No hail forecast, damage or saved weather type is added.

Verification completed:

- Production and audit projects build successfully. The production build has no warnings; the audit project has four analyzer/nullability warnings.
- 345 registered textures and 349 packaged files verified against the isolated loaded package. ZIP: `dist/NpcModern-0.8.3.zip`.
- All 101 isolated audit reports passed in `artifacts/npc-modern/runtime-audits/0.8.3-20260905-214508-486`.
- Native weather rendering: eight rain/green-rain cases, 32 snow cases at two transparency settings, 38 wind cases, three manually selected lightning fade levels. Actual native cached weather textures match loaded content. These are injected-frame fixtures, not a played-through weather day.
- Hail: actual emission and motion on a private map, all impact stages and expiry, four actual rendered frames, eligibility and schedule checks, viewport clipping and pause clearing. The game random counter stayed at zero. Pure helper tests also passed.
- 342 prior textures are byte-identical. All 109,720 previously registered Cursors pixel visits remain exact. All 17,805 weather-region pixel visits match prepared artwork. See `artifacts/weather-modern/preservation-checks.json`.
- Root reviewed before/after rain/lightning, stacked lightning joins, tiled snow and wind contacts, and actual rendered snow, green rain and hail. Snow/wind animation previews and loop checks are included in their handoff folder.

Installation folder: `C:/Program Files (x86)/Steam/steamapps/common/Stardew Valley/Mods/AbigailModern`. Previous 0.8.2 backup: `artifacts/mod-backups/AbigailModern-20260905-214731`.

Restart using SMAPI. On suitable weather days, check rain splashes, green rain, snow transparency, wind leaves, lightning and occasional storm hail. No farm was loaded or save written during testing; live weather-event subscriptions and a complete day of play were not exercised by the isolated fixtures.

Prompts and generated sources are saved under `artifacts/weather-modern/hail`, `rain-lightning` and `snow-wind`. All source artwork used the built-in image tool; preparation scripts preserve native sheet coordinates and compose the generated particle details into the game assets.
