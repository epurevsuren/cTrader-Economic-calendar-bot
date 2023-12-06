using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace FxCalendarLibrary
{
    public class OrderBook
    {
        private string symbol;
        private int takeProfit;
        private int stopLoss;
        private bool qubit;

        public OrderBook(string _symbol, int _takeProfit, int _stopLoss, bool _qubit)
        {
            this.symbol = _symbol;
            this.takeProfit = _takeProfit;
            this.stopLoss = _stopLoss;
            this.qubit = _qubit;
        }

        public string getSymbol()
        {
            return symbol;
        }

        public int getTakeProfit()
        {
            return takeProfit;
        }

        public int getStopLoss()
        {
            return stopLoss;
        }

        public bool getOrderType()
        {
            return qubit;
        }
    }
}
