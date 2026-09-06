# Egg Festival weather pilot

The existing Spring 13 festival remains in charge of admission, dialogue questions, NPC positions, egg placement, the hunt clock, scoring, and rewards. The mod adds two small striped canvas shelters at Pierre and Gus during the initial gathering. Their positions are captured from the actual festival actors, so yearly layouts are respected. Posts and roofs are drawn with the game's own solid-color texture. They never add map tiles, collision, pathfinding changes, or permanent assets.

All added shelters disappear as soon as the game's main festival event starts, before the hunt. This deliberately preserves visibility of every egg and all movement routes. They are visual shelter only: NPCs are not rerouted and rain particles are not blocked beneath the roof.

Pierre, Gus, Abigail, Penny, and Emily each receive at most one additional weather-aware page per event. The successful `Event.TryGetFestivalDialogueForYear` hook calls `EggFestivalController.AppendDialogue`. It appends a literal `DialogueLine` to the original dialogue object, retaining all original text, callbacks, translation keys, and side effects. Lewis's start conversation and spouse/roommate variants are left alone. Remarks are authored in English; this pilot does not provide translated remarks.

No festival content asset or saved NPC state is edited. The per-event state uses weak references and clears on returning to the title screen. Decorations are only drawn while the current enabled event is the Egg Festival. Other festivals receive no decoration or conversation changes from this controller.

The native game pauses ambient-light updates during events. When pilot weather changes within the outdoor Egg Festival, the weather runtime now applies the same rain/clear ambient colors used by the game. This prevents a rain-to-clear switch retaining the rainy tint and becoming dark blue. This adjustment is restricted to the Egg Festival; other event lighting is preserved. The weather HUD button is hidden during events, matching its unavailable input behavior.

## Verification

Implementation was checked against decompiled installed Stardew Valley 1.6.15 `Event` and `Dialogue` classes: the festival ID is `festival_spring13`; `forceFestivalContinue` sets `eventSwitched` before starting the main event; festival conversation is constructed by `TryGetFestivalDialogueForYear`; and `Dialogue.dialogues` is a public list of `DialogueLine` objects.

Live verification on September 4, 2026: eight gathering checks passed, including both shelter anchors, all five once-only remarks, unchanged Lewis dialogue, and unchanged native event data. The native hunt started with its timer running and 32 original egg props; decorations were hidden. Clear and rainy screenshots were visually reviewed and both native ambient-color checks passed after the lighting fix. See `testing.md` for evidence locations.

Hands-on checks still required: normal festival admission, interaction prompts and conversations, collecting eggs along the normal routes, finishing and receiving the reward, and returning to ordinary play. Repeat in clear/rainy weather and with the feature disabled; later-year layouts and snow remain additional checks. The controlled debug staging does not prove a full player-completed festival.
