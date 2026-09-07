# Phone validation — September 6, 2026

The approved blue phone layout was checked in the actual Stardew Valley 1.6.15 / SMAPI 4.5.2 renderer with NPC Modern 0.15 artwork. This branch has not been installed in the normal game. Automatic blocking is now user-approved and enabled by default. The completed-breakup persistence and validated reconciliation checks were added afterward; the historical live results below do not claim those new checks have run in game.

Final integration follow-up: Core suite now 188 passed. The phone portrait consumer compiles and passed code review; the shared provider wiring and actual generated-avatar capture belong to the combined root integration and remain unverified in this isolated branch.

| Check | Result |
| --- | --- |
| Release Core suite | 177 passed, 0 failed |
| Release mod and game harness builds | Passed, no warnings or errors |
| In-game phone integration | 23/23 passed |
| In-game contact/block integration | 18/18 passed |
| Native number-exchange accept and decline clicks | Both continued normal conversation; only acceptance unlocked the contact |
| Fresh native farm | Empty contacts, messages, shortcuts and player memories |
| Native save and full process restart | Same messages, shortcuts and shared memory; accepted contacts and block retained |
| Paid Gemini requests | Three successful calls: outgoing text, same outgoing shortcut again, NPC initiative |
| Actual send-button click | Passed |
| Windows default microphone | Opened and stopped successfully; spoken transcription accuracy not tested |
| Existing user saves | All eleven original files retained identical SHA-256 hashes |

The repeated shortcut made a new provider request and received a new reply reflecting the earlier conversation. The unsolicited incoming text did not create a fabricated farmer message or preference. Failed/retried sends retained one outgoing record. A simulated relationship block cancelled a pending reply, rejected quick sends and retries, and retained history; clearing the tested relationship restriction restored texting.

Screenshots are actual game captures: [live Gemini chat](images/phone-live-gemini.png), [compact UI](images/phone-compact.png), [contact dropdown](images/phone-contact-dropdown.png), [blocked contact](images/phone-blocked.png), and [native number exchange](images/phone-number-exchange.png). The original staged layout preview was user-approved; the live chat image separately demonstrates real provider output.

Detailed local evidence is in `C:/Users/david/SDV/artifacts/new-game-persistence/phone-20260906` and the adjacent `phone-20260906-control` folder. The profiles contain only disposable SvaAudit farms and separate mod copies. Test processes were stopped after validation. No test harness or test save belongs in the release package.

Remaining manual checks: dictate a spoken sentence on the player's own microphone and review the draft; try the phone during ordinary play at the player's preferred UI scale. The feature currently supports the twelve relationship characters and single-player saves. It uses the existing configured Gemini credentials and model.

## Automatic blocking follow-up

Focused source tests cover completed breakup/divorce across JSON reload, blocked sends/retries/initiative/incoming/stale replies with history retained, failed versus validated renewed dating, separation repair timing, native divorce precedence, old saves without the marker, and rejection of invalid marker data. The game harness also checks the enabled default, explicit opt-out, completed breakup remaining blocked with native friendship, and restoration through validated dating. The combined disposable native run passed all 24 exchange/blocking checks and 23 phone checks. Completed breakup blocking and validated reconciliation were exercised without provider calls. Normal SolaceWeather 0.3 installation was verified separately; the shared player portrait is shown in images/player-portrait-phone.png.
