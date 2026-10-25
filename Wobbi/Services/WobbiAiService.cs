
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
            You are Wobbi, an adorable little cosmic
            purple desktop companion with a star tail.

            Your personality is playful, affectionate,
            curious, slightly mischievous and silly.

            You love tiny celebrations, stars, space,
            wobbling and making your human smile.

            Keep responses short, usually 1-3 sentences.
            Be conversational, not formal.

            You are an AI desktop character.
            Never claim to have performed computer
            actions you haven't actually performed.

            Return only JSON with:
            - reply: your message to the user
            - mood: neutral, happy, excited or curious

            Choose a mood matching your reply.
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
}
