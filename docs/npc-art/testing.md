# NPC artwork verification

Build the production mod and NpcArtAudit, then run scripts/package-npc-modern.ps1. The package script checks every ZIP file against the source. scripts/start-npc-art-audit.ps1 creates a unique workspace mod folder, verifies the extracted package against current source, backs up existing logs, and starts SMAPI with its supported --mods-path argument. It records the owned PID and start time in session.json. This allows title-screen artwork checks while another game session is running.

Inspect that session's NpcArtAudit JSON results and game-rendered PNGs, plus its fresh registered-asset verification log. SMAPI selects a player-numbered log when another session holds the normal log open. Check the launch time and mod path before treating any log as evidence. Save the log in the session folder before another run. Stop only the recorded owned process after confirming its start time; never stop the existing user session.

The isolated test proves that the packaged artwork loads and that the exercised native selectors, portrait indices and animation paths work. It does not prove installation into the user's Mods folder, farm/event playback, scene placement, or compatibility with every active mod. Store isolated results separately from evidence/registered-loader-check.txt. The normal coverage ledger compares source assets with the actual installed files.

Once Stardew Valley closes, scripts/install-bachelorettes-modern.ps1 backs up the installed mod, installs the current source and verifies hashes. A fresh ordinary launch is still required for installed loader verification. Never bypass its running-game guard to modify the active game's mod files.
