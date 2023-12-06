using System;

namespace FxCalendarLibrary
{
    public class FxCalendar
    {
        public string economy { get; set; }
        public int impact { get; set; }
        public DateTimeOffset data { get; set; }
        public string name { get; set; }
        public string actual { get; set; }
        public string forecast { get; set; }
        public string previous { get; set; }
    }
}
