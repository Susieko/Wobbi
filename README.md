🌌 Wobbi

A tiny cosmic desktop companion with a surprisingly big personality. 💜

Current version: v0.5.0 — Agenda & Dota Companion (Release Candidate)
Built with: C# · .NET 10 · WPF / XAML · Ollama

Wobbi is an animated, AI-powered Windows desktop companion who floats around your screen, reacts to affection, falls asleep when things get quiet, and chats using a locally running language model. Beyond being a tiny desktop pet, Wobbi also has a personal agenda, reminders, and an experimental Dota 2 companion.

No paid AI subscription or cloud API key is required for local conversations.

👩‍💻 Looking at Wobbi as a C#/.NET portfolio project?
Read the [**Developer Case Study →**](Wobbi/README.md) for the architecture, engineering decisions, code walkthroughs, and implementation details.

🎬 See Wobbi in action

<img width="1530" height="396" alt="image" src="https://github.com/user-attachments/assets/39389275-7c61-4968-8992-2effdde6ae8b" />
<img width="1536" height="1024" alt="Wobbi’s Patting Cursor Showcase" src="https://github.com/user-attachments/assets/290e5cab-5c0e-441d-a219-6bae999c3aef" />

https://github.com/user-attachments/assets/451d0fe6-1634-4472-aa87-2f6ba91d56d3

Wobbi is an animated, AI-powered Windows desktop buddy built with C#, .NET 10, WPF and Ollama. He keeps you company on your desktop, reacts to interactions, and chats through a locally running language model.

No paid AI subscription or cloud API key is required.

✨ What can Wobbi do?

💜 An expressive desktop buddy

- A character that feels alive: floating, breathing, blinking, curious glances, sleepy expressions, and animated reactions.
- Petting and scritching: pat Wobbi's head or hold for a longer scritch, with custom cursor feedback.
- Full Mode & Mini Mode: switch between the full companion and a compact chibi version with a double-click.
- Personalities: Calm, Balanced, or Playful, affecting dialogue and reactions.
- Custom looks: Classic, Lavender, and Cosmic appearance options for the full character.
- Comfort settings: speech bubbles, reduced animations, and automatic snoozing when the computer is idle.

🤖 Private, locally powered AI conversations

- Chat through Ollama using the qwen3:4b-instruct model.
- Wobbi can react with a matching mood, such as happy, excited, or curious.
- Recent conversational context is kept during the current app session.
- Conversations can run locally once Ollama and the model have been downloaded.
- Close and reopen the chat window without losing the current session's conversation.

📅 Agenda & reminders — new in v0.5

- A weekly agenda with event creation, editing, and deletion.
- A calendar view with date navigation and event details.
- Reminders before appointments and at event start.
- Reminder notifications that remain pending until dismissed.
- Locally saved agenda events and a notification indicator integrated with Mini Mode.

🎮 Dota 2 companion — new in v0.5, still being verified

- Tracks match states through Dota 2 Game State Integration (GSI).
- Detects reported kills, deaths, and streak milestones.
- Reacts with personality-based commentary, with Quiet / Normal / Chatty frequency settings.
- Includes match-state tracking and a Dota connection status indicator.
- Runs as a separate desktop window, not a game overlay.

🧪 Release-candidate note: The Dota integration is implemented but live match-result, reconnect, and combined regression testing are still in progress. Dota 2 GSI must be configured separately. Post-match AI analysis is not part of v0.5.

🛠 Built with

Technology	                         What it does

C# / .NET 10	                       Core desktop application logic
WPF / XAML	                         Windows interface, interaction, and animation
SharpVectors.Wpf	                   SVG artwork rendering
Ollama                               Local AI runtime
Qwen3 4B Instruct	                   Locally running conversation model
HttpClient + System.Text.Json	       Local AI requests and structured responses
Dota2GSI	                           Optional game-state integration

Curious how these pieces fit together? Explore the [technical case study](Wobbi/README.md).

🚀 Run Wobbi from source

Wobbi is currently a source-build project, not an installer-based application.

Requirements

- Windows with support for .NET 10 desktop applications.
- .NET 10 SDK, or Visual Studio with the .NET desktop development workload.
- Ollama for Windows.
- Enough system resources to run a local language model.

1. Download the AI model
In PowerShell:
ollama pull qwen3:4b-instruct
Make sure Ollama is running locally.

2. Build and launch Wobbi
From the repository root:
dotnet restore Wobbi/Wobbi.csproj
dotnet run --project Wobbi/Wobbi.csproj

Or open Wobbi.slnx in Visual Studio and start the Wobbi project.
💡 Wobbi's character animations, agenda, and other non-AI features do not require an Ollama reply. Local AI conversations require Ollama and the downloaded model to be available.

🖱️ A few ways to interact

Action                                           Result

Drag Wobbi                                       Move him around the desktop
Double-click	                                   Switch between Full Mode and Mini Mode
Single-click in Mini Mode                        Open or focus the chat
Pat or hold on his head	                         Trigger petting or scritching animations
Right-click → Talk	                             Open the chat window
Right-click → Pet	                               Trigger a happy reaction
Right-click → Sleep / Wake	                     Control sleep mode
Right-click → Appearance / Personality	         Change Wobbi's look or behavior
Right-click → Preferences	                       Adjust speech bubbles and animation preferences
Right-click → Quit	                             Close Wobbi

🔒 Privacy & local data

Wobbi's AI chat sends requests to your local Ollama server at http://127.0.0.1:11434/. The default setup does not send prompts to a paid cloud AI provider.

- Chat context: stored in memory for the current session, not saved between app restarts.
- Preferences: saved locally at %LOCALAPPDATA%\Wobbi\appearance.json.
- Agenda events: saved locally at %LOCALAPPDATA%\Wobbi\agenda.json, with backup recovery logic.
- Dota companion: uses GSI event data; it does not read Dota process memory, inject code, modify the client, automate gameplay, or access hidden information.
- Initial setup: downloading Ollama and the model requires internet access; local AI inference does not require a cloud AI subscription.

🪐 Project status & roadmap

v0.5 is a release candidate and a learning/portfolio project, not a finished commercial assistant. The core companion, local AI chat, Mini Mode, agenda, and initial Dota GSI integration are implemented; some Dota edge cases and combined tests remain open.

Ideas for future releases include saved notes and memory, additional productivity features, and post-match Dota 2 analysis using external data and local AI.

See the [changelog](CHANGELOG.md) for version-by-version details and known limitations.

👩‍💻 For developers & recruiters

Wobbi is also a hands-on exploration of C#/.NET desktop development: WPF animation, event-driven UI behavior, local HTTP/JSON communication, settings persistence, scheduled reminders, and API-style integrations.

➡️ [Read Wobbi's C#/.NET Developer Case Study](Wobbi/README.md)

💜 About

Created by Susan Aben as a personal project combining software development, local AI, playful interaction design, and character animation.

Small blob. Big personality. ✨
