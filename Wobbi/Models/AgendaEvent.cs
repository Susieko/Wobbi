
using System;
using System.Text.Json.Serialization;

namespace Wobbi.Models
{
    public class AgendaEvent
    {
        public Guid Id { get; set; } = Guid.NewGuid();

        public string Title { get; set; } = string.Empty;

        public string Description { get; set; } = string.Empty;

        public DateTimeOffset StartsAt { get; set; } = DateTimeOffset.Now;

        public int DurationMinutes { get; set; } = 60;

        [JsonIgnore]
        public DateTimeOffset EndsAt =>
            StartsAt.AddMinutes(DurationMinutes);

        public bool ReminderSent { get; set; } = false;
        // True while a delivered reminder has not been acknowledged.
        public bool ReminderPending { get; set; } = false;

        [JsonIgnore]
        public DateTimeOffset ReminderAt => StartsAt.AddHours(-1);
    }
}
