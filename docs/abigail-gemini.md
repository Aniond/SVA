# Abigail Gemini pilot — 0.1.16

Clicking Abigail automatically opens text entry for ordinary dialogue. Type up to 500 characters and press Enter or the checkmark. A successful response is displayed as Abigail's spoken text; Space opens another reply. Escape exits completely, including while typing or waiting. Story events, questions and dialogue with game actions remain native. Restored fallback dialogue is not automatically reopened. The original game-selected line is supplied as a tone/topic cue, not recorded as something already spoken.

The text-entry screen has three clickable authored starter questions. A click sends that exact question through the same pipeline as typed input. The personality and generation instructions now allow 4–6 developed sentences, roughly 80–140 words, for open-ended topics. Output validation remains bounded to 1200 characters. Suggestions do not cause background API calls.

The mod sends her authored profile, farmer name, current friendship status, time, location, local rain, recent observed dialogue and completed AI exchanges to Google's Gemini API. The default model is gemini-3.8-flash, verified with a live generation request. Keys come from the user's GEMINI_API_KEY environment variable, then the running process's GEMINI_API_KEY or GOOGLE_API_KEY. Credentials are never packaged, saved with a farm or logged. No MCP server is required in this pilot.

Only explicit submitted messages issue requests. One request is allowed at a time, with a 25-second timeout and no automatic retry. Closing the menu, changing saves or beginning an event discards pending results. Failures restore ordinary dialogue and show a short message. Network work does not access game objects. Reply text is sanitized before display and has no path to grant items, hearts or quest completion.

The most recent 32 completed exchanges persist with the farm. They are labeled conversation, not verified game events. Failed and cancelled requests are not recorded. Context includes the most recent 14 daily observation records; the underlying ledger retains 112. The older memory save format loads with an empty exchange list.

Settings: EnableAbigailAi, AbigailTalkKey, GeminiModel. Disabling AI leaves ordinary conversations intact. Memory must be enabled for AI chat. Generated dialogue is an interpretation; personality quality, recall over many days and spoiler resistance require continued playtesting.

Implementation: independent HTTP client and response parsing in Core; menu lifecycle and cancellation in AbigailConversation; persistent completed exchanges in AbigailRelationship. Automated provider tests cover completed responses, thought exclusion, control-character removal, HTTP errors, missing candidates and incomplete replies. Live tests must verify the entry screen, successful display, memory addition and unchanged friendship. Save/reload, physical Space, UI scaling and offline fallback in-game remain on the manual checklist.

API reference: https://ai.google.dev/api/generate-content


