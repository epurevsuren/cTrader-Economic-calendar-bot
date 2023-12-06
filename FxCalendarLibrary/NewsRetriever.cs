using System;
using System.IO;
using System.Linq;
using System.Net;

namespace FxCalendarLibrary
{
    public class NewsRetriever
    {

        private string todayStr;
        private static int[] fibonacciSequence = { 1, 1, 2, 3, 5, 8, 13, 21, 34, 55, 89, 144, 233, 377, 610, 987, 1597, 2584, 4181, 6765, 10946, 17711, 28657, 46368, 75025, 121393, 196418, 317811 };
        private static string[] reverseEvent = { "Jobless", "Unemployment", "Auction", "Imports" };
        public LogWriter _log;

        public NewsRetriever(string str)
        {
            todayStr = str;
            ServicePointManager.SecurityProtocol = (SecurityProtocolType)3072;
            _log = new LogWriter(todayStr);

        }

        #region HttpWebRequest
        public string htmlWork(string current_url)
        {
            try
            {
                string html = string.Empty;
                HttpWebRequest request = (HttpWebRequest)WebRequest.Create(current_url);
                request.ContentType = "text/html; charset=utf-8";
                request.UserAgent = "Mozilla/5.0 (Windows NT 6.1; WOW64) AppleWebKit/535.2 (KHTML, like Gecko)" +
                                                                            " Chrome/15.0.874.121 Safari/535.2";
                request.AutomaticDecompression = DecompressionMethods.GZip;

                using (HttpWebResponse response = (HttpWebResponse)request.GetResponse())
                using (Stream stream = response.GetResponseStream())
                using (StreamReader reader = new StreamReader(stream))
                {
                    html = reader.ReadToEnd();
                }

                return html;
            }
            catch (Exception ex)
            {
                return ex.Message;
            }

        }

        public int getStat(string result)
        {
            int status = 0;

            if (result.Contains("greenFont"))
            {
                status = 1;
                _log.WriteLine("Better Than Expected");
            }
            else if (result.Contains("redFont"))
            {
                status = 2;
                _log.WriteLine("Worse Than Expected");
            }

            return status;
        }

        public string newsFeed(string html, string timeStr, string currency, string title, int impact)
        {
            try
            {
                int idx = html.IndexOf(todayStr, StringComparison.InvariantCultureIgnoreCase);
                html = html.Substring(idx);

                idx = html.IndexOf(timeStr, StringComparison.InvariantCultureIgnoreCase);

                html = html.Substring(idx);

                idx = html.IndexOf(currency, StringComparison.InvariantCultureIgnoreCase);
                html = html.Substring(idx);

                idx = html.IndexOf("<td class=\"left event\">" + title + "</td>", StringComparison.InvariantCultureIgnoreCase);

                try
                {
                    html = html.Substring(idx);
                }
                catch
                {
                    html = html.Replace("amp;", "");
                    idx = html.IndexOf("<td class=\"left event\">" + title, StringComparison.InvariantCultureIgnoreCase);
                    html = html.Substring(idx);
                }


                String actualTd = "<td class=\"bold act";
                idx = html.IndexOf(actualTd, StringComparison.InvariantCultureIgnoreCase);
                html = html.Substring(idx);

                int last = html.IndexOf("</td>");

                html = html.Substring(0, last);

                return html;
            }
            catch (Exception ex)
            {
                return ex.Message;
            }

        }
        #endregion

        private double monetarySuffix(string str)
        {
            char[] monetarySuffixChars = { '%', 'K', 'M', 'B', 'T', ' ' };
            str = str.TrimEnd(monetarySuffixChars);
            str = str.Replace("&nbsp;", "");
            str = str.Replace(",", "");
            double value = Double.Parse(str);
            return value;
        }

        private bool highLowCalculator(double valueA, double valueB)
        {
            bool qubit = true;
            if (valueA > valueB)
                _log.WriteLine("Higher is better");
            else
            {
                _log.WriteLine("Lower is better");
                qubit = false;
            }
            return qubit;
        }

        private double MAPE(double actual, double previous)
        {
            //Mean Absolute Percent Error (MAPE)
            double change = ((actual - previous) / Math.Abs(previous)) * 100;
            return Math.Round(change, 2);
        }

