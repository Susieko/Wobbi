
using System;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using Wobbi.Models;
using Wobbi.Services;

namespace Wobbi
{
    public partial class ReminderCenterWindow : Window
    {
        private readonly AgendaService _agendaService;

        public event EventHandler? RemindersChanged;

        public ReminderCenterWindow(
            AgendaService agendaService)
        {
            InitializeComponent();

            _agendaService = agendaService
                ?? throw new ArgumentNullException(
                    nameof(agendaService));

            Refresh();
        }

        public void Refresh()
        {
            var pending =
                _agendaService.GetPendingReminders();

            ReminderList.ItemsSource = pending;

            EmptyText.Visibility =
                pending.Count == 0
                    ? Visibility.Visible
                    : Visibility.Collapsed;
        }

        private void DismissReminder_Click(
            object sender,
            RoutedEventArgs e)
        {
            if (sender is not Button button ||
                button.DataContext is not AgendaEvent selected)
                return;

            try
            {
                _agendaService.DismissReminder(selected.Id);

                Refresh();

                RemindersChanged?.Invoke(
                    this, EventArgs.Empty);

                // Last reminder dismissed?
                if (!_agendaService.HasPendingReminders())
                    Close();
            }
            catch (Exception ex) when (
                ex is IOException or UnauthorizedAccessException)
            {
                MessageBox.Show(
                    this,
                    "Wobbi couldn't save the reminder status.\n\n"
                    + ex.Message,
                    "Wobbi Reminder Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
            }
        }
    }
}
