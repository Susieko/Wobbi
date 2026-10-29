# Changelog

All notable project changes are tracked here. Wobbi remains an experimental Windows desktop companion.

## [0.4.0] — 2026-10-29 — Mini Mode

### Added
- **Mini Mode:** a compact, approximately 76 × 72 desktop companion with a small chibi face.
- **Double-click mode switching:** switch between Full and Mini Mode; Full Wobbi returns to his previous position when expanded.
- **Bottom-right positioning:** Wobbi starts near the bottom-right of the usable desktop area, and Mini Mode parks there automatically; Wobbi remains draggable.
- **Saved size preference:** Mini/Full Mode selection is remembered between application sessions.
- **Single-click Mini chat:** click Mini Wobbi to open or focus the existing chat without interfering with double-click or dragging.
- **Unread reply badge:** a small notification dot appears in Mini Mode when an AI reply arrives while the chat is minimized or otherwise not active, and clears when chat is opened.
- **Mini idle expressions:** occasional blinking and left/right curious glances, returning to the neutral face. Glance frequency depends on the selected personality and respects Reduced Animations.
- **Sleep / Wake context-menu action:** sleeping is now available by right-clicking Wobbi, instead of using double-click.

### Changed
- **Lavender visual refresh:** refined the full-size Lavender character artwork while retaining Classic Wobbi and the existing animation system.
- **Chat-window behavior:** clicking X hides rather than destroys the chat window. Reopening it preserves the visible messages and short-term AI context for the current app session.
- **Mini Mode artwork:** uses a separate, sharper chibi face image and expression frames.
- Removed the temporary notification test menu item after connecting the badge to real AI replies.

### Verified
- Mini Mode positioning, double-click switching, saved mode preference and Mini blinking/glancing.
- Single-click chat, in-session conversation retention, real reply notifications and clearing the badge.
- Personality switching; sleeping/waking; pet interactions; Reduced Animations; Classic and Lavender appearances; saved preferences.
- Successful Windows Release build and all **14 functional QA checks** (user-verified).

### Known limitations
- Mini notifications currently indicate unread AI chat replies; proactive notifications and reminders are not yet implemented.
- Chat history is retained when hiding/reopening the chat *during the same app session*, but is not saved across Wobbi restarts.
- Full and Mini window positioning currently targets the primary monitor's work area; multi-monitor placement persistence is not implemented.
- No Windows installer is included in this source-code release.
- Ollama and `qwen3:4b-instruct` must be installed and running locally for AI conversations.

## [0.3.0] — 2026-10-28 — Calm & Cute

### Added
- Classic and Lavender appearance switching, with the selected skin saved between sessions.
- Calm, Balanced and Playful AI personality presets that can be changed while a chat is open.
- Personality-dependent AI mood reactions with different bounce and tilt intensities.
- Chibi Wobbi Windows application icon.
- Animated thinking speech bubble above Wobbi, cycling from `.` to `..` to `...` until Ollama responds.
- Persistent **Show speech bubbles** preference.
- Persistent **Reduced animations** preference.

### Changed
- Refined AI conversation style to distinguish the three personalities without making replies excessively animated.
- Polished welcome, thinking and technical error messages.
- Updated reaction handling to preserve Wobbi's expressions and manual pet interactions.

### Verified
- Appearance and personality switching; personality-dependent animations.
- Thinking bubbles, speech bubble controls, reduced animations and persistence.
- Sleeping, waking, petting, hearts and error handling.
- Successful Release build (user-verified on Windows).

### Known limitations
- Early prototype; no Windows installer included in the source-code release.
- Ollama and `qwen3:4b-instruct` must be installed and running locally for AI chat.
- Conversation history is short-term and not preserved between application sessions.

## [0.2.0] — Early prototype

- Animated WPF desktop character: floating, breathing, blinking and draggable window.
- Sleeping/waking interactions and drifting Z particles.
- Petting, smiles, bounces and heart particles.
- Local Ollama AI conversations with structured replies and AI-selected moods.
- Chat window and speech-bubble reply previews.
