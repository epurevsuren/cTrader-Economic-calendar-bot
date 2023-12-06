using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using FxCalendarLibrary;
using Newtonsoft.Json;

namespace Console_economic_calendar
{
    class Program
    {
        #region HashMaps
        private static Dictionary<string, string> countries = new Dictionary<string, string>();
        private static Dictionary<string, string> currencies = new Dictionary<string, string>();
        private static Dictionary<string, int> currencyPipSize = new Dictionary<string, int>();
        private static Dictionary<string, int> eventTime = new Dictionary<string, int>();
        private static Dictionary<int, double> tradingHoursStrength = new Dictionary<int, double>();
        private static Dictionary<DateTimeOffset, List<FxCalendar>> eventHash = new Dictionary<DateTimeOffset, List<FxCalendar>>();
        #endregion

        private static string jsonConfigLoc = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments) + "\\jsonConfig\\";
        private static string[] skippedEvent = { "Oil", "Fuel", "Gas", "EIA", "Survey" };

        private static int maximumTp = 13;
        
        private static string url = "https://sslecal2.forexprostools.com/?calType=day";
        private static string jsonUrl = "http://localhost/Investing.com-economic-calendar-JSON-Parser/";

        private static List<FxCalendar> calendars;
        private static PrimaryClass _primaryClass;
        private static NewsRetriever _newsRetriever;
        private static LogWriter _log;


        private static void init() {

            ServicePointManager.SecurityProtocol = (SecurityProtocolType)3072;

            countries = JsonConvert.DeserializeObject<Dictionary<string, string>>
                                 (File.ReadAllText(jsonConfigLoc + "countries.json"));

            currencies = JsonConvert.DeserializeObject<Dictionary<string, string>>
                                 (File.ReadAllText(jsonConfigLoc + "currencies.json"));

            currencyPipSize = JsonConvert.DeserializeObject<Dictionary<string, int>>
                                 (File.ReadAllText(jsonConfigLoc + "currencyPipSize.json"));

            tradingHoursStrength = JsonConvert.DeserializeObject<Dictionary<int, double>>
                                 (File.ReadAllText(jsonConfigLoc + "tradingHoursStrength.json"));

            eventTime = JsonConvert.DeserializeObject<Dictionary<string, int>>
                                 (File.ReadAllText(jsonConfigLoc + "eventTime.json"));

            //string json = JsonConvert.SerializeObject(eventTime);
            //File.WriteAllText("eventTime.json", json);

            var jsonString = new WebClient().DownloadString(jsonUrl);
            calendars = JsonConvert.DeserializeObject<List<FxCalendar>>(jsonString);
        }
        

        private static void eventsOfToday() {
            DateTime easternTime = TimeZoneInfo.ConvertTime(DateTime.Now,
                 TimeZoneInfo.FindSystemTimeZoneById("Eastern Standard Time"));

            string todayStr = easternTime.ToString("dddd, MMMM d, yyyy");

            _newsRetriever = new NewsRetriever(todayStr);
            _log = _newsRetriever._log;
            _log.WriteLine(todayStr);

            int today = easternTime.Day;
            _log.WriteLine(easternTime.ToString());


            foreach (var cs in calendars)
            {
                if (cs.data.Day == today && cs.forecast != null && cs.forecast != ""
                    && cs.previous != null && cs.previous != ""  && !skippedEvent.Any(cs.name.Contains))
                {
                    List<FxCalendar> temp;
                    if (!eventHash.TryGetValue(cs.data, out temp))
                    {
                        temp = new List<FxCalendar>();
                        eventHash.Add(cs.data, temp);
                    }
                    temp.Add(cs);
                }
            }
        }

        

        public static void run()
        {
            string current_url = url;

            foreach (var eh in eventHash)
            {
                _log.WriteLine(eh.Key.ToString());
                int eventCount = eh.Value.Count;
                _log.WriteLine("Event count at " + eh.Key.ToString("HH:mm") + " : " + eventCount);

                current_url += _primaryClass.urlBuilder(eh.Value);
                string html = _newsRetriever.htmlWork(current_url);

                makeOrder(_primaryClass.eventManager(eh.Value, html));

                _log.WriteLine("~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~");
            }

            _log.WriteLine("Done!");
        }

        public static void makeOrder(List<OrderBook> ob) {
            if(ob!=null)
            foreach (var e in ob)
            {
                Console.WriteLine(e.getSymbol() + " " + e.getTakeProfit() + " " + e.getStopLoss() + " " + e.getOrderType());
            }
        }

        static void Main(string[] args)
        {
            try
            {
                init();

                _primaryClass = new PrimaryClass(maximumTp, countries, currencies, currencyPipSize, eventTime, tradingHoursStrength);

                eventsOfToday();

                _primaryClass.setNewsRetriever(_newsRetriever);

                run();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
            
            Console.ReadLine();
        }
    }
}
