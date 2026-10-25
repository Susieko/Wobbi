# 🌌 Wobbi

**A tiny cosmic desktop companion with a surprisingly big personality.**

Wobbi is an animated, AI-powered Windows desktop buddy built with C# and .NET. He floats around your screen, reacts to interactions, falls asleep, and chats with you using a locally running language model.

No paid API subscription required.

## ✨ Features

- **Animated companion** — floating, breathing, blinking, and dragging
- **Sleep mode** — sleepy expressions, deeper breathing, and floating Z's
- **Pet interactions** — happy bouncing, smiling, and floating hearts
- **Local AI conversations** — powered by Ollama and Qwen3
- **AI-driven emotions** — happy, excited, curious, and neutral reactions
- **Conversation history** — scrollable chat with recent conversational context
- **Speech bubbles** — Wobbi displays little responses beside his character
- **Offline-capable AI** — conversations work locally after the model has been downloaded

## 🛠 Tech stack

| Technology | Purpose |
|---|---|
| C# / .NET 10 | Application logic |
| WPF / XAML | Windows desktop interface |
| SharpVectors | SVG rendering |
| Ollama | Local AI runtime |
| Qwen3 4B Instruct | Language model |
| HttpClient / JSON | Communication with the local AI |

## 🚀 Getting started

### Requirements

- Windows 10 or 11
- .NET 10 SDK for building from source
- Ollama installed
- Sufficient memory and processing power to run the local model

### 1. Install Ollama

Download Ollama from https://ollama.com/download/windows.

### 2. Download Wobbi's AI model

Open PowerShell and run:

```powershell
ollama pull qwen3:4b-instruct
```

Make sure Ollama is running.

### 3. Build and launch Wobbi

Open the solution in Visual Studio with the .NET desktop development workload installed.

Alternatively, from the repository root:

```powershell
dotnet restore Wobbi/Wobbi.csproj
dotnet run --project Wobbi/Wobbi.csproj
```

Wobbi will appear on your desktop!

## 💜 Interacting with Wobbi

- **Drag:** Move Wobbi around your desktop
- **Double-click:** Put Wobbi to sleep or wake him up
- **Right-click → Pet:** Give Wobbi some affection
- **Right-click → Talk:** Open the AI chat interface
- **Right-click → Quit:** Close the application

## 🔒 Privacy

Wobbi's current AI implementation connects to Ollama at `127.0.0.1:11434`.

Conversations are processed locally when using the downloaded model. Wobbi does not require an OpenAI API key or a paid cloud AI account.

Conversation context is currently held in application memory rather than stored permanently.

## 🌱 Development status

**Version 0.2.0 — Early prototype**

Wobbi is an ongoing learning and portfolio project.

Planned improvements include:

- More expressive animations and personality states
- Persistent memory and preferences
- Productivity features and reminders
- Better desktop integration
- A packaged Windows application

## 💜 About

Created by Susan Aben as a creative exploration of C#, WPF, local AI integration, desktop interaction, and character animation.

*Small blob. Big personality.* ✨