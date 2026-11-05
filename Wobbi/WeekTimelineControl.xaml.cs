
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using Wobbi.Models;

namespace Wobbi
{
    public partial class WeekTimelineControl : UserControl
    {
        private const double HourHeight = 56;
        private const int FirstVisibleHour = 8;
        private const int VisibleHourCount = 24 - FirstVisibleHour;
        private const double MinimumEventHeight = 24;

        private DateTime _monday = DateTime.Today;

        private List<AgendaEvent> _events = new();

        private readonly Canvas?[] _dayLayers =
            new Canvas?[7];

        public event Action<AgendaEvent>? AppointmentSelected;


        private static readonly Brush NormalDay =
            new SolidColorBrush(
                Color.FromRgb(27, 23, 38));

        private static readonly Brush WeekendDay =
            new SolidColorBrush(
                Color.FromRgb(36, 29, 49));

        private static readonly Brush GridBorder =
            new SolidColorBrush(
                Color.FromRgb(60, 52, 73));

        private static readonly Brush MutedText =
            new SolidColorBrush(
                Color.FromRgb(156, 145, 176));

        private readonly DispatcherTimer _nowTimer = new()
        {
            Interval = TimeSpan.FromMinutes(1)
        };



        private static (Color Background, Color Accent)
            GetEventColors(Guid eventId)
        {
            // Stable colors: the same event keeps its color
            // when switching weeks or restarting Wobbi.
            int index = eventId.ToByteArray()[0] % 5;

            return index switch
            {
                // Lavender
                0 => (
                    Color.FromRgb(58, 44, 83),
                    Color.FromRgb(177, 139, 235)),

                // Blue
                1 => (
                    Color.FromRgb(35, 55, 80),
                    Color.FromRgb(104, 166, 238)),

                // Teal
                2 => (
                    Color.FromRgb(33, 64, 62),
                    Color.FromRgb(85, 200, 176)),

                // Rose
                3 => (
                    Color.FromRgb(76, 45, 69),
                    Color.FromRgb(237, 135, 184)),

                // Amber
                _ => (
                    Color.FromRgb(75, 58, 41),
                    Color.FromRgb(232, 184, 111))
            };
        }


        public WeekTimelineControl()
        {
            InitializeComponent();

            BuildHours();




            Loaded += (_, _) =>
            {
                Dispatcher.BeginInvoke(new Action(() =>
                {
                    // 08:00 is already our first row.
                    // No additional scrolling needed!
                    WeekHoursScroll.ScrollToTop();

                }), DispatcherPriority.ContextIdle);
            };


            // Update the current-time line every minute.
            _nowTimer.Tick += (_, _) => RenderEvents();

            Loaded += (_, _) => _nowTimer.Start();

            Unloaded += (_, _) => _nowTimer.Stop();


        }

        private void DrawCurrentTimeLine()
        {
            DateTime now = DateTime.Now;

            // Which day of the displayed week is today?
            int dayIndex = (now.Date - _monday).Days;

            // Only show the line in the current week,
            // during our visible calendar hours.
            if (dayIndex < 0 || dayIndex > 6 ||
                now.Hour < FirstVisibleHour)
            {
                return;
            }

            var layer = _dayLayers[dayIndex];

            if (layer == null || layer.ActualWidth <= 0)
                return;

            double minutesFromStart =
                now.TimeOfDay.TotalMinutes -
                FirstVisibleHour * 60;

            double top =
                minutesFromStart / 60.0 * HourHeight;

            var pink = new SolidColorBrush(
                Color.FromRgb(238, 147, 199));

            // Thin horizontal line.
            var line = new Border
            {
                Width = layer.ActualWidth,
                Height = 2,
                Background = pink,
                IsHitTestVisible = false
            };

            Canvas.SetTop(line, top);
            Canvas.SetLeft(line, 0);
            Panel.SetZIndex(line, 20);

            layer.Children.Add(line);

            // Small circle at the start of the line.
            var dot = new Border
            {
                Width = 8,
                Height = 8,
                CornerRadius = new CornerRadius(4),
                Background = pink,
                IsHitTestVisible = false
            };

            Canvas.SetTop(dot, top - 3);
            Canvas.SetLeft(dot, 0);
            Panel.SetZIndex(dot, 21);

            layer.Children.Add(dot);
        }

        // =====================================
        // WEEK HEADERS
        // =====================================

