using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using WeaveBase;

namespace WMQ
{
    public class WMQData
    { 
        public String id;
        public String to;
        public String message;
        [JsonIgnore]
        public DateTime ctime;
        //毫秒
        public int Validityperiod;
        public string form;
        public DataConfirm DC = DataConfirm.Send;
    }
    public enum  DataConfirm { Send , Wait }
    public class RegData
    {
        public string to;
        public String type;
        [JsonIgnore]
        public Socket soc;
        public string from;

    }
    public class WMQMODE
    {
        public IWeaveTcpBase iwtb;
        public int count = 0;
        public minForm mf;
    }


    //点对点
    public class WMQueuesoc
    {
        public string token;
        public string fromtoken;
        public Socket soc;

    }
    //订阅
    public class WMQTOPIC
    {
        public List<WMQData> wdata = new List<WMQData>();
        public string topic;
        public List<Socketway> ALLsoc = new List<Socketway>();


    }
    public class Socketway
    {
        public Socket soc;
        public string from;
    }
}
