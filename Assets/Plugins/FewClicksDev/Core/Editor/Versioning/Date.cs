namespace FewClicksDev.Core.Versioning
{
    using System;
    using UnityEngine;

    [Serializable]
    public class Date : IComparable<Date>
    {
        public enum MonthName
        {
            January = 1,
            February = 2,
            March = 3,
            April = 4,
            May = 5,
            June = 6,
            July = 7,
            August = 8,
            September = 9,
            October = 10,
            November = 11,
            December = 12
        }

        public const int SECONDS_IN_MINUTE = 60;
        public const int SECONDS_IN_HOUR = SECONDS_IN_MINUTE * 60;
        public const int SECONDS_IN_DAY = SECONDS_IN_HOUR * 24;

        [SerializeField] private int day = 0;
        [SerializeField] private int month = 0;
        [SerializeField] private int year = 0;

        [SerializeField] private int hour = 0;
        [SerializeField] private int minutes = 0;
        [SerializeField] private int seconds = 0;

        public long NumberOfSeconds
        {
            get
            {
                long _seconds = (seconds + minutes * SECONDS_IN_MINUTE) + (hour * SECONDS_IN_HOUR) + (day * SECONDS_IN_DAY) + (month * 30 * SECONDS_IN_DAY) + ((year - 2000) * 12 * 30 * SECONDS_IN_DAY);

                return _seconds;
            }
        }

        public Date() : this(DateTime.Now) { }

        public Date(DateTime _date)
        {
            day = _date.Day;
            month = _date.Month;
            year = _date.Year;

            hour = _date.Hour;
            minutes = _date.Minute;
            seconds = _date.Second;
        }

        public int CompareTo(Date _other)
        {
            if (NumberOfSeconds < _other.NumberOfSeconds)
            {
                return -1;
            }

            if (NumberOfSeconds > _other.NumberOfSeconds)
            {
                return 1;
            }

            return 0;
        }

        public string ToStringWithMonthName()
        {
            return $"{day} {((MonthName) month).ToString()} {year}";
        }
    }
}
