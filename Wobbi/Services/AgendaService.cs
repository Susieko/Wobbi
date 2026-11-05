
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using Wobbi.Models;

namespace Wobbi.Services
{
    public class AgendaService
    {
        private readonly string _filePath;
        private readonly List<AgendaEvent> _events;

        public bool RecoveredFromBackup { get; private set; }

        public string? DamagedFilePath { get; private set; }


        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            WriteIndented = true
        };

        public AgendaService()
        {
            string folder = Path.Combine(
                Environment.GetFolderPath(
                    Environment.SpecialFolder.LocalApplicationData),
                "Wobbi"
            );

            Directory.CreateDirectory(folder);

            _filePath = Path.Combine(folder, "agenda.json");
            _events = LoadEvents();
        }

        // Get all events, sorted by date.
        public IReadOnlyList<AgendaEvent> GetAll()
        {
            return _events
                .OrderBy(e => e.StartsAt)
                .ToList();
        }

        // Add a new event.

        public void Add(AgendaEvent newEvent)
        {
            ArgumentNullException.ThrowIfNull(newEvent);

            if (string.IsNullOrWhiteSpace(newEvent.Title))
                throw new ArgumentException(
                    "Event needs a title.");

            if (_events.Any(e => e.Id == newEvent.Id))
                throw new InvalidOperationException(
                    "Duplicate event ID.");

            newEvent.Title = newEvent.Title.Trim();

            _events.Add(newEvent);

            try
            {
                SaveEvents();
            }
            catch
            {
                // Saving failed.
                // Remove the unsaved appointment from memory.
                _events.Remove(newEvent);

                throw;
            }
        }

        // Update an existing event.

        public bool Update(AgendaEvent updatedEvent)
        {
            ArgumentNullException.ThrowIfNull(updatedEvent);

            if (string.IsNullOrWhiteSpace(updatedEvent.Title))
                throw new ArgumentException(
                    "Event needs a title.");

            var existing = _events
                .FirstOrDefault(e => e.Id == updatedEvent.Id);

            if (existing == null)
                return false;

            // Remember the previous values.
            string oldTitle = existing.Title;
            string oldDescription = existing.Description;
            DateTimeOffset oldStartsAt = existing.StartsAt;
            int oldDuration = existing.DurationMinutes;
            bool oldReminderSent = existing.ReminderSent;
            bool oldReminderPending = existing.ReminderPending;

            bool timeChanged =
                existing.StartsAt != updatedEvent.StartsAt;

            try
            {
                existing.Title = updatedEvent.Title.Trim();
                existing.Description = updatedEvent.Description;
                existing.StartsAt = updatedEvent.StartsAt;
                existing.DurationMinutes =
                    updatedEvent.DurationMinutes;

                if (timeChanged)
                {
                    // Rescheduling requires a new reminder.
                    existing.ReminderSent = false;
                    existing.ReminderPending = false;
                }

                SaveEvents();
                return true;
            }
            catch
            {
                // Restore the original appointment.
                existing.Title = oldTitle;
                existing.Description = oldDescription;
                existing.StartsAt = oldStartsAt;
                existing.DurationMinutes = oldDuration;
                existing.ReminderSent = oldReminderSent;
                existing.ReminderPending = oldReminderPending;

                throw;
            }
        }


        // Delete an event.

        public bool Delete(Guid id)
        {
            // Preserve the existing list and its ordering.
            var previousEvents = _events.ToList();

            int removedCount =
                _events.RemoveAll(e => e.Id == id);

            if (removedCount == 0)
                return false;

            try
            {
                SaveEvents();
                return true;
            }
            catch
            {
                // Restore deleted appointments if saving fails.
                _events.Clear();
                _events.AddRange(previousEvents);

                throw;
            }
        }


        // Mark an event reminder as delivered.


        public void MarkReminderSent(Guid id)
        {
            var agendaEvent = _events
                .FirstOrDefault(e => e.Id == id);

            if (agendaEvent == null || agendaEvent.ReminderSent)
                return;

            // Remember the previous state.
            bool previousSent = agendaEvent.ReminderSent;
            bool previousPending = agendaEvent.ReminderPending;

            agendaEvent.ReminderSent = true;
            agendaEvent.ReminderPending = true;

            try
            {
                SaveEvents();
            }
            catch
            {
                // Saving failed! Restore the original state
                // so Wobbi can retry on the next timer check.
                agendaEvent.ReminderSent = previousSent;
                agendaEvent.ReminderPending = previousPending;

                throw;
            }
        }



