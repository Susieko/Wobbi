# 🌌 Wobbi

**A tiny cosmic desktop companion with a surprisingly big personality.**

**Current version: 0.4.0 — Mini Mode (early prototype)**

## 🎬 See Wobbi in action

https://github.com/user-attachments/assets/451d0fe6-1634-4472-aa87-2f6ba91d56d3

Wobbi is an animated, AI-powered Windows desktop buddy built with C#, .NET 10, WPF and Ollama. He keeps you company on your desktop, reacts to interactions, and chats through a locally running language model.

No paid AI subscription or cloud API key is required.

## ✨ Features

### New in 0.4 — Mini Mode

- **Full and Mini modes** — switch between the full character and a tiny, face-only desktop companion by double-clicking.
- **A home in the corner** — Wobbi launches at the bottom-right of the primary screen, and Mini Wobbi parks just above the taskbar. The window can still be dragged, and expanding restores the full-size character's previous position.
- **Remembered size** — Mini/Full preference persists across restarts.
- **Mini interactions** — single-click Mini Wobbi to open or focus chat; double-click to switch modes; drag to reposition.
- **Mini expressions** — blinking and occasional left/right glances, with personality-dependent glance frequency and reduced-motion support.
- **Unread reply badge** — a small notification dot appears in Mini Mode when a new AI answer arrives while the chat is not active. Opening chat clears the badge.
- **Hide instead of forgetting** — closing the chat with X hides the window; reopening it restores the conversation while Wobbi remains running.
- **Refreshed Lavender artwork** — updated character shading, highlights and expressions.

### Also included

- **Two full-size appearances** — Classic and Lavender Wobbi, with your selection saved.
- **Three AI personalities** — Calm, Balanced and Playful; switch during an open conversation.
- **Personality-aware reactions** — different happy bounces, excited hops and curious tilts.
- **Animated companion** — floating, breathing, blinking, dragging, and expressive faces.
- **Sleep and pet interactions** — sleepy eyes and Z's; smiling, bouncing and floating hearts.
- **Local AI chat** — Ollama with `qwen3:4b-instruct`, recent conversational context and AI-selected moods.
- **Thinking and reply bubbles** — animated `.` → `..` → `...` while Ollama responds, followed by a short reply preview.
- **Saved visual preferences** — show/hide speech bubbles and reduce animations.
- **Custom Windows app icon** — chibi Wobbi.

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
