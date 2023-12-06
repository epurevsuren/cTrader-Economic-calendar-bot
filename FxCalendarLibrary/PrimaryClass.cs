using System;
using System.Collections.Generic;
using System.Linq;

namespace FxCalendarLibrary
{
    public class PrimaryClass
    {
        private Dictionary<string, string> countries;
        private Dictionary<string, string> currencies;
        private Dictionary<string, int> currencyPipSize;
        private Dictionary<string, int> eventTime;
        private Dictionary<int, double> tradingHoursStrength;

        private static string[] lowImpactTitles = { "Consumer", "Building Permits", "Trade Balance" };
        private static string[] highImpactTitles = { "CPI", "GDP", "PPI", "RPI" };

        private int eventCount;
        private int maximumTp;

        private LogWriter _log;
        private NewsRetriever _newsRetriever;

        public PrimaryClass(int _maximumTp, Dictionary<string, string> _countries, Dictionary<string, string> _currencies, Dictionary<string, int> _currencyPipSize, Dictionary<string, int> _eventTime, Dictionary<int, double> _tradingHoursStrength)
        {
            this.maximumTp = _maximumTp;
            this.countries = _countries;
            this.currencies = _currencies;
            this.currencyPipSize = _currencyPipSize;
            this.eventTime = _eventTime;
            this.tradingHoursStrength = _tradingHoursStrength;
        }

        public void setNewsRetriever(NewsRetriever nr)
        {
            this._newsRetriever = nr;
            this._log = nr._log;
            eventCount = 1;
        }

        public string urlBuilder(List<FxCalendar> value)
        {
            string getStr = "";
            HashSet<string> country = new HashSet<string>();
            HashSet<int> importance = new HashSet<int>();
            foreach (var cs in value)
            {
                country.Add(countries[cs.economy]);
                importance.Add(cs.impact);
            }

            var value1 = string.Join(",", country);
            _log.WriteLine("Countries:" + value1);

            var value2 = string.Join(",", importance);
            _log.WriteLine("Impacts:" + value2);

            getStr = "&countries=" + value1 + "&importance=" + value2;

            return getStr;
        }

        private double impactCalculator(string name, int impact, double power, int eventHour)
        {
            if (lowImpactTitles.Any(name.Contains))
            {
                if (Math.Abs(power / 8) < 1)
                {
                    if (power > 0)
                        power = 1;
                    else
                        power = -1;
                }
                else
                    power = power / 8;
            }            
            else if (highImpactTitles.Any(name.Contains))
                power = power * 3.14 * impact;
            else
                power = power * impact;

            int multiplier = 1;
            if (eventTime.Keys.Any(name.Contains))
            {
                foreach (var ml in eventTime)
                {
                    if (name.Contains(ml.Key))
                    {
                        if (ml.Key.Equals("-Year"))
                        {
                            int last = name.IndexOf("-Year");
                            string val = name.Substring(0, last);
                            _log.WriteLine(val);
                            if (val.Contains(" "))
                            {
                                last = val.LastIndexOf(" ");
                                val = val.Substring(last);
                            }

                            _log.WriteLine(val + "-Year");
                            multiplier = Int32.Parse(val) * ml.Value;
                        }
                        else
                            multiplier = ml.Value;
                        break;
                    }
                }
            }

            power = power * multiplier;

            power = power * tradingHoursStrength[eventHour];

            return power;
        }

