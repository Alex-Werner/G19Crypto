using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using Newtonsoft.Json;

namespace G19Crypto
{
    class MarketParcer
    {
        private const string USER_AGENT = "Mozilla/5.0 (Windows NT 6.3; WOW64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/41.0.2272.118 Safari/537.36";
        private const string ACCEPT = "text/html,application/xhtml+xml,application/xml;q=0.9,image/webp,*/*;q=0.8";
        private const string CONTENT_TYPE = "application/json; charset=utf-8;";

        private int RetryAttempts { get; set; }
        private int RetryTimeMillis { get; set; }
        private bool state = false;

        private List<List<String>> markets= new List<List<String>>();



        public MarketParcer()
        {
            this.RetryAttempts = 3;
            this.RetryTimeMillis = 1000;
        }

        public void Parse()
        {
            this.state = false;
            int num = RetryAttempts;
            while (!(this.state || num == 0))
            {
                this.state = this.getMarketInfo();
                if (!this.state)
                {
                    Thread.Sleep(this.RetryTimeMillis);
                }
                num--;
            }
        }
        // Returns JSON string
        string GET(string url)
        {
            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);
            try
            {
                WebResponse response = request.GetResponse();
                using (Stream responseStream = response.GetResponseStream())
                {
                    StreamReader reader = new StreamReader(responseStream, Encoding.UTF8);
                    return reader.ReadToEnd();
                }
            }
            catch (WebException ex)
            {
                WebResponse errorResponse = ex.Response;
                using (Stream responseStream = errorResponse.GetResponseStream())
                {
                    StreamReader reader = new StreamReader(responseStream, Encoding.GetEncoding("utf-8"));
                    String errorText = reader.ReadToEnd();
                    // log errorText
                }
                throw;
            }
        }
        private bool getMarketInfo(){
             try
            {
                var marketAPIname = new string[]{"bitcoin","litecoin"};
                var marketAPIurl = new string[]{"https://www.bitstamp.net/api/ticker/","https://api.bitfinex.com/v1//pubticker/LTCUSD"};
                //var type = new  Type[] { TickBitstamp, TickBitfinex };

                var i = 0;

                foreach (var marketName in marketAPIname)
                {

                    string last ="";
                    if(marketName=="bitcoin")
                        last = JsonConvert.DeserializeObject<TickBitstamp>(GET(marketAPIurl[i])).last;
                    if(marketName=="litecoin")
                        last = JsonConvert.DeserializeObject<TickBitfinex>(GET(marketAPIurl[i])).last_price;

                    MarketInfo _market = new MarketInfo(marketName, last);



                    this.markets.Add(new List<String>());
                    this.markets[i].Add(_market.name.ToString()); 
                    this.markets[i].Add(_market.last.ToString());
                    i++;

                }
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }
       

        private static string GetStringBetween(string input, string startString, string endString, int nth = 1)
        {
            int startIndex = IndexOfNth(input, startString, nth) + startString.Length;
            int endIndex = input.IndexOf(endString, startIndex);
            return input.Substring(startIndex, endIndex - startIndex).Trim();
        }

        private static int IndexOfNth(string input, string match, int nth)
        {
            int i = 1;
            int index = 0;

            while (i <= nth && (index = input.IndexOf(match, index + 1)) != -1)
            {
                if (i == nth)
                    return index;

                i++;
            }
            return -1;
        }

       

        public bool Success
        {
            get { return this.state; }
        
        }

        public List<List<string>> getData()
        {
            return this.markets;
        }
    }

    public class MarketInfo
    {
        public string name;
        public string last;
        public MarketInfo(string _mn,string _ml){
            this.name = _mn;
            this.last = _ml;
        }

    }
    public class TickBitstamp
    {
        public string high { get; set; }
        public string last { get; set; }
        public string bid { get; set; }
        public string volume { get; set; }
        public string low { get; set; }
        public string ask { get; set; }
        public string timestamp { get; set; }

        public override string ToString()
        {
            try
            {
                return String.Format("High: {0}, Last {1}, Low {2}, Ask {3},Bid {4}, Volume {5}, Time {6}",
                    high, last, low, ask, bid, volume, UnixTimestampToDateTime(long.Parse(timestamp)));
            }
            catch
            {
                return "Tick:";
            }
        }
        public static DateTime UnixTimestampToDateTime(long _UnixTimeStamp)
        {
            return (new DateTime(1970, 1, 1, 0, 0, 0)).AddSeconds(_UnixTimeStamp).ToLocalTime();
        }
    }
    public class TickBitfinex
    {
        public string high { get; set; }
        public string mid { get; set; }
        public string last_price { get; set; }
        public string bid { get; set; }
        public string volume { get; set; }
        public string low { get; set; }
        public string ask { get; set; }
        public string timestamp { get; set; }

        public override string ToString()
        {
            try
            {
                return String.Format("High: {0}, Last {1}, Low {2}, Ask {3},Bid {4}, Volume {5}, Time {6}",
                    high, last_price, low, ask, bid, volume, UnixTimestampToDateTime(long.Parse(timestamp)));
            }
            catch
            {
                return "Tick:";
            }
        }
        public static DateTime UnixTimestampToDateTime(long _UnixTimeStamp)
        {
            return (new DateTime(1970, 1, 1, 0, 0, 0)).AddSeconds(_UnixTimeStamp).ToLocalTime();
        }
    }
}

