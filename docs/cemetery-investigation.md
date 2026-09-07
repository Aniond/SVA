# Abigail's cemetery investigation

An in-person AI conversation can propose this supported outing when Abigail has a checked free night within seven days. The player must explicitly accept the dated invitation. Abigail meets the player at the Town cemetery at 8 pm, with arrival allowed through 8:30 pm. Talking to her there starts the investigation immediately; ordinary conversations elsewhere remain ordinary conversations.

Three explicit choices examine a headstone, wait beside a pale light, and leave a harmless ghost in peace. Each advances ten game minutes. The final choice requires the ghost to have been drawn for at least 1.5 seconds. This is a visual apparition, not a spawned monster: there is no combat, damage, loot, money, or automatic romance change. Only completing all three steps records the shared experience in Abigail's Living Memory.

The journal tracks the meeting. Declining allows another offer after three days. Cancellation, bad weather, interruptions, or a missed meeting allow another night without punishment. Accepted cemetery plans and regular shared activities cannot overlap. Player movement and Abigail's schedule ownership are restored on exit. Save data belongs to the current farmer; an interrupted active scene reloads as missed, never completed. Phone replies cannot propose or accept the outing.

## Validation

- Release Core suite: 188 passed, including consent, grace period through 9 pm completion, ordered observed steps, decline cooldown, farmer identity, and provider template restrictions.
- Actual Stardew Valley/SMAPI disposable farm: eight runtime checks passed for absent/declined/early invitations, reciprocal booking reservation, latest-grace native dialogue start, cancellation movement, and NPC schedule restoration.
- One live Gemini request proposed the verified Spring 5, 8 pm meeting, without assuming acceptance. The full three-step scene completed and restored player movement. The open path meeting tile is discovered from the native grave action, rather than relying on an invented map coordinate.
- Screenshot: [actual ghost scene](images/cemetery-ghost.png), captured from the game renderer with NPC Modern 0.15. Combined 0.17 artwork is tested by the integration owner.
- Native overnight Saving/Saved and full process restart: identical completed outing state, exactly one retained shared experience, and player movement available. All eleven original user save files retained identical SHA-256 hashes.

The game harness uses only the guarded disposable SvaAudit phone profile. Its staging commands move the audit date and characters to exercise the scene; these are test fixtures, not production behavior. Do not install the harness in normal play.
