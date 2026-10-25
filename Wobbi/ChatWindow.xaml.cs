
using System;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using Wobbi.Services;

namespace Wobbi;

public partial class ChatWindow : Window
{
    private readonly WobbiAiService _ai = new();
    private readonly Action<WobbiMood> _onMood;
    private readonly Action<string> _onReply;

    private bool _isSending = false;

    public ChatWindow(
        Action<WobbiMood> onMood,
        Action<string> onReply)
    {
        InitializeComponent();

        _onMood = onMood;
        _onReply = onReply;

        AddMessage(
            "Wobbi",
            "Psst! I'm here! What's on your mind? 💜",
            true
        );

        MessageInput.Focus();
    }

    private async void Send_Click(
        object sender,
        RoutedEventArgs e)
    {
        await SendMessageAsync();
    }

    private async void MessageInput_KeyDown(
        object sender,
        KeyEventArgs e)
    {
        if (e.Key == Key.Enter)
        {
            e.Handled = true;
            await SendMessageAsync();
        }
    }

    private async Task SendMessageAsync()
    {
        string message = MessageInput.Text.Trim();

        if (string.IsNullOrWhiteSpace(message)
            || _isSending)
        {
            return;
        }

        _isSending = true;

        // Display the user's message
        AddMessage("You", message, false);

        MessageInput.Clear();
        MessageInput.IsEnabled = false;
        SendButton.IsEnabled = false;

        // Show Wobbi thinking
        TextBlock thinkingMessage = AddMessage(
            "Wobbi",
            "Thinking among the stars... ✨",
            true
        );

        try
        {
            // Ask our local Ollama brain
            WobbiAiReply reply =
                await _ai.ReplyAsync(message);

            // Replace thinking text with real answer
            thinkingMessage.Text = reply.Text;

            // React emotionally
            _onMood(reply.Mood);

            // Show Wobbi's speech bubble
            _onReply(reply.Text);
        }
        catch (Exception ex)
        {
            thinkingMessage.Text =
                "Oh no! My brain got tangled " +
                "in some stardust. 💜";

            Debug.WriteLine(ex);
        }
        finally
        {
            _isSending = false;

            MessageInput.IsEnabled = true;
            SendButton.IsEnabled = true;

            MessageInput.Focus();
            ScrollToBottom();
        }
    }

    // Create a new conversation bubble
    private TextBlock AddMessage(
        string sender,
        string message,
        bool fromWobbi)
    {
        var container = new StackPanel
        {
            HorizontalAlignment = fromWobbi
                ? HorizontalAlignment.Left
                : HorizontalAlignment.Right,

            MaxWidth = 330,
            Margin = new Thickness(0, 0, 0, 12)
        };

        // Sender label
        var label = new TextBlock
        {
            Text = sender,
            FontSize = 12,
            Foreground = new SolidColorBrush(
                fromWobbi
                    ? Color.FromRgb(210, 187, 255)
                    : Color.FromRgb(181, 175, 207)
            ),

            Margin = new Thickness(0, 0, 0, 6)
        };

        // Actual message text
        var messageText = new TextBlock
        {
            Text = message,
            TextWrapping = TextWrapping.Wrap,
            Foreground = Brushes.White,
            FontSize = 13
        };

        // Purple message bubble
        var bubble = new Border
        {
            Background = new SolidColorBrush(
                fromWobbi
                    ? Color.FromRgb(57, 48, 81)
                    : Color.FromRgb(48, 44, 73)
            ),

            CornerRadius = new CornerRadius(12),
            Padding = new Thickness(12),
            Child = messageText
        };

        container.Children.Add(label);
        container.Children.Add(bubble);

        MessagesPanel.Children.Add(container);

        ScrollToBottom();

        return messageText;
    }

    private void ScrollToBottom()
    {
        Dispatcher.BeginInvoke(
            new Action(() =>
                ChatScrollViewer.ScrollToEnd()
            ),
            DispatcherPriority.Background
        );
    }
}
