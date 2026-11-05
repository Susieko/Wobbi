
using System;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Threading;
using Wobbi.Models;

namespace Wobbi
{
    public partial class ReminderToastWindow : Window
    {
        private readonly DispatcherTimer _closeTimer = new()
        {
            Interval = TimeSpan.FromSeconds(8)
        };

        // Windows extended window styles.
        private const int GWL_EXSTYLE = -20;
        private const int WS_EX_TRANSPARENT = 0x00000020;
        private const int WS_EX_NOACTIVATE = 0x08000000;

        [DllImport("user32.dll", EntryPoint = "GetWindowLongW")]
        private static extern int GetWindowLong(
            IntPtr hwnd, int index);

        [DllImport("user32.dll", EntryPoint = "SetWindowLongW")]
        private static extern int SetWindowLong(
            IntPtr hwnd, int index, int value);

        public ReminderToastWindow(AgendaEvent agendaEvent)
        {
            InitializeComponent();

            var start = agendaEvent.StartsAt.ToLocalTime();

            ReminderText.Text =
                $"{agendaEvent.Title}\n" +
                $"Starts at {start:HH:mm}";

            // Bottom-right corner of the primary work area.
            var area = SystemParameters.WorkArea;

            Left = area.Right - Width - 20;
            Top = area.Top + 20;


            _closeTimer.Tick += (_, _) =>
            {
                _closeTimer.Stop();
                Close();
            };

            Loaded += (_, _) => _closeTimer.Start();

            Closed += (_, _) => _closeTimer.Stop();
        }

        protected override void OnSourceInitialized(EventArgs e)
        {
            base.OnSourceInitialized(e);

            var hwnd = new WindowInteropHelper(this).Handle;

            int style = GetWindowLong(
                hwnd, GWL_EXSTYLE);

            // Don't activate the window or intercept
            // mouse clicks intended for other applications.
            SetWindowLong(
                hwnd,
                GWL_EXSTYLE,
                style | WS_EX_NOACTIVATE | WS_EX_TRANSPARENT);
        }
    }
}
