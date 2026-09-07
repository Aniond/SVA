# Five character profile refinements

Additive to Emily source commit a79ceb21e405dcff7447e2b272a2a64729887609.

Abigail is adventurous and independently minded with room for nerves; Haley keeps her early sharpness and earns warmth; Penny is gentle with agency; Alex balances athletic confidence with earned vulnerability; Maru is approachable and curious. Family, friendship, private events, and shared memories remain limited by available game context. Existing quests, rewards, schedules, and relationship trees remain unchanged.

All five use delighted, thoughtful, concerned, stern, and neutral structured reactions. Existing portrait art is reused: delighted cell 1, concerned cell 2; Abigail thoughtful 4 and stern 6; other characters use neutral cell 0 where a matching expression is unavailable. The dialogue window preserves these reaction labels. Older stored reaction names remain supported. Emily benefits from the same dialogue-window correction.

Penny receives the native pamHouseUpgrade flag as context for Pam's home. It does not assert that Penny still lives there after marriage or that the player funded it.

## Validation

- 246 core tests passed; full Release build passed with zero warnings or errors.
- Five live production-provider samples succeeded, one per character, using gemini-3.8-flash. Each returned a valid structured reaction without text prefixes or unexpected quest requests. Penny's upgraded-home sample did not call it a trailer or assume the player paid.
- Native dialogue data was inspected for family, interests, and event-dependent details.
- Code review found and corrected the dialogue window's legacy reaction normalization.
- Provider samples are not a substitute for native game or human playtesting. Assets owns the combined installation and native verification.

## Tomorrow's player checks

Talk with each character at the current friendship level; compare their voices and confirm that they do not invent past shared events. Check a delighted or concerned reply displays a suitable portrait. Talk with Penny after Pam's house upgrade. Try phone messages and existing Abigail/Haley quests to confirm continuity.

Mouse-control work is parked separately and is excluded from this handoff.

## Combined integration verification

The combined Solace 0.5 source with NPC Modern 0.22 passed 133 native checks across isolated process groups on September 6, 2026: profile reactions and Penny home context 37, tailoring recovery 7, tailoring isolation/restored artwork 9, phone 23, exchange 24, chatter 14, cemetery 8, fashion 3, and garment IDs/pixel hashes 8. Fresh copied-save load retained all four orders, equipped IDs and non-default dyes, with SaveParsed preceding native farmer restoration. No provider request was issued.

The corruption fixture exposed a stale fallback texture during artwork restoration; publication now invalidates garment textures after repopulating their images. Four restored pixel hashes pass. Earlier fixture failures and corrected ordering are retained in the evidence. Four standing Town directions were visually reviewed with the HD player body; no obvious garment clipping or sleeve/body seam was seen. This does not cover every tool or movement frame. Evidence: `artifacts/emily-integration/runtime-summary.json`; images: `artifacts/new-game-persistence/phone-emily-combined-20260906-205143/restored-facing-0.png` through `restored-facing-3.png`.
