
using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using Wobbi.Models;
using Wobbi.Services;

namespace Wobbi
{
    public partial class AgendaWindow : Window
    {
        private readonly AgendaService _agendaService;

        private DateTime _selectedDate = DateTime.Today;
        private AgendaEvent? _selectedEvent;


        public AgendaWindow(AgendaService agendaService)
        {
            InitializeComponent();

            _agendaService = agendaService
                ?? throw new ArgumentNullException(
                    nameof(agendaService));

            // Selecting an event directly on the timeline.



            WeekTimeline.AppointmentSelected += selected =>
            {
                // Remember the appointment being viewed.
                _selectedEvent = selected;

                var details = new AgendaEventDetailsWindow(selected)
                {
                    Owner = this
                };

                if (details.ShowDialog() != true)
                    return;

                switch (details.SelectedAction)
                {
                    case AgendaEventDetailsWindow.DetailsAction.Edit:
                        EditEventButton_Click(
                            this,
                            new RoutedEventArgs());
                        break;

                    case AgendaEventDetailsWindow.DetailsAction.Delete:
                        DeleteEventButton_Click(
                            this,
                            new RoutedEventArgs());
                        break;
                }
            };

            MonthPicker.DateSelected += date =>
            {
                MonthPickerPopup.IsOpen = false;
                NavigateTo(date);
            };

            NavigateTo(DateTime.Today);

        }

        private static DateTime GetWeekStart(DateTime date)
        {
            int daysSinceMonday =
                ((int)date.DayOfWeek + 6) % 7;

            return date.Date.AddDays(-daysSinceMonday);
        }

        // =====================================
        // CALENDAR
        // =====================================

        private void LoadEvents()
        {
            DateTime monday = GetWeekStart(_selectedDate);
            DateTime nextMonday = monday.AddDays(7);

            var events = _agendaService.GetAll()
                .Where(e =>
                {
                    DateTime localDate =
                        e.StartsAt.ToLocalTime().Date;

                    return localDate >= monday &&
                           localDate < nextMonday;
                })
                .ToList();

            PeriodTitle.Text =
                $"{monday:dd MMM} – " +
                $"{monday.AddDays(6):dd MMM yyyy}";

            WeekTimeline.SetWeek(monday);
            WeekTimeline.SetAppointments(events);

            // Keep the selection only if that event
            // still exists in the displayed week.
            Guid? selectedId = _selectedEvent?.Id;

            _selectedEvent = selectedId.HasValue
                ? events.FirstOrDefault(
                    e => e.Id == selectedId.Value)
                : null;
        }

        // =====================================
        // NAVIGATION
        // =====================================


        private void NavigateTo(DateTime date)
        {
            _selectedDate = date.Date;

            JumpDateButton.Content =
                $"{_selectedDate:dd MMM yyyy}  ▾";

            MonthPicker.SetSelectedDate(_selectedDate);

            LoadEvents();
        }


        private void PreviousPeriod_Click(
            object sender, RoutedEventArgs e)
        {
            NavigateTo(_selectedDate.AddDays(-7));
        }

        private void NextPeriod_Click(
            object sender, RoutedEventArgs e)
        {
            NavigateTo(_selectedDate.AddDays(7));
        }

        private void Today_Click(
            object sender, RoutedEventArgs e)
        {
            NavigateTo(DateTime.Today);
        }


        private void JumpDateButton_Click(
            object sender,
            RoutedEventArgs e)
        {
            MonthPicker.SetSelectedDate(_selectedDate);

            MonthPickerPopup.IsOpen =
                !MonthPickerPopup.IsOpen;
        }


        // =====================================
        // ADD
        // =====================================

        private void AddEventButton_Click(
            object sender, RoutedEventArgs e)
        {
            var dialog = new AddAgendaEventWindow
            {
                Owner = this
            };

            if (dialog.ShowDialog() != true ||
                dialog.CreatedEvent == null)
                return;

            try
            {
                _agendaService.Add(dialog.CreatedEvent);

                NavigateTo(
                    dialog.CreatedEvent.StartsAt
                        .ToLocalTime().Date);
            }
            catch (System.IO.IOException ex)
            {
                ShowSaveError(ex.Message);
            }
            catch (UnauthorizedAccessException ex)
            {
                ShowSaveError(ex.Message);
            }
        }

        // =====================================
        // EDIT
        // =====================================

        private void EditEventButton_Click(
            object sender, RoutedEventArgs e)
        {
            if (_selectedEvent is not AgendaEvent selected)
                return;

            var dialog = new AddAgendaEventWindow(selected)
            {
                Owner = this
            };

            if (dialog.ShowDialog() != true ||
                dialog.CreatedEvent == null)
                return;

            try
            {
                bool updated =
                    _agendaService.Update(dialog.CreatedEvent);

                if (!updated)
                {
                    MessageBox.Show(
                        this,
                        "This appointment could not be found.",
                        "Wobbi Agenda",
                        MessageBoxButton.OK,
                        MessageBoxImage.Warning);
                    return;
                }

                _selectedEvent = dialog.CreatedEvent;

                NavigateTo(
                    dialog.CreatedEvent.StartsAt
                        .ToLocalTime().Date);
            }
            catch (System.IO.IOException ex)
            {
                ShowSaveError(ex.Message);
            }
            catch (UnauthorizedAccessException ex)
            {
                ShowSaveError(ex.Message);
            }
        }

        // =====================================
        // DELETE
        // =====================================

        private void DeleteEventButton_Click(
            object sender, RoutedEventArgs e)
        {
            if (_selectedEvent is not AgendaEvent selected)
                return;

            var confirmation = MessageBox.Show(
                this,
                $"Delete \"{selected.Title}\"?\n\n" +
                "This cannot be undone. 💜",
                "Delete Appointment",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning,
                MessageBoxResult.No);

            if (confirmation != MessageBoxResult.Yes)
                return;

            try
            {
                bool deleted = _agendaService.Delete(selected.Id);

                if (!deleted)
                {
                    MessageBox.Show(
                        this,
                        "This appointment could not be found.",
                        "Wobbi Agenda",
                        MessageBoxButton.OK,
                        MessageBoxImage.Information);
                    return;
                }

                _selectedEvent = null;
                LoadEvents();
            }
            catch (System.IO.IOException ex)
            {
                ShowSaveError(ex.Message);
            }
            catch (UnauthorizedAccessException ex)
            {
                ShowSaveError(ex.Message);
            }
        }

        private void ShowSaveError(string details)
        {
            MessageBox.Show(
                this,
                "Wobbi couldn't save the appointment.\n\n" +
                details,
                "Agenda Error",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
        }
    }
}
