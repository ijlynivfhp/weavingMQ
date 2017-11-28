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

    public class WMQ
    {
        Dictionary<String, WMQTOPIC> WMQTOPICList = new Dictionary<String, WMQTOPIC>();
        Dictionary<String, bool> WMQTOPICListbool = new Dictionary<String, bool>();
        List<WMQueuesoc> WMQueuesoclink = new List<WMQueuesoc>();
        List<WMQData> WMQDatalink = new List<WMQData>();
        List<WMQMODE> listiwtcp = new List<WMQMODE>();
        public bool ISmaster { get ; set; }

        public WMQ(List<WMQMODE> _listiwtcp)
        {
            listiwtcp = _listiwtcp;
            System.Threading.Thread t = new System.Threading.Thread(new System.Threading.ThreadStart(topicgo));
            t.Start();
            System.Threading.Thread t1 = new System.Threading.Thread(new System.Threading.ThreadStart(Queuego));
            t1.Start();
        }
        public bool Send<T>(Socket soc, byte command, T t,String fromtoken)
        {
            try
            {
                foreach (WMQMODE wm in listiwtcp)
                {
                    if (wm.iwtb.Port == ((System.Net.IPEndPoint)soc.LocalEndPoint).Port)
                    {
                        String str = Newtonsoft.Json.JsonConvert.SerializeObject(t);
                        return wm.iwtb.Send(soc, command, fromtoken+str);

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
                        WMQData wmq = WMQDatalink[0];
                        if (wmq != null)
                        {
                            int len = WMQueuesoclink.Count;
                            WMQueuesoc[] wmqsoc = new WMQueuesoc[len];
                            WMQueuesoclink.CopyTo(wmqsoc, 0);
                            bool isok = false;
                            if(ISmaster)
                            foreach (WMQueuesoc wmqs in wmqsoc)
                            {
                                if (wmq.to == wmqs.token)
                                {
                                    isok = Send<WMQData>(wmqs.soc, 0x01, wmq, wmqs.fromtoken);
                                    break;
                                }
                            }

                            if (!isok)
                            {
                                
                                    WMQDatalink.Add(wmq);
                                
                            }
                            WMQDatalink.RemoveAt(0);
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
                List<Socketway> socs = WMQTOPICList[key].ALLsoc;
                len = socs.Count;
                Socketway[] Sockets = new Socketway[len];
                socs.CopyTo(Sockets, 0);
                foreach (WMQData wd in WMQDatas)
                {
                    if (wd != null && ISmaster==true)
                        foreach (Socketway soc in Sockets)
                        {
                          
                            Send<WMQData>(soc.soc, 0x02,  wd, soc.from);
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
                String token = "";
                if (data.IndexOf('{') > 0)
                {
                    token = data.Substring(0, data.IndexOf('{'));
                    data = data.Substring(data.IndexOf('{'));
                }
                
                WMQData wmqd = new WMQData();
                switch (command)
                {
                    case 0:
                        RegData rd = Newtonsoft.Json.JsonConvert.DeserializeObject<RegData>(data);
                        rd.from = token;
                        rd.soc = soc;
                        if (rd.type == "topic")
                        {
                            addtopicsoc(rd);
                        }
                        else
                        {
                            Addqueuesoc(rd);
                        }
                        break;
                    case 1:
                        wmqd = Newtonsoft.Json.JsonConvert.DeserializeObject<WMQData>(data);
                        wmqd.ctime = DateTime.Now;
                        
                            WMQDatalink.Add(wmqd);
                         
                        break;
                    case 2:
                        wmqd = Newtonsoft.Json.JsonConvert.DeserializeObject<WMQData>(data);
                        wmqd.ctime = DateTime.Now;
                        if (wmqd == null)
                        {
                        }
                        addtopic(wmqd);
                        break;
                    case 0xff:
                        if (data.IndexOf("out") >= 0)
                        {
                            string fromtoken = data.Split('|')[1];
                            deletesocbyfromtoken(fromtoken);
                        }
                        if (data.IndexOf("ISmaster") >= 0)
                        {
                            ISmaster = true;
                        }
                        if (data.IndexOf("slave") >= 0)
                        {
                            ISmaster = false;
                        }
                        break;

                }
            }
            catch(Exception e) { return false; }
            return true;

        }

        void Addqueuesoc(RegData rd)
        {
            int len = WMQueuesoclink.Count;
            WMQueuesoc wq = new WMQueuesoc();
            wq.token = rd.to; wq.soc = rd.soc;
            if (len > 0)
            {
                WMQueuesoc[] wmqsoc = new WMQueuesoc[len];
                WMQueuesoclink.CopyTo(wmqsoc, 0);

                wq.fromtoken = rd.from;
                foreach (WMQueuesoc wmqs in wmqsoc)
                {
                    if (wmqs != null)
                    {
                        if (rd.from != "")
                            if (wmqs.fromtoken == rd.from)
                            {

                                WMQueuesoclink.Remove(wmqs);

                            }
                        if (wmqs.token == rd.to)
                        { WMQueuesoclink.Remove(wmqs); }
                        else
                        {
                            if (wmqs.soc == rd.soc)
                                WMQueuesoclink.Remove(wmqs);
                        }

                    }
                }
            }
            try
            {
                WMQueuesoclink.Add(wq);
            }
            catch
            {
                WMQueuesoclink.Add(wq);
            }
            return ;
        }

        #region 移除用户
        public void deletesocbyfromtoken(string fromtoken)
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
                            int len1 = WMQTOPICList[key].ALLsoc.Count;
                            Socketway[] wsoc = new Socketway[len1];
                            WMQTOPICList[key].ALLsoc.CopyTo(wsoc, 0);
                            foreach (Socketway sw in wsoc)
                            {
                                if (sw != null)
                                    if (sw.from == fromtoken)
                                    {
                                        WMQTOPICList[key].ALLsoc.Remove(sw);
                                        break;
                                    }
                            }

                        }
                        catch { }
                    }
                }


            }
            catch (Exception e) { }
            try
            {

                int len = WMQueuesoclink.Count;
                WMQueuesoc[] wmqsoc = new WMQueuesoc[len];
                WMQueuesoclink.CopyTo(wmqsoc, 0);

                foreach (WMQueuesoc wmqs in wmqsoc)
                {
                    if (wmqs != null)
                        if (wmqs.fromtoken == fromtoken)
                        {

                            WMQueuesoclink.Remove(wmqs);
                            return;
                        }
                }
            }
            catch { }
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
                            int len1 = WMQTOPICList[key].ALLsoc.Count;
                            Socketway[] wsoc = new Socketway[len1];
                            WMQTOPICList[key].ALLsoc.CopyTo(wsoc, 0);
                            foreach (Socketway sw in wsoc)
                            {
                                if (sw != null)
                                    if (sw.soc == soc)
                                    {
                                        WMQTOPICList[key].ALLsoc.Remove(sw);
                                        // break;
                                    }
                            }

                        }
                        catch { }
                    }
                }


            }
            catch (Exception e) { }
            try
            {

                int len = WMQueuesoclink.Count;
                WMQueuesoc[] wmqsoc = new WMQueuesoc[len];
                WMQueuesoclink.CopyTo(wmqsoc, 0);

                foreach (WMQueuesoc wmqs in wmqsoc)
                {
                    if (wmqs != null)
                        if (wmqs.soc == soc)
                        {

                            WMQueuesoclink.Remove(wmqs);
                            return;
                        }
                }
            }
            catch { }
        }
        #endregion

        #region 加入TOPIC

        
        void addtopic(WMQData wmqd)
        {
            try
            {
                if (wmqd == null)
                {

                }
                wmqd.id = DateTime.Now.ToString("yyyyMMddHHmmssfff");
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
                    Socketway sw = new Socketway();
                    sw.soc = wmqd.soc;
                    sw.from = wmqd.from;
                    
                    wtpic.ALLsoc.Add(sw);
                    WMQTOPICListbool.Add(wmqd.to, false);
                    WMQTOPICList.Add(wmqd.to, wtpic);
                }
                else
                {
                  
                    WMQTOPIC wtpic = WMQTOPICList[wmqd.to];
                    wtpic.topic = wmqd.to;
                    Socketway sw = new Socketway();
                    sw.soc = wmqd.soc;
                    sw.from = wmqd.from;
                    wtpic.ALLsoc.Add(sw);
                    //WMQTOPICList.Add(wmqd.to, wtpic);
                }
            }
            catch(Exception e) { }
        }
        #endregion
        // SortedList<>

    }
}
