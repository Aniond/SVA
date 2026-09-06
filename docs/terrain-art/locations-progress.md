# Interiors and outdoor locations — 0.12.0 complete

Installed 103 location atlases: 30 interiors, 31 outdoor/cinematic sheets, 22 mine/volcano sheets and 20 additional seasonal sheets. Native seasonal, festival and story variants are included.

![Rendered native map layers](../../artifacts/locations-modern/overview.png)

## Preservation

The registry now contains 463 textures: 81 new and 22 extended entries. All 360 other previous files remain unchanged. Independent review verified 1,794,977 previous artwork pixels and every previous patch rectangle. All 259 native map hashes remain unchanged. Localized tile protection restored 20,674 pixels; no newly registered pixels overlap locale-sensitive cells. Dimensions and transparency remain exact.

Thirteen canonical Maps textures are intentionally preserved: actors, object icons, UI, path/control data, four seasonal shadows, two festival text sheets and the black Maru silhouette. VolcanoLayouts/Layouts is procedural data. All 103 production donors, prompts and provenance remain in the four worker folders under artifacts/locations-modern. One redundant winter beach generation is unused.

## Validation

- Isolated and normal startup: 463/463 textures and 467 package files verified.
- 259 maps, 106 atlas dimensions, 919,629 source frames and 14,478 animated tile placements checked; 24 map/season previews rendered.
- 23 existing visual-effects checks passed.
- Final peak game memory: 1,696,845,824 bytes (1.58 GiB).
- Fresh normal launch: music/player volumes 0, sound effects 1, ambience 0.75.
- Backup: artifacts/mod-backups/AbigailModern-20260906-141951. Package: dist/NpcModern-0.12.0.zip.
- No farm loaded or save written; owned test processes stopped.

An earlier audit crashed at 30.8 GB game memory. Repeated full-image GPU loading for every small artwork patch caused the growth. Production now caches source raw pixels once. The audit processes one map at a time and enforces a memory limit. Baseline and final tests subsequently passed.

Evidence: [isolated](../../artifacts/npc-modern/evidence/isolated-0.12.0.json), [installed](../../artifacts/npc-modern/evidence/installed-0.12.0.json), [preservation](../../artifacts/locations-modern/preservation-checks.json), [independent review](../../artifacts/locations-modern/review.md), [audio](../../artifacts/locations-modern/normal-audio-runtime-status.json).

## Player checks

Visit rooms and outdoor areas across seasons; check doors, walking boundaries, events and night lighting. Previews render native map layers rather than full live scenes. NPCs, runtime furniture, interactions and weather overlays require normal gameplay checks. Animated source bounds were checked; previews show the first frame.
