# Public town chatter

Two supported villagers standing close together in Town can exchange short overhead speech bubbles while the farmer walks nearby. The first version uses the twelve existing personality profiles, with actual testing focused on Abigail and Sam. It does not change schedules, stop movement or play voiced audio.

Each exchange contains two short lines, one per villager, shown sequentially for four to six seconds each. Compact wrapped text uses an integer-scale pixel font with a clean outline, subtle translucent rounded background and speaker-pointing tail. The NPCs must be visible, idle, within three tiles of each other and within six tiles of the farmer. A menu, event, active phone/conversation request, sleeping or occupied speaker prevents an exchange. Leaving, a speaker moving away, changing farms/days or changing weather cancels it. Native speech bubbles are respected.

Gemini receives only the two authored personalities and a separate public snapshot: current Town weather, actual calendar festivals in the next seven days, and confirmed Community Center/Joja state. It receives no farmer messages, personal details or private relationship memories. The provider must return the selected supplied fact identifier and exactly two valid speaker lines. Failure skips the exchange quietly, without retry or a fabricated replacement.

The limit is three generation attempts per game day, at least two game hours and sixty real seconds between attempts, and each pair at most once per day. There is one pending exchange at a time. `EnableTownChatter` disables the feature; existing AI and memory settings also apply. Existing Gemini credentials and model settings are reused.

Only fully witnessed exchanges are remembered: both lines must finish while the farmer remains nearby. Each farm retains the most recent twenty-four exchanges as attributed public speech. Later in-person and phone context can include the last four involving that NPC. These words never prove additional events or imply that the farmer participated. This adds no new private memory system for other NPCs; Abigail retains her richer existing promise/activity integration.

History and attempt limits save with ordinary native farm saves. A fresh farm begins empty. Invalid stored data is preserved and disables chatter for that session. The feature follows the existing single-player scope.

## Validation

Core suite: 180 passed. Release mod and test harness builds passed. Fourteen actual game checks passed, covering visibility, privacy of public context, the native festival seven-day boundary, sequential speakers, delayed memory recording, Abigail's later context, walking-away cancellation, stale-day and stale-weather rejection, and state validation.

Two bounded paid Gemini calls succeeded with Abigail and Sam in actual rainy Town. Cosmetic iterations reused the second reply without additional calls. Native Saving/Saved events and a full process restart preserved identical chatter state and its attribution in Abigail's conversation context. The disposable save harness explicitly established native bed state; invoking Sleep_Yes alone after teleporting did not complete the earlier save attempts. Those incomplete attempts were not counted as successful persistence checks.

The separate control farm started with no heard chatter, attempt history or earlier dialogue in its conversation context. All eleven original user-save files retained identical SHA-256 hashes. Both test processes were stopped after their checks.

Final actual-game readability captures: [Abigail speaks](images/town-chatter-abigail-v3.png), then [Sam replies](images/town-chatter-sam-v3.png). These replay the validated public dialogue for visual inspection without another provider call. The first native one-line strip was too wide; the first custom font was too small. The final captures use a two-times integer pixel font and a stronger clean outline over rain and cobblestones, with a translucent background and no solid blue panel.

Live evidence is stored in `C:/Users/david/SDV/artifacts/new-game-persistence/phone-20260906` and the adjacent control folder. The test harness is not part of the release. Normal installation remains unchanged pending coordinator integration. Wider ordinary-play testing of natural NPC proximity and individual UI/zoom preferences remains useful; actual validation here focused on Abigail and Sam.
