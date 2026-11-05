using System;
using System.Globalization;
using System.Windows;
using Wobbi.Models;

namespace Wobbi
{
    public partial class AddAgendaEventWindow : Window
    {
        private readonly AgendaEvent? _editingEvent;
        private DateTime _selectedDate = DateTime.Today;

        public AgendaEvent? CreatedEvent { get; private set; }

        public AddAgendaEventWindow()
            : this(null)
        {
        }

        public AddAgendaEventWindow(AgendaEvent? existingEvent)
        {
            InitializeComponent();

            _editingEvent = existingEvent;

            EventMonthPicker.DateSelected += date =>
            {
                _selectedDate = date.Date;
                UpdateDateButton();
                DatePopup.IsOpen = false;
            };

            if (_editingEvent != null)
            {
                Title = "Edit Appointment 💜";

                TitleInput.Text = _editingEvent.Title;
                DescriptionInput.Text = _editingEvent.Description ?? "";

                var localTime = _editingEvent.StartsAt.ToLocalTime();
                _selectedDate = localTime.Date;

                TimeInput.Text = localTime.ToString("HH:mm");
                DurationInput.Text =
                    _editingEvent.DurationMinutes.ToString(
                        CultureInfo.InvariantCulture);
            }
            else
            {
                _selectedDate = DateTime.Today;
                TimeInput.Text = DateTime.Now.ToString("HH:mm");
                DurationInput.Text = "60";
            }

            UpdateDateButton();
            EventMonthPicker.SetSelectedDate(_selectedDate);
        }

        private void UpdateDateButton()
        {
            DateButton.Content = $"{_selectedDate:dd-MM-yyyy}  ▾";
        }

        private void DateButton_Click(object sender, RoutedEventArgs e)
        {
            EventMonthPicker.SetSelectedDate(_selectedDate);
            DatePopup.IsOpen = !DatePopup.IsOpen;
        }

        private void Save_Click(object sender, RoutedEventArgs e)
        {
            string title = TitleInput.Text.Trim();
            string description = DescriptionInput.Text.Trim();
            string timeText = TimeInput.Text.Trim();

            if (string.IsNullOrWhiteSpace(title))
            {
                ShowWarning("Please enter a title.");
                return;
            }

            if (!TimeSpan.TryParseExact(
                    timeText,
                    "hh\\:mm",
                    CultureInfo.InvariantCulture,
                    out TimeSpan time))
            {
                ShowWarning("Please enter a valid time in HH:mm format.");
                return;
            }

            if (!int.TryParse(
                    DurationInput.Text.Trim(),
                    NumberStyles.None,
                    CultureInfo.InvariantCulture,
                    out int durationMinutes)
                || durationMinutes < 15
                || durationMinutes > 720)
            {
                ShowWarning("Please enter a duration between 15 and 720 minutes.");
                return;
            }

            DateTime localDateTime =
                _selectedDate.Date.Add(time);

            var start = new DateTimeOffset(localDateTime);

            CreatedEvent = new AgendaEvent
            {
                Id = _editingEvent?.Id ?? Guid.NewGuid(),
                Title = title,
                Description = description,
                StartsAt = start,
                DurationMinutes = durationMinutes,
                ReminderSent = _editingEvent?.ReminderSent ?? false,
                ReminderPending = _editingEvent?.ReminderPending ?? false
            };

            DialogResult = true;
        }

        private void ShowWarning(string message)
        {
            MessageBox.Show(
                this,
                message,
                "Wobbi Appointment",
                MessageBoxButton.OK,
                MessageBoxImage.Warning);
        }
    }
}