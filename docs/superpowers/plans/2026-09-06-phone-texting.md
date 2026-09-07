# Alive phone texting implementation plan

**Goal:** Implement the approved translucent blue contact/inbox/chat phone, with fresh Gemini replies to typed, dictated, or reused outgoing texts.

**Architecture:** A bounded Core phone state owns messages, shortcuts, retry identity and proactive limits. A game service owns save lifecycle and async provider work using RomanceService context/memory. A custom menu owns layout, input and local Windows speech recognition. No art changes.

**Approved spec:** Coordinator task 01a07829-8280-7330-8b55-69b3937ce7a6 and approved generated image exec-3e402836-922f-4787-9ce1-a7063f71f8cb.png.

- [x] Write and run failing state tests for validation, shortcut reuse, retries, interrupted requests, retention, save ownership and proactive limits; implement state.
- [x] Integrate farm lifecycle and existing Gemini/relationship context. Cancel stale results on transitions. Store successful texts in shared Living Memory. Proactive texts cannot invent player messages or create commitments.
- [x] Implement responsive blue phone, contacts/inbox, scrolling bubbles, saved-message chips, draft, pending/error/retry and real microphone capture with review before send.
- [x] Build and run focused state/provider checks. Coordinate isolated game harness with Assets; capture actual menu at normal and compact sizes. Preserve installation/configuration and user farms.
- [x] Document controls, supported characters, bounded behavior and verified versus manual limitations. Report reviewable artifacts to coordinator. No push.

## Added number exchange and contact controls

- [x] Native in-person accept/decline offer, with bounded later offers for existing acquaintances; only acceptance unlocks a number.
- [x] Named contact dropdown with keyboard/scroll navigation and preserved drafts.
- [x] Block guard across sends, quick messages, retries, proactive texts and pending replies; preserve history and show disabled controls.
- [x] Verify native question button path, farm isolation and save/restart persistence.
- [ ] Finalize automatic block triggers after the user clarifies what unfriend means. Current separation/pending transition/native divorce mapping is provisional and not installed.

See [validation evidence](../../phone-validation.md).
