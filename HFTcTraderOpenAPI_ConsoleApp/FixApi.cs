using System;
using System.Net;
using System.Net.Security;
using System.Net.Sockets;
using System.Security.Cryptography.X509Certificates;
using System.Text;

namespace HFTcTraderOpenAPI_ConsoleApp
{
    class FixApi
    {
        private int _pricePort = 5211;
        private int _tradePort = 5212;

        private string _host = "h51.p.ctrader.com";
        private string _username = "3630031";
        private string _password = "P@ssW0rd";
        private string _senderCompID = "demo.icmarkets.3630031";
        private string _senderSubID = "TRADE";

        private string _targetCompID = "CSERVER";

        private int _messageSequenceNumber = 1;
        private TcpClient _priceClient;
        private SslStream _priceStreamSSL;
        private TcpClient _tradeClient;
        private SslStream _tradeStreamSSL;
        private MessageConstructor _messageConstructor;

        public FixApi()
        {
            init();
            _priceClient = new TcpClient(_host, _pricePort);
            _priceStreamSSL = new SslStream(_priceClient.GetStream(), false,
                        new RemoteCertificateValidationCallback(ValidateServerCertificate), null);
            _priceStreamSSL.AuthenticateAsClient(_host);
            _tradeClient = new TcpClient(_host, _tradePort);
            _tradeStreamSSL = new SslStream(_tradeClient.GetStream(), false,
                        new RemoteCertificateValidationCallback(ValidateServerCertificate), null);
            _tradeStreamSSL.AuthenticateAsClient(_host);
            _messageConstructor = new MessageConstructor(_host, _username,
                _password, _senderCompID, _senderSubID, _targetCompID);
        }

        private bool ValidateServerCertificate(object sender, X509Certificate certificate, X509Chain chain, SslPolicyErrors sslPolicyErrors)
        {
            if (sslPolicyErrors == SslPolicyErrors.None)
                return true;
            Console.WriteLine("Certificate error: {0}", sslPolicyErrors);
            return false;
        }

        private string SendTradeMessage(string message, bool readResponse = true)
        {
            return SendMessage(message, _tradeStreamSSL, readResponse);
        }

        private string SendMessage(string message, SslStream stream, bool readResponse = true)
        {
            var byteArray = Encoding.ASCII.GetBytes(message);
            stream.Write(byteArray, 0, byteArray.Length);
            var buffer = new byte[1024];
            if (readResponse)
            {
                //Thread.Sleep(100);
                stream.Read(buffer, 0, 1024);
            }
            _messageSequenceNumber++;
            var returnMessage = Encoding.ASCII.GetString(buffer);
            return returnMessage;
        }

        public string login()
        {
            var message = _messageConstructor.LogonMessage(MessageConstructor.SessionQualifier.TRADE,
                _messageSequenceNumber, 30, false);
            return SendTradeMessage(message);
        }

        public string newMarketOrder(int fixID, string symbolName, bool side, int orderQuantity)
        {
            int orderSide = 2;
            if (side)
                orderSide = 1;

            var message = _messageConstructor.NewOrderSingleMessage(MessageConstructor.SessionQualifier.TRADE,
                _messageSequenceNumber, symbolName+fixID, fixID, orderSide, DateTime.UtcNow.ToString("yyyyMMdd-HH:mm:ss"), orderQuantity, 1, "3");
            return SendTradeMessage(message).Replace("\u0001", "|");
        }

        public string orderStatus(int fixID, string symbolName)
        {
            var message = _messageConstructor.OrderStatusRequest(MessageConstructor.SessionQualifier.TRADE,
                _messageSequenceNumber, symbolName+fixID);
            return SendTradeMessage(message).Replace("\u0001", "|");
        }

        public string executionReport(int fixID, string symbolName, string posID)
        {
            var message = _messageConstructor.ExecutionReport(MessageConstructor.SessionQualifier.TRADE,
                _messageSequenceNumber, symbolName + fixID, "2", DateTime.UtcNow.ToString("yyyyMMdd-HH:mm:ss"));
            return SendTradeMessage(message).Replace("\u0001", "|");
        }

        public string requestForPosition(int fixID, string symbolName, string posID)
        {
            var message = _messageConstructor.PositionReport(MessageConstructor.SessionQualifier.TRADE,
                _messageSequenceNumber, symbolName+fixID, "1", "0", posID, symbolName, "1", "0", "0");
            return SendTradeMessage(message).Replace("\u0001", "|");
        }

        public string requestForPositions()
        {
            var message = _messageConstructor.RequestForPositions(MessageConstructor.SessionQualifier.TRADE,
                _messageSequenceNumber, "AllTheFuckersComeTogether");
            return SendTradeMessage(message).Replace("\u0001", "\n");
        }

        private void init()
        {
            ServicePointManager.SecurityProtocol = (SecurityProtocolType)3072;
        }
    }
}
