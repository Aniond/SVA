# Abigail memory pilot

Approved scope: preserve Abigail's established personality and existing friendship rules; add per-farm memory and context before connecting generated conversation.

Implementation plan:
1. Test a versioned, bounded memory ledger for daily conversations and gifts, duplicate prevention, restoration and farm isolation.
2. Observe native friendship records without editing them. Save through SMAPI, reset at title, and refuse unsupported state versions without overwriting them.
3. Load a separate personality asset. Build context from current relationship status, location, game time and recorded experiences, including displayed ordinary Abigail dialogue pages. Do not import unseen heart events or assume the farmer's remote actions are known. Weather context will be connected with the conversation consumer.
4. Provide F6 to inspect the memory foundation. Build and run automated tests; record in-game verification still pending.

The profile is authored guidance, not replacement canon. It emphasizes curiosity, independence, playful competitiveness, music and adventure, with room for uncertainty and vulnerability. Do not treat speculation about parentage as fact, turn gift jokes into a defining trait, or assume romance from high hearts alone. Current dialogue selected by the game is stronger evidence than the profile.

Memory starts when installed, including the loaded day's existing interaction flags. Conversation flags prove a daily interaction, not its words. Separately retain up to eight distinct displayed ordinary dialogue pages per day, each bounded to 2000 characters. Gift counters prove a gift, not its identity or reception. Store only those supported facts. No invented promises or retroactive memories. Keep at most 112 daily records. Repeated daily observations update the same record. Normal saving persists; quitting without saving rolls back memories with the day.

No provider calls, secret access, friendship changes, dialogue replacement or automatic town gossip in this foundation.

Character reference: https://stardewvalleywiki.com/Abigail ; runtime dialogue and friendship are read from the installed game.