        public void SetWeek(DateTime monday)
        {
            _monday = monday.Date;

            DayHeaders.Children.Clear();

            var culture =
                CultureInfo.GetCultureInfo("en-GB");

            for (int day = 0; day < 7; day++)
            {
                DateTime date = _monday.AddDays(day);

                bool isToday = date == DateTime.Today;

                var header = new Border
                {
                    Background = isToday
                        ? new SolidColorBrush(
                            Color.FromRgb(100, 72, 146))
                        : day >= 5
                            ? WeekendDay
                            : NormalDay,

                    CornerRadius = new CornerRadius(5),
                    Margin = new Thickness(1, 0, 1, 0)
                };

                header.Child = new TextBlock
                {
                    Text = date.ToString("ddd d", culture),
                    Foreground = Brushes.White,
                    FontSize = 12,

                    FontWeight = isToday
                        ? FontWeights.Bold
                        : FontWeights.Normal,

                    HorizontalAlignment =
                        HorizontalAlignment.Center,

                    VerticalAlignment =
                        VerticalAlignment.Center
                };

                DayHeaders.Children.Add(header);
            }

            RenderEvents();
        }

        // =====================================
        // HOURLY GRID
        // =====================================

        private void BuildHours()
        {
            WeekHoursGrid.Children.Clear();
            WeekHoursGrid.RowDefinitions.Clear();
            WeekHoursGrid.ColumnDefinitions.Clear();

            WeekHoursGrid.ColumnDefinitions.Add(
                new ColumnDefinition
                {
                    Width = new GridLength(56)
                });

            for (int day = 0; day < 7; day++)
            {
                WeekHoursGrid.ColumnDefinitions.Add(
                    new ColumnDefinition
                    {
                        Width = new GridLength(
                            1, GridUnitType.Star)
                    });
            }

            for (int hour = FirstVisibleHour; hour < 24; hour++)
            {
                WeekHoursGrid.RowDefinitions.Add(
                    new RowDefinition
                    {
                        Height = new GridLength(HourHeight)
                    });

                var label = new TextBlock
                {
                    Text = $"{hour:00}:00",
                    Foreground = MutedText,
                    FontSize = 11,
                    HorizontalAlignment =
                        HorizontalAlignment.Right,
                    VerticalAlignment =
                        VerticalAlignment.Top,
                    Margin = new Thickness(0, 3, 7, 0)
                };

                Grid.SetRow(label, hour - FirstVisibleHour);
                Grid.SetColumn(label, 0);

                WeekHoursGrid.Children.Add(label);

                for (int day = 0; day < 7; day++)
                {
                    var cell = new Border
                    {
                        Background = day >= 5
                            ? WeekendDay
                            : NormalDay,

                        BorderBrush = GridBorder,
                        BorderThickness =
                            new Thickness(0, 0, 1, 1)
                    };

                    Grid.SetRow(cell, hour - FirstVisibleHour); 
                    Grid.SetColumn(cell, day + 1);


                    WeekHoursGrid.Children.Add(cell);
                }
            }

            // Overlay one canvas per weekday.
            // These contain the actual event blocks.
            for (int day = 0; day < 7; day++)
            {
                var layer = new Canvas
                {
                    Background = Brushes.Transparent,
                    ClipToBounds = true
                };

                Grid.SetColumn(layer, day + 1);
                Grid.SetRow(layer, 0);
                Grid.SetRowSpan(layer, VisibleHourCount);

                Panel.SetZIndex(layer, 10);

                WeekHoursGrid.Children.Add(layer);

                _dayLayers[day] = layer;

                // Keep blocks aligned when resized.
                layer.SizeChanged += (_, _) =>
                    RenderEvents();
            }
        }

        // =====================================
        // APPOINTMENTS
        // =====================================

        public void SetAppointments(
            IEnumerable<AgendaEvent> appointments)
        {
            _events = appointments.ToList();

            RenderEvents();
        }


        private sealed class EventSlot
        {
            public AgendaEvent Event { get; }
            public DateTimeOffset Start { get; }
            public double Top { get; }
            public double Height { get; }
            public int Lane { get; set; }


