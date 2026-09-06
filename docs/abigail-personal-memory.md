# Abigail personal memory and activity evidence — 0.1.18

Approved scope: lasting personal details, natural follow-ups, and game-recorded mine visits so Abigail can question contradictory claims. Hearts, gifts and story progression remain under native game rules.

Gemini returns speech and up to three proposed details together in one structured request. The mod accepts only a bounded supported category, a short topic identifier, and a quote copied from the actual farmer message. It also keeps the complete source message so qualifications and negations are not lost. Proposals are still interpretations, never game facts. Corrections reuse a topic and category to replace an older detail; outcomes close a matching pending plan. Topic matching depends on Gemini correctly recognizing the correction.

Up to 64 personal details persist separately from the 32 recent exchanges. Existing saves load with an empty personal store. Recent conversation history remains available, but old conversations are not retroactively promoted into lasting details. New details save normally with the farm. Failures/cancellations do not commit a reply or its proposed memories.

Plans mentioning 'today' can be followed up the next day; explicit 'tomorrow' plans can be followed up after that planned day. Other dates have uncertain timing: after three days Abigail can ask whether the plan is still on, without presuming completion. At most one offered plan follow-up per game day is marked asked after a completed response reports asking it. Plans expire from the follow-up queue after 14 days; their statements can still be remembered. Follow-ups happen naturally when the player next talks to her, not through unsolicited popups. Date interpretation beyond the supported English relative words is intentionally uncertain.

The game records the first actual presence on a MineShaft level above zero each day, including ordinary mines and Skull Cavern floors. The entrance lobby does not count. This records entering a mine only, not mined ore, fights, time spent or rewards. Both location transitions and periodic current-location observation feed this record. The AI cannot write visits. A day observed from morning can support 'no recorded visit'; a missing/partial day is unknown. For today's incomplete day, absence only means 'not yet'. Keep 112 activity-day records; send the most recent 15 days as dated evidence.

At the user's request, Abigail can use this game evidence to question an explicit dated visit claim even if she did not personally witness it. She must not invent a witness or her own whereabouts to explain the evidence, infer the farmer's intent, or automatically impose a friendship penalty. This is limited to mine visits in this release, not universal lie detection.

F6 has a second page showing recent personal details and today's/yesterday's mine-visit status. The model never receives secret credentials in its context; API auth stays in the request header. No new server or separate extraction call is added.

Validation: deterministic tests for exact-source acceptance, corrections and JSON restoration, follow-up timing/deduplication, partial-record uncertainty and separate statement/activity write paths. Real Gemini probes cover extraction, a later-day greeting and an explicit contradiction. Live SMAPI checks exercise storage, a native MineShaft location, context generation, serialization and unchanged friendship. Full multi-day gameplay, physical mine travel and save/sleep/reload remain player acceptance checks.

API reference: https://ai.google.dev/gemini-api/docs/structured-output
