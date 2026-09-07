# Abigail cemetery investigation implementation plan

**Approved:** Conversation-proposed playable outing, explicit acceptance, Abigail meets the farmer at the cemetery at night. Initiating conversation at that accepted meeting immediately starts the scene. Noncombat ghost investigation; do not hijack ordinary conversations. User authorized routine timing and implementation choices and asked to continue.

**Architecture:** One supported cemetery template with per-farmer Core state and native quest-log entry. Existing conversation response schema can request the template but cannot accept it or run arbitrary actions. Reuse existing leisure-window checking and explicit conversation buttons. The game service owns Abigail's temporary meeting placement, cleanup, three validated ten-minute scene steps and cosmetic Ghost texture drawing. SharedExperienceStore records only actual completion.

**Defaults:** Next verified free night within seven days, 20:00 meeting with 30-minute grace, up to 30 minutes of investigation, no money/items/combat/romantic commitment. Existing 19:30–21:00 leisure check conservatively covers the meeting window. Decline has a three-day offer cooldown; misses and changed conditions allow rescheduling without a punishment. Only one such outing and no conflicting shared activity.

- [x] Core failing tests then state: offered/accepted/active/completed/missed, explicit acceptance, ordered scene steps, stale choices, cancellation/reschedule, save ownership and no duplicate completion.
- [x] Add validated Gemini cemetery proposal and existing conversation choice hooks; accepted cemetery conversation intercept precedes phone offer/input only at the valid meeting.
- [x] Locate cemetery using actual native map data; stage Abigail only for the accepted meeting, preserve/restore NPC schedule and player control. Add native quest guidance and three visible investigation beats with cosmetic ghost and no monster entity.
- [x] Record verified shared experience; persist state and restore scene/meeting safely across day/title/save transitions. Decline/miss cannot complete or invent memories.
- [x] Build, focused tests and independent review. Coordinate isolated native game slot after Assets; validate invitation, meeting-trigger conversation, ghost visibility, steps, cleanup, save/reload and no-combat effects. Paid proposal test bounded; no extra cosmetic calls.
- [x] Document evidence and commit locally without push or overwriting main creator/art/movement work.
