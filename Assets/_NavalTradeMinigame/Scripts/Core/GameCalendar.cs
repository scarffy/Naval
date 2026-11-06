using System;

namespace Naval.Core
{
    /// <summary>Day index to date, and the strength of the hurricane season on any date.</summary>
    public sealed class GameCalendar
    {
        readonly Rules rules;

        public GameCalendar(Rules rules)
        {
            this.rules = rules;
        }

        /// <summary>Day 0 is the mission's start date.</summary>
        public DateTime DateOf(int day)
        {
            return rules.StartDate.AddDays(day);
        }

        /// <summary>
        /// 0 outside the season, rising in a straight line from the season start to the peak start,
        /// 1 from the peak start to the peak end, falling in a straight line to 0 at the season end.
        /// The same every year.
        /// </summary>
        public double Season(DateTime date)
        {
            date = date.Date;
            int year = date.Year;
            DateTime start = rules.SeasonStart.In(year), peakStart = rules.PeakStart.In(year);
            DateTime peakEnd = rules.PeakEnd.In(year), end = rules.SeasonEnd.In(year);
            if (date < start || date >= end) return 0;
            if (date < peakStart) return (date - start).TotalDays / (peakStart - start).TotalDays;
            if (date <= peakEnd) return 1;
            return (end - date).TotalDays / (end - peakEnd).TotalDays;
        }
    }
}
