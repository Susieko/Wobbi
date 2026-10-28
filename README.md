# 🌌 Wobbi

**A tiny cosmic desktop companion with a surprisingly big personality.**

**Current version: 0.3.0 — Calm & Cute (early prototype)**

## 🎬 See Wobbi in action

https://github.com/user-attachments/assets/451d0fe6-1634-4472-aa87-2f6ba91d56d3

Wobbi is an animated, AI-powered Windows desktop buddy built with C#, .NET 10, WPF and Ollama. He floats on your screen, reacts to interactions, falls asleep, and chats with you through a locally running language model.

No paid AI subscription or cloud API key is required.

## ✨ Features

- **Two appearances** — switch between Classic and Lavender Wobbi, with your choice saved across restarts.
- **Three personality presets** — Calm, Balanced and Playful; switch live without clearing the open conversation.
- **Personality-aware reactions** — happy bounces, excited hops and curious tilts respond to Wobbi's selected personality and AI mood.
- **Animated desktop companion** — floating, breathing, blinking and draggable character.
- **Sleep mode** — sleepy expression, slower breathing and drifting Z's.
- **Pet interactions** — smiles, happy bounces and floating hearts.
- **Local AI chat** — Ollama + `qwen3:4b-instruct`, with short-term conversational context and AI-selected emotions.
- **Thinking and reply bubbles** — animated typing dots (`.` → `..` → `...`) above Wobbi while Ollama responds, followed by a short reply preview.
- **Saved preferences** — turn speech bubbles on/off or reduce animations; appearance and personality choices are saved too.
- **Custom application icon** — a chibi Wobbi icon for Windows.

> **Project status:** Wobbi is an experimental learning and portfolio project, not a finished desktop assistant or installer-based product. The v0.3.0 GitHub release contains source code; build Wobbi locally to run it.

## 🛠 Tech stack

| Technology | Role |
| --- | --- |
| C# / .NET 10 | Application logic |
| WPF / XAML | Windows UI and animation |
| SharpVectors.Wpf | SVG character art |
| Ollama | Local AI runtime |
| Qwen3 4B Instruct | Local language model (`qwen3:4b-instruct`) |
| HttpClient / JSON | AI requests, structured replies and mood selection |

## 🚀 Getting started

### Prerequisites

- Windows with support for .NET 10 desktop apps
- .NET 10 SDK and Visual Studio with the **.NET desktop development** workload (if building from Visual Studio)
- [Ollama](https://ollama.com/download/windows)
- Enough free RAM and processing capacity to run the selected local model

### 1. Install the model

Open PowerShell:

```powershell
ollama pull qwen3:4b-instruct
```

Ensure Ollama is running.

### 2. Build and launch

From the repository root:

```powershell
dotnet restore Wobbi/Wobbi.csproj
dotnet run --project Wobbi/Wobbi.csproj
```

Or open `Wobbi.slnx` in Visual Studio and run the Wobbi project.

## 💜 Using Wobbi

- **Drag Wobbi:** move him around the desktop.
- **Double-click:** put him to sleep or wake him up.
- **Right-click → Pet Wobbi:** play his happy animation.
- **Right-click → Talk to Wobbi:** open the AI chat.
- **Right-click → Appearance:** choose Classic or Lavender.
- **Right-click → Personality:** choose Calm, Balanced or Playful.
- **Right-click → Preferences:** configure speech bubbles and reduced animations.
- **Right-click → Quit:** close Wobbi.

The exact wording of context-menu items may vary slightly between builds.

## 🔒 Privacy and data

Wobbi sends chat requests to **your local Ollama instance** at `http://127.0.0.1:11434/`. No paid cloud AI provider or API key is needed for this setup.

- Recent chat context is held **in memory** and is not intended as permanent AI memory.
- Appearance, personality and visual preferences are saved locally in `%LOCALAPPDATA%\Wobbi\appearance.json`.
- Downloading Ollama or the model requires internet access initially; subsequent model inference runs locally.

## 📋 Release notes

See [CHANGELOG.md](CHANGELOG.md) for changes in v0.3.0.

## 🪐 What's next?

The planned **v0.4 Mini Mode** focuses on a tiny desktop version of Wobbi, with a compact face, notification badge and expandable interaction. Later ideas include reminders, local memory and weather.

## 💜 About

Created by Susan Aben as a creative exploration of C#, WPF, local AI integration, interaction design and character animation.

*Small blob. Big personality.* ✨
