
using System;
using System.Windows;
using Wobbi.Models;

namespace Wobbi
{
    public partial class AgendaEventDetailsWindow : Window
    {
        public enum DetailsAction
        {
            None,
            Edit,
            Delete
        }

        public DetailsAction SelectedAction { get; private set; }
            = DetailsAction.None;

        public AgendaEventDetailsWindow(AgendaEvent agendaEvent)
        {
            InitializeComponent();

            ArgumentNullException.ThrowIfNull(agendaEvent);

            var start = agendaEvent.StartsAt.ToLocalTime();
            var end = agendaEvent.EndsAt.ToLocalTime();

            EventTitleText.Text = agendaEvent.Title;

            EventTimeText.Text =
                $"From: {start:ddd dd MMM yyyy, HH:mm}\n" +
                $"Until: {end:ddd dd MMM yyyy, HH:mm}\n" +
                $"Duration: {agendaEvent.DurationMinutes} minutes";

            EventDescriptionText.Text =
                string.IsNullOrWhiteSpace(agendaEvent.Description)
                    ? "No description added."
                    : agendaEvent.Description;
        }

        private void Edit_Click(object sender, RoutedEventArgs e)
        {
            SelectedAction = DetailsAction.Edit;
            DialogResult = true;
        }

        private void Delete_Click(object sender, RoutedEventArgs e)
        {
            SelectedAction = DetailsAction.Delete;
            DialogResult = true;
        }

        private void Close_Click(object sender, RoutedEventArgs e)
        {
            SelectedAction = DetailsAction.None;
            DialogResult = false;
        }
    }
}
