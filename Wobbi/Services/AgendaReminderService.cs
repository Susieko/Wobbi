
using System;
using System.Diagnostics;
using System.Linq;
using System.Windows.Threading;
using Wobbi.Models;

namespace Wobbi.Services
{
    public sealed class AgendaReminderService
    {
        private readonly AgendaService _agendaService;
        private readonly Action<AgendaEvent> _showReminder;
        private readonly DispatcherTimer _timer;

        private bool _isChecking;

        public AgendaReminderService(
            AgendaService agendaService,
            Action<AgendaEvent> showReminder)
        {
            _agendaService = agendaService;
            _showReminder = showReminder;

            _timer = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(30)
            };

            _timer.Tick += (_, _) => CheckReminders();
        }

        public void Start()
        {
            if (_timer.IsEnabled)
                return;

            // Check immediately when Wobbi starts.
            CheckReminders();

            _timer.Start();
        }

        public void Stop()
        {
            _timer.Stop();
        }

        private void CheckReminders()
        {
            if (_isChecking)
                return;

            _isChecking = true;

            try
            {
                var now = DateTimeOffset.Now;

                var dueEvents = _agendaService
                    .GetAll()
                    .Where(e =>
                        !e.ReminderSent &&
                        e.ReminderAt <= now &&
                        e.StartsAt > now)
                    .ToList();

                foreach (var agendaEvent in dueEvents)
                {
                    try
                    {
                        // Persist the pending reminder first.
                        _agendaService.MarkReminderSent(
                            agendaEvent.Id);

                        // Then display it.
                        _showReminder(agendaEvent);
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine(
                            $"Agenda reminder failed: {ex}");
                    }
                }
            }
            finally
            {
                _isChecking = false;
            }
        }
    }
}
