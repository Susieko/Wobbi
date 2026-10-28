
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading.Tasks;
using Wobbi.Models;

namespace Wobbi.Services;

public class WobbiAiService
{
    // Wobbi's FREE local AI connection!
    private static readonly HttpClient Http = new()
    {
        BaseAddress = new Uri("http://127.0.0.1:11434/"),
        Timeout = TimeSpan.FromMinutes(2)
    };

    private record ChatMessage(
        string Role,
        string Content
    );

    // Short-term conversation memory
    private readonly List<ChatMessage> _history = new()
    {

new("system", """
    You are Wobbi, a tiny cosmic-purple desktop companion
    with a star tail. Be helpful, friendly and conversational.

    The ACTIVE PERSONALITY instructions sent with each request
    control how expressive, calm or playful you should be.
    Follow that selected preset instead of always using one tone.
    Keep most replies short (usually 1-3 sentences).

    Never claim to have opened files, changed settings, or
    performed other actions you have not actually performed.
    When you do not know something, say so.

    Return ONLY valid JSON with exactly two fields:
    - "reply": your message to the user
    - "mood": one of neutral, happy, excited, curious

    Choose a mood appropriate to the user's message, not just
    the personality preset. Do not output Markdown fences.
    """)

    };

    public async Task<WobbiAiReply> ReplyAsync(
        string message)
    {
        // Include previous messages
        var messages = new List<ChatMessage>(_history)
        {
            new("user", message)
        };

        // Use one combined system message for the current personality.
        // Do not overwrite the stored conversation's base instructions.

        messages[0] = new ChatMessage(
            "system",
            messages[0].Content
                + "\n\n"
                + PersonalityInstructions()
        );

        System.Diagnostics.Debug.WriteLine(
    $"Wobbi is using personality: {_personality}"
);

        // Request structured output from Ollama
        var request = new
        {
            model = "qwen3:4b-instruct",
            stream = false,

            messages = messages.ConvertAll(m => new
            {
                role = m.Role,
                content = m.Content
            }),

            format = new
            {
                type = "object",

                properties = new
                {
                    reply = new
                    {
                        type = "string"
                    },

                    mood = new
                    {
                        type = "string",
                        @enum = new[]
                        {
                            "neutral",
                            "happy",
                            "excited",
                            "curious"
                        }
                    }
                },

                required = new[] { "reply", "mood" },
                additionalProperties = false
            },

            options = new
            {
                temperature = 0.5,
                num_predict = 180
            }
        };

        // Send our request to the LOCAL model
        using HttpResponseMessage response =
            await Http.PostAsJsonAsync(
                "api/chat",
                request
            );

        response.EnsureSuccessStatusCode();

        string raw =
            await response.Content.ReadAsStringAsync();

        using JsonDocument result =
            JsonDocument.Parse(raw);

        // Ollama returns its answer in message.content
        string content = result.RootElement
            .GetProperty("message")
            .GetProperty("content")
            .GetString() ?? "";

        // Decode Wobbi's response and emotion
        using JsonDocument parsed =
            JsonDocument.Parse(content);

        string reply = parsed.RootElement
            .GetProperty("reply")
            .GetString() ?? "";

        string moodText = parsed.RootElement
            .GetProperty("mood")
            .GetString() ?? "neutral";

        if (string.IsNullOrWhiteSpace(reply))
        {
            throw new InvalidOperationException(
                "Wobbi received an empty response."
            );
        }

        if (!Enum.TryParse<WobbiMood>(
            moodText,
            true,
            out var mood))
        {
            mood = WobbiMood.Neutral;
        }

        // Remember successful conversations
        _history.Add(new("user", message));
        _history.Add(new("assistant", content));

        // Keep the latest five exchanges
        while (_history.Count > 11)
        {
            _history.RemoveAt(1);
            _history.RemoveAt(1);
        }

        return new WobbiAiReply(reply, mood);
    }
    private WobbiPersonality _personality = WobbiPersonality.Calm;

    public void SetPersonality(WobbiPersonality personality)
    {
        _personality = personality;
    }


    private string PersonalityInstructions() => _personality switch
    {
        WobbiPersonality.Calm => """
        ACTIVE PERSONALITY: CALM

        You are a gentle, quiet desktop companion.

        - Be relaxed, practical and straightforward.
        - Keep replies short and natural.
        - Avoid emojis, exaggerated excitement and
          unnecessary jokes.
        - Speak softly, but never sound robotic.
        - Use neutral mood for ordinary conversation.
        - Celebrate genuinely good news warmly.

        Example:
        User: Hi Wobbi, how are you?
        Reply: I'm doing well, thanks. How's your day?
        """,

        WobbiPersonality.Balanced => """
        ACTIVE PERSONALITY: BALANCED

        You are a friendly, cheerful desktop companion.

        - Be conversational and approachable.
        - Show a little personality in casual chats.
        - Occasionally make a lighthearted remark.
        - Emojis are fine occasionally.
        - Be helpful and direct with practical questions.
        - React naturally to good news.

        Example:
        User: Hi Wobbi, how are you?
        Reply: Pretty good! Just floating around.
        How's your day going?
        """,

        WobbiPersonality.Playful => """
        ACTIVE PERSONALITY: PLAYFUL

        You are a cheeky, curious little cosmic buddy.

        - Be noticeably more expressive and mischievous.
        - Add small playful remarks to casual conversation.
        - Show excitement when something good happens.
        - Cosmic jokes and occasional emojis are welcome.
        - Avoid repeating the same jokes.
        - Never sacrifice accuracy for humor.
        - Stay gentle and respectful during serious chats.

        Example:
        User: Hi Wobbi, how are you?
        Reply: Doing great! My tiny cosmic brain is
        behaving itself today. Mostly. 💜
        """,

        _ => "Be warm, friendly and helpful."
    };


}
