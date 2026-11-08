# 🌌 Wobbi

**A tiny cosmic desktop companion with a surprisingly big personality.**

**Current version: 0.5.0 — Agenda & Dota Companion (Release Candidate)**

## 🎬 See Wobbi in action

<img width="1530" height="396" alt="image" src="https://github.com/user-attachments/assets/39389275-7c61-4968-8992-2effdde6ae8b" />
<img width="1536" height="1024" alt="Wobbi’s Patting Cursor Showcase" src="https://github.com/user-attachments/assets/290e5cab-5c0e-441d-a219-6bae999c3aef" />

https://github.com/user-attachments/assets/451d0fe6-1634-4472-aa87-2f6ba91d56d3

Wobbi is an animated, AI-powered Windows desktop buddy built with C#, .NET 10, WPF and Ollama. He keeps you company on your desktop, reacts to interactions, and chats through a locally running language model.

No paid AI subscription or cloud API key is required.

## ✨ Features


### New in 0.5 — Agenda & Dota Companion 💜

#### 📅 Personal Agenda
- Weekly calendar with event creation, editing and deletion.
- Custom month navigation and event details.
- Reminders before appointments and at event start.
- Persistent reminder notifications.
- Integration with Mini Mode's notification badge.

#### 🎮 Dota 2 Companion
- Live match tracking through official Dota 2 GSI.
- Kill and death reactions.
- Special reactions for kill and death streaks.
- Victory and defeat commentary.
- Personality-specific dialogue.
- Quiet, Normal and Chatty commentary settings.
- Dota connection status indicator.
- Match ID tracking and reconnect safeguards.
- Debug-only reaction previews.

**Note:** GSI requires separate configuration in Dota 2.
Live match-result and reconnect edge cases are still
undergoing final verification.

All interactions work locally without an AI request.


> **Project status:** Wobbi is an experimental learning and portfolio project, not a finished desktop assistant or installer-based product. Build the source locally to run it.

## 🛠 Tech stack

| Technology | Role |
| --- | --- |
| C# / .NET 10 | Application logic |
| WPF / XAML | Windows UI and animation |
| SharpVectors.Wpf | SVG character art |
| Ollama | Local AI runtime |
| Qwen3 4B Instruct | Local language model (`qwen3:4b-instruct`) |
| HttpClient / JSON | AI requests and structured mood replies |

## 🚀 Getting started

### Prerequisites

- Windows with support for .NET 10 desktop apps
- .NET 10 SDK and, optionally, Visual Studio with the **.NET desktop development** workload
- [Ollama](https://ollama.com/download/windows)
- Enough memory and processing capacity to run the selected local AI model

### 1. Install the model

Open PowerShell:

```powershell
ollama pull qwen3:4b-instruct
```

Make sure Ollama is running.

### 2. Build and launch

From the repository root:

```powershell
dotnet restore Wobbi/Wobbi.csproj
dotnet run --project Wobbi/Wobbi.csproj
```

Or open `Wobbi.slnx` in Visual Studio and run the Wobbi project.

## 💜 Using Wobbi

- **Double-click:** switch between Full and Mini Mode.
- **Single-click in Mini Mode:** open or focus the AI chat (after a short delay to distinguish double-clicks).
- **Drag:** reposition Wobbi on the desktop.
- **Right-click → Talk:** open the AI chat.
- **Right-click → Pet:** trigger his happy reaction.
- **Right-click → Sleep / Wake:** toggle sleep mode.
- **Right-click → Appearance:** choose Classic or Lavender for Full Wobbi.
- **Right-click → Personality:** choose Calm, Balanced or Playful.
- **Right-click → Preferences:** toggle speech bubbles and reduced animations.
- **Right-click → Quit:** exit the application.
- **Chat window X:** hide chat without clearing the current conversation; reopening restores it while Wobbi is running.
- **Hover and pat his forehead:** click for a happy pat, or hold for head scritchies.

The Mini character currently uses its own lavender chibi artwork, independently of the selected full-size appearance.

## 🔒 Privacy and data

Wobbi sends chat requests to **your local Ollama instance** at `http://127.0.0.1:11434/`. No paid cloud AI provider or API key is required for this setup.

- Recent chat context is held **in memory**, including while the chat window is hidden. It is **not preserved between application restarts**.
- Appearance, personality, Mini Mode and visual preferences are stored locally in `%LOCALAPPDATA%\Wobbi\appearance.json`.
- The notification dot indicates an unread AI reply; it is not yet a general reminder or alert system.
- Downloading Ollama and the model requires internet access initially; subsequent model inference runs locally.

## 📋 Release notes

See [CHANGELOG.md](CHANGELOG.md) for version history and the v0.4.0 changes.

## 🪐 What's next?

Planned next steps include reminders and agenda integration, optional persistent memory, local notes and everyday productivity features. These are roadmap ideas, not currently shipped features.

## 💜 About

Created by Susan Aben as a creative exploration of C#, WPF, local AI integration, interaction design and character animation.

*Small blob. Big personality.* ✨
