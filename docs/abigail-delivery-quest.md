# Abigail: Trust and Follow-Through — 0.1.21

Abigail remembers whether you keep an explicit promise. Stardew's hearts, gifts, dating and heart events retain their normal rules. F6 describes her outlook and the moments behind it; there is no extra meter.

| Request | Meaning | Availability | Timing |
| --- | --- | --- | --- |
| One fish, any species | A casual favor | First available request | Open-ended |
| One Quartz | Curiosity about unusual stones | After carrying Quartz | Tomorrow or three days |
| One Iron Bar | Preparing for her own adventure | After carrying an Iron Bar and completing another favor | Tomorrow or three days |

These are authored personal requests, not claims about original story events. Only one request can be outstanding. Completed favors do not repeat.

Abigail can propose a request in conversation. Use a choice **below chat** to accept or decline. Typing agreement alone does not create a promise. The ordinary quest journal shows an accepted promise and its agreed date. Before that date has passed, **I need more time** grants one two-day extension without a penalty. **Not right now** declines an offer without a penalty.

Return with the item anywhere in your backpack and choose **Give Abigail: [item]**. Quest item names appear purple. Holding an item is unnecessary. The game rechecks the item and your distance from Abigail, removes exactly one item, records the actual delivery and refreshes her AI context. Discussion alone never delivers anything. F9-protected items and items reserved for other quests are kept. For fish, the game prefers lower quality, then lower value.

Promises become overdue the morning after their agreed date. A missed or abandoned timed promise creates one disappointment; passing days, repeated clicks and apologies do not pile on penalties. Delivering later repairs that request's contribution while preserving the history. An abandoned request can be offered again after two mornings. Fish favors never become overdue and carry no abandonment penalty. While a promise remains outstanding, another cannot be proposed.

Trust contribution per request is based on its importance: fish 1, Quartz 2, Iron Bar 3. On-time completion contributes that weight; a failed timed promise contributes its negative weight once. Late or abandonment repair replaces the earlier contribution with max(1, weight minus 1). Combined results guide Abigail from cautious, through still learning and starting to rely on you, to consistently dependable. These numbers are internal and are not shown as a player meter.

Gemini expresses the reaction; it cannot change trust, dates, items or native friendship. Apologies and explanations remain conversation memories, with no automatic trust gain. Missing mine records and disagreements are not trust violations. Unsolicited promise reminders share the existing personal follow-up allowance of at most one per game day.

## Saving and existing farms

Versioned promise records save with the farm when you sleep. Existing memory remains intact. An old active fish quest becomes an open-ended promise; an already completed fish delivery counts once. Restarting without saving returns to the last overnight save, as usual. Separate farms keep separate records. Disabling memory preserves its saved data.

## Solace test aid

`AbigailQuestTestFarm` defaults to empty and remains `Solace` only in Dave's local configuration. After explicitly accepting the fish favor and leaving conversation, that setting supplies one Sardine. A full backpack delays it. Reopening chat does not grant another; completion cancels any pending grant. Quartz and Iron Bars are not granted by this setting. Credentials and local settings are excluded from the release ZIP.

## Player checklist

- Talk to Abigail and accept a request with the row below chat; check its date in the normal journal.
- Leave and return with the item, then use the handover row. Check purple names and her response.
- Check F6 for her outlook and the history explaining it.
- Try Space to continue and Escape to leave, including after choosing an extension or delivery.
- Play several days to assess whether her tone and occasional reminders feel natural.

See testing.md for recorded build, automated, live-game and overnight results. The optional test harness is never included in the release package.

## Portrait reactions

AI conversation uses the game's loaded `Portraits/Abigail` texture. Dave's installed AbigailModern pack supplies the new portraits and everyday walking sprites. The mod never replaces that artwork with an embedded original sheet. Expression choices are restricted to neutral, happy, sad, angry, thoughtful, serious, surprised and warm, mapped to the corresponding authored cells. Missing or unknown expressions use neutral. Typing is neutral; waiting is thoughtful. Small UI viewports use a smaller portrait. No expression changes trust or native friendship.
