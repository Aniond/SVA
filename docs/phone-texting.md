# Alive phone texting

Press **F10** during ordinary single-player play. First, talk to a villager in person and accept their offer to exchange numbers. Accepting unlocks that contact; declining does not, and they can offer again after three days. Existing acquaintances can offer on your next conversation too. The supported contacts are the twelve existing relationship characters. Texting uses their existing personality, relationship boundaries, game context and Living Memory; it does not award hearts, accept quests or start dates.

Select a contact in the inbox, or click the NPC name at the top of a chat to open the contact dropdown. The dropdown shows only exchanged numbers, supports scrolling and arrow keys/Enter, and preserves each contact's draft while you switch.

Type up to 500 characters and press Enter or the arrow button. Outgoing messages become **Quick messages**: clicking a chip sends those words again and requests a fresh Gemini response. Hover a shortened chip to read the full text; Next cycles through your twelve most recent unique shortcuts. These shortcuts contain your words, never cached NPC replies.

The microphone uses local Windows speech recognition and the default microphone. Click it to start or stop, review the resulting draft, then send. It stops after 30 seconds or when leaving the chat. Windows needs an installed speech language, working microphone and microphone access. An unavailable device shows a message; typed input remains available. The mod does not save audio.

Scroll or use Page Up/Page Down to read older texts. Contacts show unread messages. Pending replies can finish after the phone closes, with a notification when ready. A failed text offers **Retry**, which reuses its delivery record, or **Dismiss**, which removes the unsent bubble but retains its quick message.

The phone can display the shared player portrait beside outgoing messages. It only reads the existing portrait; it does not generate another image. Combined-build wiring is `Relationships.PhoneMenu.PlayerPortraits = playerPortraits.TryGetPortrait` alongside the existing conversation portrait assignment. If no portrait is available, the existing chat layout remains usable.

NPCs can initiate texts between 10am and 6pm when the player is free. The default allows at most one attempt per game day across the phone, with at least three days between attempts from the same NPC. A contact needs an exchanged number, two hearts, no block, no unread texts and no unresolved outgoing text. Failures do not trigger automatic retries. `EnablePhoneInitiative` disables these spontaneous texts; `EnablePhone` disables the phone; `PhoneKey` changes F10.

**Automatic relationship blocking is enabled by default** (`EnableAutomaticPhoneBlocking=true`). Authored separation, breakup or divorce blocks texting; completing the transition to friendship does not remove the block. A saved relationship marker clears only after successful repair or validated renewed dating. Native divorced status always blocks while present. The chat retains its history, but sending, quick messages, retries, dictation and NPC-initiated texts are disabled, and pending replies are cancelled. Ordinary disagreements, heart decreases and Gemini dialogue never create a block. Set the option to false to opt out. Older relationship saves without the new marker remain readable; past completed breakups cannot be reconstructed reliably and are not invented.

Phone history and shortcuts are stored inside each farm's SMAPI save data. A successful native save preserves them across reload/restart; a new farm begins empty. Quitting before saving loses changes since the last save, as with other farm progress. Each contact retains 80 messages and twelve shortcuts; recent phone history and the shared bounded Living Memory provide context for future replies. Invalid phone save data is preserved and disables the phone for that session instead of overwriting it.

The current feature is single-player, following the existing relationship system. Dictation currently supports Windows; other platforms can type. The same local Gemini credentials and model setting as in-person AI conversation are required. Provider failures stay visible and are not replaced with a pretend AI response.

## Validation

Core tests: `dotnet test tests/SolaceWeather.Core.Tests`. Phone tests cover farm identity, invalid input, duplicate clicks, retry identity, interrupted delivery, retention, proactive limits and fresh provider calls for reused text. Build: `dotnet build src/SolaceWeather`.

The separate game harness has `phonechecks`, `phoneai`, `phoneshortcut`, `phoneinitiative`, `phonecapture`, `phonecompact`, `phonestate` and `phoneclose` requests. They require the explicitly isolated `SvaAudit` phone profile. Do not ship test harnesses or generated test saves. Live outcomes and screenshots are recorded separately after running these commands.

Detailed results: [phone validation](phone-validation.md).
