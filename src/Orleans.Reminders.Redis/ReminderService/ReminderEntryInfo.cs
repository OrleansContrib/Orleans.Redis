using System;

namespace Orleans.Runtime.ReminderService
{
    /// <summary>
    /// Class ReminderEntryInfo.
    /// </summary>
    [Serializable]
    internal class ReminderEntryInfo
    {
        /// <summary>
        /// The grain reference of the grain that created the reminder. Forms the reminder
        /// primary key together with <see cref="ReminderName" />.
        /// </summary>
        /// <value>The grain reference.</value>
        public string GrainId { get; set; }

        /// <summary>
        /// The name of the reminder. Forms the reminder primary key together with
        /// <see cref="GrainId" />.
        /// </summary>
        /// <value>The name of the reminder.</value>
        public string ReminderName { get; set; }

        /// <summary>
        /// the time when the reminder was supposed to tick in the first time
        /// </summary>
        /// <value>The start at.</value>
        public DateTime StartAt { get; set; }

        /// <summary>
        /// the time period for the reminder
        /// </summary>
        /// <value>The period.</value>
        public TimeSpan Period { get; set; }

        /// <summary>
        /// Gets or sets the e tag.
        /// </summary>
        /// <value>The e tag.</value>
        public string ETag { get; set; }

        /// <summary>
        /// Converts to entry.
        /// </summary>
        /// <param name="grainReferenceConverter">The grain reference converter.</param>
        /// <returns>ReminderEntry.</returns>
        public ReminderEntry ToEntry(IGrainReferenceConverter grainReferenceConverter)
        {
            return new ReminderEntry
            {
                ETag = ETag,
                GrainRef = grainReferenceConverter.GetGrainFromKeyString(GrainId),
                Period = Period,
                ReminderName = ReminderName,
                StartAt = StartAt
            };
        }

        /// <summary>
        /// Froms the entry.
        /// </summary>
        /// <param name="reminderEntry">The reminder entry.</param>
        /// <returns>ReminderEntryInfo.</returns>
        public static ReminderEntryInfo FromEntry(ReminderEntry reminderEntry)
        {
            return new ReminderEntryInfo()
            {
                ETag = reminderEntry.ETag,
                GrainId = reminderEntry.GrainRef.ToKeyString(),
                Period = reminderEntry.Period,
                ReminderName = reminderEntry.ReminderName,
                StartAt = reminderEntry.StartAt
            };
        }
    }
}