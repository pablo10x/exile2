namespace FewClicksDev.Core
{
    using System;
    using UnityEngine;

    [Serializable]
    public class SerializableDateTime : IComparable<SerializableDateTime>
    {
        [SerializeField] private long ticks = 0;

        private bool initialized = false;
        private DateTime dateTime = default;

        public DateTime DateTime
        {
            get
            {
                if (initialized == false)
                {
                    dateTime = new DateTime(ticks);
                    initialized = true;
                }

                return dateTime;
            }
        }

        public SerializableDateTime() : this(DateTime.Now) { }

        public SerializableDateTime(DateTime _dateTime)
        {
            ticks = _dateTime.Ticks;
            dateTime = _dateTime;

            initialized = true;
        }

        public int CompareTo(SerializableDateTime _other)
        {
            if (_other == null)
            {
                return 1;
            }

            return ticks.CompareTo(_other.ticks);
        }
    }
}
