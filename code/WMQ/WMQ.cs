using Newtonsoft.Json;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using WeaveBase;

namespace WMQ
{
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
        public Socket soc;

    }
    //订阅
    public class WMQTOPIC
    {
        public List<WMQData> wdata=new List<WMQData>();
        public string topic;
        public List<Socket> ALLsoc =new List<Socket>();
      

    }
    
    public class WMQ
    {
        Dictionary<String, WMQTOPIC> WMQTOPICList = new Dictionary<String, WMQTOPIC>();
        Dictionary<String, bool> WMQTOPICListbool = new Dictionary<String, bool>();
        LinkedList<WMQueuesoc> WMQueuesoclink = new LinkedList<WMQueuesoc>();
        LinkedList<WMQData> WMQDatalink = new LinkedList<WMQData>();
        List<WMQMODE> listiwtcp = new List<WMQMODE>();
        public WMQ(List<WMQMODE> _listiwtcp)
        {
            listiwtcp = _listiwtcp;
            System.Threading.Thread t = new System.Threading.Thread(new System.Threading.ThreadStart(topicgo));
            t.Start();
            System.Threading.Thread t1 = new System.Threading.Thread(new System.Threading.ThreadStart(Queuego));
            t1.Start();
        }
        public bool Send<T>(Socket soc, byte command, T t)
        {
            try
            {
                foreach (WMQMODE wm in listiwtcp)
                {
                    if (wm.iwtb.Port == ((System.Net.IPEndPoint)soc.LocalEndPoint).Port)
                    {
                        String str = Newtonsoft.Json.JsonConvert.SerializeObject(t);
                        return wm.iwtb.Send(soc, command, str);

                    }
                }
            }
            catch { deletesoc(soc); }
            return false;
        }
        void Queuego()
        {
            while (true)
            {
                try
                {
                    if (WMQDatalink.Count > 0)
                    {
                        WMQData wmq = WMQDatalink.First();
                        if (wmq != null)
                        {
                            int len = WMQueuesoclink.Count;
                            WMQueuesoc[] wmqsoc = new WMQueuesoc[len];
                            WMQueuesoclink.CopyTo(wmqsoc, 0);
                            bool isok = false;
                            foreach (WMQueuesoc wmqs in wmqsoc)
                            {
                                if (wmq.to == wmqs.token)
                                {
                                    isok = Send<WMQData>(wmqs.soc, 0x01, wmq);
                                    break;
                                }
                            }

                            if (wmq.ctime.AddMilliseconds(wmq.Validityperiod) > DateTime.Now && isok == false)
                            {
                                lock (this)
                                {
                                    WMQDatalink.AddLast(wmq);
                                }
                            }
                            WMQDatalink.RemoveFirst();
                        }
                    }
                }
                catch (Exception ){  };
                System.Threading.Thread.Sleep(10);
            }
        }
        void topicgo()
        {
            while (true)
            {

                try
                {

                    String[] keys = WMQTOPICList.Keys.ToArray();
                    foreach (string key in keys)
                    {
                        if (WMQTOPICList[key].wdata.Count > 0)
                        {
                            if (WMQTOPICListbool[key] == false)
                            {
                                WMQTOPICListbool[key] = true;
                                System.Threading.ThreadPool.QueueUserWorkItem(new System.Threading.WaitCallback(sendtopic), key);
                            }

                        }
                    }
                }
                catch { }
                    
                System.Threading.Thread.Sleep(10);
            }
        }

        void sendtopic(object obj)
        {
            string key = obj as string;
            try
            {

                List<WMQData> wdata = WMQTOPICList[key].wdata;
                int len = wdata.Count;
                WMQData[] WMQDatas = new WMQData[len];
                wdata.CopyTo(WMQDatas, 0);
                List<Socket> socs = WMQTOPICList[key].ALLsoc;
                len = socs.Count;
                Socket[] Sockets = new Socket[len];
                socs.CopyTo(Sockets, 0);
                foreach (WMQData wd in WMQDatas)
                {
                    if (wd != null)
                        foreach (Socket soc in Sockets)
                        {
                            Send<WMQData>(soc, 0x02, wd);
                        }
                    else
                    {

                    }
                    wdata.Remove(wd);
                }
            }
            catch(Exception e) { }
            WMQTOPICListbool[key] = false;
        }

        public bool EXEC(byte command, string data, System.Net.Sockets.Socket soc)
        {
            try
            {
                WMQData wmqd = new WMQData();
                switch (command)
                {
                    case 0:
                        RegData rd = Newtonsoft.Json.JsonConvert.DeserializeObject<RegData>(data);
                        if (rd.type == "topic")
                        {
                            rd.soc = soc;
                            addtopicsoc(rd);
                        }
                        else
                        {
                            WMQueuesoc wq = new WMQueuesoc();
                            wq.token = rd.to; wq.soc = soc;
                            WMQueuesoclink.AddLast(wq);
                        }
                        break;
                    case 1:
                        wmqd = Newtonsoft.Json.JsonConvert.DeserializeObject<WMQData>(data);
                        wmqd.ctime = DateTime.Now;
                        lock (this)
                        {
                            WMQDatalink.AddLast(wmqd);
                        }
                        break;
                    case 2:
                        wmqd = Newtonsoft.Json.JsonConvert.DeserializeObject<WMQData>(data);
                        wmqd.ctime = DateTime.Now;
                        if (wmqd == null)
                        {
                        }
                        addtopic(wmqd);
                        break;

                }
            }
            catch(Exception e) { return false; }
            return true;

        }
        public void deletesoc(Socket soc)
        {
            try
            {
                String[] keys = WMQTOPICList.Keys.ToArray();
                foreach (string key in keys)
                {
                    if (WMQTOPICList[key].ALLsoc.Count > 0)
                    {
                        try
                        {
                            WMQTOPICList[key].ALLsoc.Remove(soc);
                        }
                        catch { }
                    }
                }

                int len = WMQueuesoclink.Count;
                WMQueuesoc[] wmqsoc = new WMQueuesoc[len];
                WMQueuesoclink.CopyTo(wmqsoc, 0);

                foreach (WMQueuesoc wmqs in wmqsoc)
                {
                    if (wmqs.soc == soc)
                    {

                        WMQueuesoclink.Remove(wmqs);
                        return;
                    }
                }
            }
            catch(Exception e){ }
        }
            
        void addtopic(WMQData wmqd)
        {
            try
            {
                if (wmqd == null)
                {

                }
                if (!WMQTOPICList.ContainsKey(wmqd.to))
                {
                    WMQTOPIC wtpic = new WMQTOPIC();
                    wtpic.topic = wmqd.to;
                    wtpic.wdata.Add(wmqd);
                    WMQTOPICListbool.Add(wmqd.to, false);
                    WMQTOPICList.Add(wmqd.to, wtpic);
                }
                else
                {
                    WMQTOPIC wtpic = WMQTOPICList[wmqd.to];
                    wtpic.topic = wmqd.to;
               
                    wtpic.wdata.Add(wmqd);
                    //WMQTOPICList.Add(wmqd.to, wtpic);
                }
            }
            catch(Exception e) { }
        }
        void addtopicsoc(RegData wmqd)
        {
            try
            {
                if (!WMQTOPICList.ContainsKey(wmqd.to))
                {
                    WMQTOPIC wtpic = new WMQTOPIC();
                    wtpic.topic = wmqd.to;
                    wtpic.ALLsoc.Add(wmqd.soc);
                    WMQTOPICListbool.Add(wmqd.to, false);
                    WMQTOPICList.Add(wmqd.to, wtpic);
                }
                else
                {
                    WMQTOPIC wtpic = WMQTOPICList[wmqd.to];
                    wtpic.topic = wmqd.to;
                    wtpic.ALLsoc.Add(wmqd.soc);
                    //WMQTOPICList.Add(wmqd.to, wtpic);
                }
            }
            catch(Exception e) { }
        }

        // SortedList<>

    }
}
