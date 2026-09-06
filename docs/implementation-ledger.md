# Solace Weather implementation

Approved scope: local single-player weather and forecasts, regional climate, existing gameplay integration, per-save opt-in, and an Egg Festival shelter pilot. PC 1.6.15 / SMAPI 4.5.2. No AI or new soil system.

- [x] Deterministic simulation and save-state tests.
- [x] Weather/gameplay integration and version-guarded patches.
- [x] Weather journal, HUD, and TV forecasts.
- [x] Event-local Egg Festival canopies and dialogue.
- [x] Release build and independent source review.
- [x] Final package and live-game verification record.

The implementation is a playable candidate. The remaining hands-on acceptance checks are tracked in `testing.md`; building successfully does not close those checks.

Rulings:
- Empty project folder is already isolated from existing source; work in place.
- Build output never auto-installs. Install the finished package explicitly after checks.
- Default per-save state is disabled, including existing saves. Enable Solace through the journal, taking effect the following morning.
- Internal climate units are Celsius; default presentation is Fahrenheit.
- Preserve engine-mandated special weather and festival weather outside the Egg Festival pilot.
- Rain integration uses the entire 06:00–26:00 simulated day when completing sleep, before crops perform their growth update.
