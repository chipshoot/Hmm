using System;
using System.Collections.Generic;

namespace Hmm.Utility.Misc
{
    /// <summary>
    /// The interface is use for wrap date time
    /// in appropriate injectable interface
    /// </summary>
    public interface IDateTimeProvider
    {
        DateTime UtcNow { get; }

        DateTime AddBusinessDays(DateTime startUtcDate, int days, ICollection<string> holidays = null);

        ///  <summary>
        ///  Finds the next date whose day of the week equals the specified day of the week.
        ///  </summary>
        ///  <param name="startUtcDate">
        /// 		The date to begin the search.
        ///  </param>
        ///  <param name="desiredDay">
        /// 		The desired day of the week whose date will be returned.
        ///  </param>
        /// <param name="numberToSkip">
        ///         The number to control how many week day to skip, e.g. if we set number to
        ///         skip to 1 then we will get Friday of following week not this week
        /// </param>
        /// <returns>
        /// 		The returned date occurs on the given date's week.
        /// 		If the given day occurs before given date, the date for the
        /// 		following week's desired day is returned.
        ///  </returns>
        DateTime GetNextDateForDay(DateTime startUtcDate, DayOfWeek desiredDay, int numberToSkip = 0);

        /// <summary>
        /// Gets the current week day based on startDate, e.g. say current date is July 10, 2015, then
        /// we can you this method to get current week's Monday is July 8, 2015
        /// </summary>
        /// <param name="startDate">The start date.</param>
        /// <param name="desiredDay">The desired week day.</param>
        /// <returns>DateTime.</returns>
        DateTime GetCurrentWeekDateForDay(DateTime startDate, DayOfWeek desiredDay);
    }
}