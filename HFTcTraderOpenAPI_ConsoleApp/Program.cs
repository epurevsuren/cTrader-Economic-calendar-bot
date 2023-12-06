using System;
using System.Collections.Generic;
using System.Threading;
using Newtonsoft.Json;
using System.IO;

namespace HFTcTraderOpenAPI_ConsoleApp
{
    class Program
    {
        private static Dictionary<string, int> currencyFixNumber = new Dictionary<string, int>();
        private static Dictionary<string, string> positionNumbers = new Dictionary<string, string>();

        private static string jsonConfigLoc = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments)+"\\jsonConfig\\";

        static void Main(string[] args)
        {
            currencyFixNumber = JsonConvert.DeserializeObject<Dictionary<string, int>>
                                 (File.ReadAllText(jsonConfigLoc + "currencyFixNumber.json"));

            FixApi _fixApi = new FixApi();

            _fixApi.login();

            int count=1;
            foreach (var cfn in currencyFixNumber)
            {
                string result= _fixApi.newMarketOrder(cfn.Value, cfn.Key, true, 1000);
                Console.WriteLine(count+"   "+result);
                try
                {
                    int first = result.IndexOf("721=");
                    result = result.Substring(first+4);

                    int second = result.IndexOf("|");
                    string posID = result.Substring(0, second);
                    Console.WriteLine(posID);

                    positionNumbers.Add(posID, cfn.Key);

                    

                }
                catch(Exception ex)
                {
                }
                

                
                Console.WriteLine("-------------------------------------------------------");
                count++;
            }

            Console.WriteLine("Thread sleeping for 3sec!");
            Thread.Sleep(3000);

            foreach (var pn in positionNumbers)
            {
                string orderStatus = _fixApi.orderStatus(currencyFixNumber[pn.Value], pn.Value);
                Console.WriteLine("orderStatus=" + currencyFixNumber[pn.Value] + pn.Value + "   " + orderStatus);

                //Thread.Sleep(100);

                string requestForPositions = _fixApi.requestForPosition(currencyFixNumber[pn.Value], pn.Value, pn.Key);
                Console.WriteLine("requestForPositions=" + pn.Key + pn.Value + "   " + requestForPositions);

                  
            }

            Console.WriteLine("Done!");
            Console.ReadLine();
        }
    }
}
