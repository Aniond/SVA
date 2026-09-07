# Cave and mining atmosphere — 0.17.0

The cave update was verified in 0.17.0 and is retained in the installed NPCModern 0.18.0 clothing release. The normal installation passed 607 artwork registrations and 640 file checks; SolaceWeather 0.3 is also installed. The later clothing addition left all 630 prior PNG files unchanged.

The approved temperate-cave direction uses slate rock, retained warm mineral colors, and restrained water details. This update also includes the separately validated player HD companions and enlarged creator preview.

## Artwork

- 55 exact mining-node cells, including ordinary, ore, gem, geode, radioactive, volcano, coal and festival variants.
- Seven 32x32 rock clumps, including boulders, meteorite and mine rock variants.
- The live upperCavePlants sheet, preserving its mine debris and Summit uses.
- Selected stone surfaces on Maps/Mines/mine, Maps/Mines/mine_dark and Maps/cave. Timber, ladders, elevator, signs, saturated signals, water and functional tile layout remain intact. Ice and lava are not recolored.

There are 66 prepared patches across six sheets. Five existing sheets are extended and one new registration is added, bringing the registry to 607. Staging verified zero changes outside the authorized rectangles, zero native alpha/hidden-pixel changes, and zero overlapping conflicts. Node and plant detail preserves color relationships; the three atmosphere sheets intentionally change selected stone colors. All other prior artwork and player companions are retained.

Three named Stalagmite sheets have no confirmed live caller and are excluded. Existing pointed rock formations on the live mine atlas receive surface detail; no new collidable formations or map geometry are added. The concept image illustrates the intended atmosphere, not an exact replacement room or captured gameplay.

## Effects and sound

Small ground contact shadows support visible mining rocks. Glints and occasional drip/ripple particles are restricted to native water tiles beside a bank: at most 24 rock shadows, 12 water anchors and eight simultaneous particles. Cosmetic timing does not use the game's random generator. Lava and Skull Cavern wet effects are excluded.

Existing indoor light halos and native biome lighting remain authoritative. The native mine's existing drip opportunity is routed to nearby real water, with an 8–18 second cooldown; there is no second sound loop. Native upper/frost/lava ambience remains. Sound and ambience sliders, map transitions, pauses and the local music mute are respected. OpenArt's connected catalog currently exposes image/video modes but no standalone sound-generation mode, so no external audio generation was used.

Optional cave-atmosphere.json settings are Enabled, LocalizedDrips and Strength (default 0.18, capped at 0.35). Disabling the feature restores the original native drip call and removes its cosmetic drawing.

## Evidence and player checks

Staging: artifacts/mining-modern/candidate/preservation-report.json. Exact inventory and asset IDs: [mining inventory](mining-inventory.md). Before/after sheets and generation provenance are under artifacts/mining-modern/art and atmosphere.

Before-art native gameplay baseline passed 19 checks: real pickaxe removal of stones, copper, iron, amethyst and ruby; expected ore/gem drops; boulder rejection of a basic pickaxe and destruction with an upgraded pickaxe; original test-world state restored. These use a detached native-map fixture in an owned disposable farm, not real player saves. They do not test generated ladder placement, every drop variant or multiplayer.

The post-art interaction run also passed all 19 checks. Full native mine rooms at levels 15, 20, 60 and 100 were captured, with a matched effects-off/effects-on comparison at level 20. Images and room-state records are under `artifacts/new-game-persistence/phone-combined-20260906/Profile`: `cave-room-15-native.png`, `cave-room-20-native.png`, `cave-room-60-native.png`, `cave-room-100-native.png`, and `cave-room-20-effects-off.png` / `cave-room-20-effects-on.png`.

The recorded sound settings were Master 1, SFX 1, Ambience 0.75 and Music 0. No externally generated audio was used. These settings and successful native checks do not prove how the drips sound: human listening and sound-balance review remain pending. Farm-cave and volcano/Skull Cavern comparisons remain useful player checks beyond the captured mine rooms.