        public List<OrderBook> eventManager(List<FxCalendar> value, string html)
        {

            Dictionary<string, List<double>> totalPower = new Dictionary<string, List<double>>();

            int counter = 0;

            foreach (var cs in value)
            {
                _log.WriteLine(eventCount++.ToString());
                _log.WriteLine(cs.data.ToString("HH:mm"));
                _log.WriteLine(cs.name);
                _log.WriteLine(cs.economy);
                _log.WriteLine(cs.impact.ToString());
                _log.WriteLine("Actual:" + cs.actual);
                _log.WriteLine("Forecast:" + cs.forecast);
                _log.WriteLine("Previous:" + cs.previous);

                string result = _newsRetriever.newsFeed(html, cs.data.ToString("HH:mm"), cs.economy, cs.name, cs.impact);
                _log.WriteLine(result);

                try
                {
                    int status = _newsRetriever.getStat(result);
                    double power = 0;
                    Boolean released = true;

                    int idx = result.IndexOf(">");
                    result = result.Substring(idx + 1);
                    _log.WriteLine(result);

                    if (result.Any(char.IsDigit))
                        power = _newsRetriever.newsManager(result, cs.forecast, cs.previous, status, cs.name, cs.economy, cs.data);
                    else
                    {
                        released = false;
                        _log.WriteLine("Not released!");
                    }

                    if (released)
                    {
                        power = impactCalculator(cs.name, cs.impact, power, cs.data.Hour);

                        List<double> tPower;

                        if (totalPower.TryGetValue(cs.economy, out tPower))
                            totalPower[cs.economy].Add(power);
                        else
                            totalPower.Add(cs.economy, new List<double> { power });

                        _log.WriteLine("Power: " + power);

                        counter++;
                    }

                }
                catch (Exception ex)
                {
                    _log.WriteLine(ex.Message);
                }


                _log.WriteLine("------------------------------------------");
            }

            if (value.Count == counter)
                return loadOrder(totalPower);
            else
                return null;
            
        }

        private List<OrderBook> loadOrder(Dictionary<string, List<double>> totalPower)
        {
            List<OrderBook> ob = new List<OrderBook>();

            foreach (var tp in totalPower)
            {
                double total = tp.Value.Sum();
                int count = totalPower[tp.Key].Count;

                _log.WriteLine("Total power on " + tp.Key + ":" + total);
                _log.WriteLine("Current currency total event count at the time: " + count);

                double averageTotal = Math.Round((total / count), MidpointRounding.AwayFromZero);
                double pipVal = Math.Abs(averageTotal);

                _log.WriteLine("Exact Pip:" + pipVal);

                List<OrderBook> temp = new List<OrderBook>();

                if (averageTotal > 0)
                    temp=buildOrder(tp.Key, true, pipVal);
                else if (averageTotal <= 0)
                    temp=buildOrder(tp.Key, false, pipVal);

                ob.AddRange(temp);
            }

            return ob;
        }

        private List<OrderBook> buildOrder(string currency, bool qubit, double dVal)
        {
            List<OrderBook> ob = new List<OrderBook>();

            try
            {
                string tags = currencies[currency];
                string[] symbols = tags.Split(',');
                foreach (var symbol in symbols)
                {
                    _log.Write($"<{symbol}> = ");

                    double pip = currencyPipSize[symbol];

                    if (dVal > maximumTp)
                        dVal = maximumTp;
                    double tempVal = dVal * pip;

                    int pipTP = _newsRetriever.fibonacciTP(tempVal);
                    _log.Write("[" + pipTP);

                    int pipSL = _newsRetriever.fibonacciSL(pipTP);
                    _log.Write(", " + pipSL + "] ");

                    bool orderTypeBuy = false;
                    if (symbol.StartsWith(currency))
                    {
                        if (qubit)
                        {
                            _log.WriteLine("Buy");
                            orderTypeBuy = true;
                        }
                        else
                        {
                            _log.WriteLine("Sell");
                            orderTypeBuy = false;
                        }
                            
                    }
                    else
                    {
                        if (!qubit)
                        {
                            _log.WriteLine("Buy");
                            orderTypeBuy = true;
                        }
                        else
                        {
                            _log.WriteLine("Sell");
                            orderTypeBuy = false;
                        }
                            
                    }

                    ob.Add(new OrderBook(symbol, pipTP, pipSL, orderTypeBuy));

                }
            }
            catch
            {
                _log.WriteLine("No Symbol!");
            }

            return ob;
        }



    }
}