        // Load previously saved events.

        private List<AgendaEvent> LoadEvents()
        {
            string backupPath = _filePath + ".bak";

            // Brand-new installation: no appointments yet.
            if (!File.Exists(_filePath) &&
                !File.Exists(backupPath))
            {
                return new List<AgendaEvent>();
            }

            // Attempt to load the primary agenda.
            if (File.Exists(_filePath))
            {
                try
                {
                    return ReadEventsFromFile(_filePath);
                }
                catch (JsonException)
                {
                    // Invalid JSON: try the backup.
                }
                catch (InvalidDataException)
                {
                    // Invalid agenda structure: try the backup.
                }
            }

            // Do not replace damaged data with an empty list.
            if (!File.Exists(backupPath))
            {
                throw new InvalidDataException(
                    "Wobbi couldn't read agenda.json, " +
                    "and no backup is available.");
            }

            List<AgendaEvent> recoveredEvents;

            try
            {
                recoveredEvents = ReadEventsFromFile(backupPath);
            }
            catch (Exception ex) when (
                ex is JsonException or InvalidDataException)
            {
                throw new InvalidDataException(
                    "Both the agenda and its backup are invalid. " +
                    "Your files have been left untouched.",
                    ex);
            }

            // Preserve the damaged original for investigation.
            if (File.Exists(_filePath))
            {
                string damagedPath = _filePath +
                    ".corrupt-" +
                    DateTime.UtcNow.ToString("yyyyMMdd-HHmmssfff") +
                    "-" + Guid.NewGuid().ToString("N")[..8];

                File.Move(_filePath, damagedPath);

                DamagedFilePath = damagedPath;
            }

            // Restore the valid backup as the primary file.
            File.Copy(backupPath, _filePath);

            RecoveredFromBackup = true;

            return recoveredEvents;
        }

        private static List<AgendaEvent> ReadEventsFromFile(
            string path)
        {
            string json = File.ReadAllText(path);

            var events =
                JsonSerializer.Deserialize<List<AgendaEvent>>(json);

            if (events == null || events.Any(e => e is null))
            {
                throw new InvalidDataException(
                    "The agenda file contains invalid event data.");
            }

            return events;
        }


        // Save events to local JSON storage.

        private void SaveEvents()
        {
            string json = JsonSerializer.Serialize(
                _events,
                JsonOptions);

            string tempPath = Path.Combine(
                Path.GetDirectoryName(_filePath)!,
                $"agenda.{Guid.NewGuid():N}.tmp"
            );

            string backupPath = _filePath + ".bak";

            try
            {
                // First write to a temporary file.
                File.WriteAllText(tempPath, json);

                if (File.Exists(_filePath))
                {
                    // Replace the original and keep its
                    // previous version as a backup.
                    File.Replace(
                        tempPath,
                        _filePath,
                        backupPath);
                }
                else
                {
                    // First-ever save: no original exists.
                    File.Move(tempPath, _filePath);
                }
            }
            finally
            {
                // Remove leftover temporary files.
                if (File.Exists(tempPath))
                    File.Delete(tempPath);
            }
        }



        // Check if Wobbi has any unread agenda reminders.
        public bool HasPendingReminders()
        {
            return _events.Any(e => e.ReminderPending);
        }

        public IReadOnlyList<AgendaEvent> GetPendingReminders()
        {
            return _events
                .Where(e => e.ReminderPending)
                .OrderBy(e => e.StartsAt)
                .ToList();
        }

        // Dismiss one specific reminder.
        public bool DismissReminder(Guid id)
        {
            var agendaEvent = _events
                .FirstOrDefault(e => e.Id == id);

            if (agendaEvent == null || !agendaEvent.ReminderPending)
                return false;

            agendaEvent.ReminderPending = false;

            try
            {
                SaveEvents();
                return true;
            }
            catch
            {
                // Saving failed, so keep the reminder unread.
                agendaEvent.ReminderPending = true;
                throw;
            }
        }


        // Acknowledge all unread reminders.

        public void AcknowledgePendingReminders()
        {
            var pending = _events
                .Where(e => e.ReminderPending)
                .ToList();

            if (pending.Count == 0)
                return;

            foreach (var agendaEvent in pending)
                agendaEvent.ReminderPending = false;

            try
            {
                SaveEvents();
            }
            catch
            {
                // Saving failed.
                // Keep the reminders unread.
                foreach (var agendaEvent in pending)
                    agendaEvent.ReminderPending = true;

                throw;
            }
        }


    }
}