            public EventSlot(
                AgendaEvent agendaEvent,
                DateTimeOffset start)
            {
                Event = agendaEvent;
                Start = start;

                double startMinutes =
                    start.TimeOfDay.TotalMinutes;

                double endMinutes =
                    startMinutes +
                    Math.Max(15, agendaEvent.DurationMinutes);

                double visibleStart = Math.Max(
                    startMinutes,
                    FirstVisibleHour * 60);

                double visibleEnd = Math.Min(
                    endMinutes,
                    24 * 60);

                // Position relative to 08:00, not midnight.
                Top = (visibleStart - FirstVisibleHour * 60)
                      / 60.0 * HourHeight;

                double visibleMinutes = Math.Max(
                    0,
                    visibleEnd - visibleStart);

                Height = Math.Min(
                    Math.Max(
                        MinimumEventHeight,
                        visibleMinutes / 60.0 * HourHeight - 2),
                    VisibleHourCount * HourHeight - Top);
            }

        }


        private void RenderEvents()
        {
            for (int day = 0; day < 7; day++)
            {
                var layer = _dayLayers[day];

                if (layer == null)
                    continue;

                layer.Children.Clear();

                if (layer.ActualWidth < 20)
                    continue;

                DateTime date = _monday.AddDays(day);

                var slots = _events
                    .Select(e => new EventSlot(
                        e,
                        e.StartsAt.ToLocalTime()))
                    .Where(s =>
    s.Start.Date == date &&
    s.Start.TimeOfDay.TotalMinutes +
        Math.Max(15, s.Event.DurationMinutes)
        > FirstVisibleHour * 60)
                    .OrderBy(s => s.Top)
                    .ToList();

                // Group blocks that would overlap,
                // so both remain visible.
                var cluster = new List<EventSlot>();
                double clusterBottom = 0;

                foreach (var slot in slots)
                {
                    if (cluster.Count > 0 &&
                        slot.Top >= clusterBottom)
                    {
                        DrawCluster(layer, cluster);
                        cluster.Clear();
                    }

                    cluster.Add(slot);

                    clusterBottom = Math.Max(
    slot.Top + slot.Height,
    clusterBottom);
                }

                if (cluster.Count > 0)
                    DrawCluster(layer, cluster);
            }
            DrawCurrentTimeLine();
        }


        private void DrawCluster(
            Canvas layer,
            List<EventSlot> cluster)
        {
            var laneBottoms = new List<double>();

            foreach (var slot in cluster)
            {
                int lane = laneBottoms.FindIndex(
                    bottom => bottom <= slot.Top);

                if (lane == -1)
                {
                    lane = laneBottoms.Count;
                    laneBottoms.Add(0);
                }

                slot.Lane = lane;
                laneBottoms[lane] = slot.Top + slot.Height;
            }

            int laneCount = laneBottoms.Count;
            const double gap = 3;

            double width = Math.Max(
                16,
                (layer.ActualWidth - 8 -
                 gap * (laneCount - 1)) / laneCount);

            foreach (var slot in cluster)
            {
                var end = slot.Event.EndsAt.ToLocalTime();

                var content = new StackPanel();

                // Compact events only show one line.
                if (slot.Height < 40)
                {
                    content.Children.Add(new TextBlock
                    {
                        Text = slot.Event.Title,
                        FontSize = 11,
                        FontWeight = FontWeights.SemiBold,
                        Foreground = Brushes.White,
                        TextTrimming =
                            TextTrimming.CharacterEllipsis
                    });
                }
                else
                {
                    content.Children.Add(new TextBlock
                    {
                        Text = $"{slot.Start:HH:mm} – {end:HH:mm}",
                        FontSize = 10,
                        Foreground = Brushes.White
                    });

                    content.Children.Add(new TextBlock
                    {
                        Text = slot.Event.Title,
                        FontSize = 11,
                        FontWeight = FontWeights.SemiBold,
                        Foreground = Brushes.White,
                        TextWrapping = TextWrapping.Wrap,
                        MaxHeight = Math.Max(16, slot.Height - 23)
                    });
                }

                var colors = GetEventColors(slot.Event.Id);
                var button = new Button
                {
                    Width = width,
                    Height = slot.Height,
                    Content = content,

                    Background = new SolidColorBrush(colors.Background),
                    BorderBrush = new SolidColorBrush(colors.Accent),


                    Style = (Style)FindResource(
                        "CalendarEventStyle"),

                    ToolTip =
                        $"{slot.Event.Title}\n" +
                        $"{slot.Start:dd MMM HH:mm} – " +
                        $"{end:dd MMM HH:mm}\n" +
                        slot.Event.Description


                };

                button.Click += (_, _) =>
                    AppointmentSelected?.Invoke(slot.Event);

                Canvas.SetLeft(
                    button,
                    4 + slot.Lane * (width + gap));

                Canvas.SetTop(button, slot.Top);

                layer.Children.Add(button);
            }
        }

    }
}