        private double volumeWeightedForecastError(double actual, double forecast, double previous)
        {
            double change = (forecast*MAPE(actual,forecast) + previous*MAPE(actual,previous))/(forecast + previous);
            return Math.Round(change, 2);
        }

        private double percentageCalculator(string prev, double actual, double forecast, double previous, bool higherIsBetter)
        {
            double percent = 0;

            if (prev.Contains("%"))
            {
                percent = ((actual - forecast) + (actual - previous))/2;
            }              
            else
                percent = volumeWeightedForecastError(actual, forecast, previous);

            if (!higherIsBetter)
                percent = percent * (-1);

            if (actual > forecast)
            {
                if (higherIsBetter)
                {
                    _log.WriteLine("better");
                }
                else
                {
                    _log.WriteLine("worse");
                }

            }
            else if (actual == forecast)
            {
                _log.WriteLine("equal");
            }
            else
            {
                if (higherIsBetter)
                {
                    _log.WriteLine("worse");
                }
                else
                {
                    _log.WriteLine("better");
                }

            }

            return percent;
        }

        private double percentageCalculator(string prev, double actual, double previous, bool higherIsBetter)
        {
            double percent = 0;

            if (prev.Contains("%"))
            {
                percent = actual - previous;
            }        
            else
                percent = MAPE(actual,previous);

            if (!higherIsBetter)
                percent = percent * (-1);

            if (actual > previous)
            {
                if (higherIsBetter)
                {
                    _log.WriteLine("better");
                }
                else
                {
                    _log.WriteLine("worse");
                }

            }
            else if (actual == previous)
            {
                _log.WriteLine("equal");
            }
            else
            {
                if (higherIsBetter)
                {
                    _log.WriteLine("worse");
                }
                else
                {
                    _log.WriteLine("better");
                }

            }

            return percent;
        }

        public double newsManager(string act, string fore, string prev, int status, string title, string currency, DateTimeOffset date)
        {
            double actual = monetarySuffix(act);
            double previous = monetarySuffix(prev);
            double forecast = 0;
            double percent = 0;
            bool higherIsBetter = true;
            bool haveForecast = false;

            if (fore.Any(char.IsDigit))
            {
                forecast = monetarySuffix(fore);
                haveForecast = true;
            }
                

            if (reverseEvent.Any(title.Contains))
            {
                higherIsBetter = false;
                _log.WriteLine("Lower is better");
            }
            else
            {
                switch (status)
                {
                    case 1:
                        higherIsBetter = highLowCalculator(actual, forecast);
                        break;
                    case 2:
                        higherIsBetter = highLowCalculator(forecast, actual);
                        break;
                    default: break;
                }
            }



            if ((title.Contains("QoQ") || title.Contains(getFinancialQuarter(date))) && isNewQuarter(date))
                previous = 0;

            if (haveForecast)
                percent = percentageCalculator(prev, actual, forecast, previous, higherIsBetter);
            else
                percent = percentageCalculator(prev, actual, previous, higherIsBetter);

            _log.WriteLine(actual + "__" + previous);

            _log.WriteLine("Percentage: " + percent);

            return percent;
        }

        private string getFinancialQuarter(DateTimeOffset date)
        {
            int quarter = (date.AddMonths(-3).Month + 2) / 3;
            return string.Format("(Q{0})", quarter);
        }

        private bool isNewQuarter(DateTimeOffset date)
        {
            bool chk = false;

            if (date.Month % 3 == 1)
                chk = true;

            return chk;
        }

        public int fibonacciTP(double dVal)
        {
            int fiboValue = 1;
            foreach (int sVal in fibonacciSequence)
            {
                if (sVal >= dVal)
                {
                    fiboValue = sVal;
                    break;
                }

            }
            return fiboValue;
        }

        public int fibonacciSL(int tp)
        {
            int sl = 1;
            if (tp > 1)
                for (int i = 2; i < fibonacciSequence.Length; i++)
                {
                    if (fibonacciSequence[i] == tp)
                    {
                        sl = fibonacciSequence[i - 2];
                        break;
                    }

                }

            return sl;
        }

    }
}
