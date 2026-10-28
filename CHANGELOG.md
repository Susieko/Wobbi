# Changelog

All notable project changes are tracked here. Wobbi remains an experimental desktop companion.

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
