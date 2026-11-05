
using System;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace Wobbi
{
    public partial class MonthPickerControl : UserControl
    {
        private DateTime _displayMonth =
            new(DateTime.Today.Year, DateTime.Today.Month, 1);

        private DateTime _selectedDate = DateTime.Today;

        public event Action<DateTime>? DateSelected;

        public MonthPickerControl()
        {
            InitializeComponent();
            RenderMonth();
        }

        public void SetSelectedDate(DateTime date)
        {
            _selectedDate = date.Date;

            _displayMonth = new DateTime(
                date.Year, date.Month, 1);

            RenderMonth();
        }

        private void RenderMonth()
        {
            DaysGrid.Children.Clear();

            var culture = CultureInfo.GetCultureInfo("en-GB");

            MonthTitle.Text = _displayMonth.ToString(
                "MMMM yyyy", culture);

            // Monday is the first weekday.
            int offset =
                ((int)_displayMonth.DayOfWeek + 6) % 7;

            DateTime firstCell =
                _displayMonth.AddDays(-offset);

            // Six rows, seven days per row.
            for (int i = 0; i < 42; i++)
            {
                DateTime date = firstCell.AddDays(i);

                bool isSelected =
                    date.Date == _selectedDate;

                bool isToday =
                    date.Date == DateTime.Today;

                bool isCurrentMonth =
                    date.Month == _displayMonth.Month &&
                    date.Year == _displayMonth.Year;

                var button = new Button
                {
                    Content = date.Day.ToString(),
                    Height = 34,
                    Margin = new Thickness(1),
                    Style = (Style)FindResource(
                        "PickerButtonStyle"),

                    Foreground = isSelected
                        ? Brushes.White
                        : isCurrentMonth
                            ? new SolidColorBrush(
                                Color.FromRgb(248, 245, 255))
                            : new SolidColorBrush(
                                Color.FromRgb(113, 102, 129)),

                    Background = isSelected
                        ? new SolidColorBrush(
                            Color.FromRgb(152, 118, 219))
                        : Brushes.Transparent,

                    BorderBrush = isToday
                        ? new SolidColorBrush(
                            Color.FromRgb(210, 169, 245))
                        : Brushes.Transparent,

                    ToolTip = date.ToString(
                        "dddd d MMMM yyyy", culture)
                };

                button.Click += (_, _) =>
                {
                    _selectedDate = date.Date;
                    RenderMonth();
                    DateSelected?.Invoke(date.Date);
                };

                DaysGrid.Children.Add(button);
            }
        }

        private void PreviousMonth_Click(
            object sender, RoutedEventArgs e)
        {
            _displayMonth = _displayMonth.AddMonths(-1);
            RenderMonth();
        }

        private void NextMonth_Click(
            object sender, RoutedEventArgs e)
        {
            _displayMonth = _displayMonth.AddMonths(1);
            RenderMonth();
        }

        private void Today_Click(
            object sender, RoutedEventArgs e)
        {
            SetSelectedDate(DateTime.Today);
            DateSelected?.Invoke(DateTime.Today);
        }
    }
}
