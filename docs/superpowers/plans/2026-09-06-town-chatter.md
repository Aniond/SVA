# Town chatter implementation plan

**Approved scope:** Coordinator relayed user approval for the first twelve existing personality profiles and Abigail-first testing. Two nearby NPCs exchange public street chatter through speech bubbles only. No voiced audio. Phone block mapping remains a separate unresolved decision.

**Architecture:** A public-only typed context and bounded Core state own generation input, cadence and witnessed speech. A separate Gemini request returns two short attributed lines. A game controller uses compact wrapped overhead bubbles without changing movement or schedules. RomanceService exposes witnessed public speech to later in-person/phone conversations without copying private memory into chatter requests.

**Constraints:** Stardew 1.6.15, SMAPI 4.5.2, single player; existing Gemini credentials/model; no save/config/art overwrite; no push. One coordinated disposable game process. First live pair includes Abigail.

- [x] Add failing Core tests for daily/pair/time limits, farm identity, bounded attributed history and provider schema/privacy/invalid replies; implement Core state/provider.
- [x] Add Town controller: visible idle supported pair within three tiles, farmer within six; no menus/events/sleep/native bubble; two sequential compact wrapped 4–6 second bubbles; cancel on changed location/day/farm/proximity or occupied speaker. At most three attempts/day, two game hours and sixty real seconds between attempts, each pair once/day. Fail quietly without retry or invented fallback.
- [x] Build public weather/current town flags and next-seven-day native festival snapshot. Exclude private player details, romance, gifts and chat histories. Add optional witnessed chatter context to existing Living Memory conversations.
- [x] Verify Core and Release builds, request independent review, then use isolated Abigail/partner harness for generation, sequential captures, cancellation, save/reload and fresh-farm checks. Coordinate slot with Assets.
- [x] Document actual evidence/limits and commit locally. Keep normal installation unchanged until coordinator integrates the combined build.
