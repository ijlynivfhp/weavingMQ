﻿using client;
using WeaveBase;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text;
using System.Xml;
using SocketServer;
namespace cloud
{
    public delegate void Mylog(string type, string log);
    public class GateWay
    {
        protected IWeaveTcpBase p2psev;
        List<CommandItem> listcomm = new List<CommandItem>();
        WeaveTable qt = new WeaveTable();
        private int proportion=10;
        public event Mylog EventMylog;
       // public List<ConnObj> ConnObjlist = new List<ConnObj>();
        public List<CommandItem> CommandItemS = new List<CommandItem>();
        public List<WayItem> WayItemS = new List<WayItem>();
        protected WeaveP2Server p2psev2;
        int max = 5000;
        int counttemp = 0;
        WeavePortTypeEnum Wptype = WeavePortTypeEnum.Json;
        private WeavePipelineTypeEnum pipeline = WeavePipelineTypeEnum.ten;
        #region 初始化
        public GateWay(WeavePortTypeEnum Wpte)
        {
            Wptype = Wpte;
            init(Wpte);
        }
        public GateWay(WeavePortTypeEnum Wpte, int _max)
        {
            
            max = _max;
            Wptype = Wpte;
            init(Wpte);



        }
        public clientItem[,,,] ConnItemlist = new clientItem[10, 10, 10, 10];
        void init(WeavePortTypeEnum Wpte)
        {
            if (Wpte == WeavePortTypeEnum.Web)
                p2psev = new WeaveWebServer();
            else if (Wpte == WeavePortTypeEnum.Json)
                p2psev = new WeaveP2Server();
            else if (Wpte == WeavePortTypeEnum.Bytes)
                p2psev = new WeaveP2Server(WeaveDataTypeEnum.Bytes);

        }
        #endregion
        public bool Run(string loaclIP, int port, int port2)
        {
            // Mycommand comm = new Mycommand(, connectionString);
            ReLoad();
            
           
            if (Wptype == WeavePortTypeEnum.Bytes)
                p2psev.weaveReceiveBitEvent += P2psev_weaveReceiveBitEvent;
            else
                p2psev.waveReceiveEvent += p2psev_receiveevent;

            p2psev.weaveUpdateSocketListEvent += p2psev_EventUpdataConnSoc;
            p2psev.weaveDeleteSocketListEvent += p2psev_EventDeleteConnSoc;
            //   p2psev.NATthroughevent += tcp_NATthroughevent;//p2p事件，不需要使用
            p2psev.Start(Convert.ToInt32(port));
            p2psev2 = new WeaveP2Server(loaclIP);
            p2psev2.weaveDeleteSocketListEvent += P2psev2_EventDeleteConnSoc;
            p2psev2.weaveUpdateSocketListEvent += P2psev2_EventUpdataConnSoc;
            p2psev2.waveReceiveEvent += P2psev2_receiveevent;
            //   p2psev.NATthroughevent += tcp_NATthroughevent;//p2p事件，不需要使用
            p2psev2.Start(Convert.ToInt32(port2));
            if (EventMylog != null)
                EventMylog("连接", "连接启动成功");
            return true;
        }

       

        string token = "";
        protected void V_ErrorMge(int type, string error)
        {
            if (EventMylog != null)
                EventMylog("V_ErrorMge", type + ":" + error);
        }
        #region 加载配置文件
        public void ReLoad()
        {
            System.Threading.ThreadPool.QueueUserWorkItem(new System.Threading.WaitCallback(ReloadFlies));
            System.Threading.ThreadPool.QueueUserWorkItem(new System.Threading.WaitCallback(ReloadFliesway));
        }
        public class ipMaster
        {
            public string ip;
            public bool ismaster;
            public bool isline;

        }
        List<ipMaster> iplist = new List<ipMaster>();
        protected void ReloadFlies(object obj)
        {
            try
            { 
                CommandItemS.Clear();
                XmlDocument xml = new XmlDocument();
                xml.Load("node.xml");
         
                foreach (XmlNode xn in xml.FirstChild.ChildNodes)
                {
                    string type = "json";
                    if (Wptype == WeavePortTypeEnum.Bytes)
                    { type = "byte"; }

                    if (type == xn.Attributes["type"].Value)
                    {
                        bool isok = true;
                        CommandItem ci = new CommandItem();
                        ci.Ip = xn.Attributes["ip"].Value;
                        ci.Port = Convert.ToInt32(xn.Attributes["port"].Value);
                        ci.CommName = byte.Parse(xn.Attributes["command"].Value);
                        CommandItemS.Add(ci);
                        foreach (ipMaster s in iplist)
                            if (s.ip == ci.Ip + ":" + ci.Port)
                                isok = false;
                        if (isok)
                        {
                            ipMaster ipm = new ipMaster();
                            ipm.ip = ci.Ip + ":" + ci.Port;
                            if (xn.Attributes["ismaster"] == null)
                                ipm.ismaster = false;
                            else
                           ipm.ismaster= Convert.ToBoolean(xn.Attributes["ismaster"].Value);
                            iplist.Add(ipm);
                        }
                    }
                }
                int countpipeline = iplist.Count * (int)pipeline;
                if (countpipeline > 60000)
                {
                    if (EventMylog != null)
                        EventMylog("设置异常", "你所开启的通道不能超过6万，计算方法，不同的（IP+PORT）总数*通道数量。开启当前设置需要：" + countpipeline + "本地连接，此链接不被系统支持。");
                    return;
                }
                foreach (ipMaster s in iplist)
                {
                    P2Pclient[,,,] client = new P2Pclient[10, 10, 10, 10];
                    for (int i = (int)pipeline; i > 1; i--)
                    {
                        String im = (i - 1).ToString(),temp="";
                        for (int j = 0; j < 4 - im.Length; j++)
                            temp += "0";
                              im = temp+im;
                        System.Threading.Thread.Sleep(100);
                          P2Pclient ct = newp2p(s.ip.Split(':')[0], Convert.ToInt32(s.ip.Split(':')[1]));
                        //P2Pclient ct = new P2Pclient(false);
                      
                            client[int.Parse(im.Substring(0, 1)), int.Parse(im.Substring(1, 1)), int.Parse(im.Substring(2, 1)), int.Parse(im.Substring(3, 1))] = ct;
                       
                    }
                   
                    client[0,0,0,0]= newp2p(s.ip.Split(':')[0], Convert.ToInt32(s.ip.Split(':')[1]));
                    client[0, 0, 0, 0].timeoutobjevent -= P2p_timeoutobjevent;
                    client[0, 0, 0, 0].timeoutobjevent += GateWay_timeoutobjevent;
                    foreach (CommandItem ci in CommandItemS)
                    {
                        if (s.ip == ci.Ip + ":" + ci.Port)
                            ci.Client = client;
                    }
                }
                EventMylog("加载成功", "已完成通道加载。");
            }
            catch (Exception ex)
            {
                if (EventMylog != null)
                    EventMylog("加载异常", ex.Message);
            }
        }

        private void GateWay_timeoutobjevent(P2Pclient p2pobj)
        {
            P2Pclient Client = p2pobj;
            string master = "";

            if (!Client.Isline)
            {
                string ip = p2pobj.IP + ":" + p2pobj.PORT;

                foreach (ipMaster s in iplist)
                {
                    if (s.ip == ip)
                    {
                        s.isline = false;
                        break;
                    }
                }
                foreach (CommandItem ci in CommandItemS)
                {
                    if (ci.Ip + ":" + ci.Port == ip)
                    {
                        ci.ismaster = false;
                        ci.isline = false;


                    }
                }
                foreach (ipMaster s in iplist)
                {
                    if (s.ip != ip && s.isline)
                    {
                        master = s.ip;
                        break;
                    }
                }


                foreach (CommandItem ci in CommandItemS)
                {

                    if (ci.isline)
                        if (master == ci.Ip + ":" + ci.Port)
                            if (!ci.ismaster)
                            {

                                ci.Client[0, 0, 0, 0].send(0xff, "ISmaster");
                                ci.ismaster = true;


                            }
                }
                P2p_timeoutobjevent(p2pobj);
                p2pobj.send(0xff, "slave");
                foreach (CommandItem ci in CommandItemS)
                {
                    if (ci.Ip + ":" + ci.Port == ip)
                    {
                        ci.isline = true;
                    }
                }
                foreach (ipMaster s in iplist)
                {
                    if (s.ip != ip)
                    {
                        s.isline = true;
                    }
                }
            }
            //这里把新链接的服务，设置为从机
        }

        P2Pclient newp2p(String Ip,int Port)
        {
            P2Pclient p2p = new P2Pclient(false);
            if (Wptype == WeavePortTypeEnum.Bytes)
                p2p.receiveServerEventbit += P2p_receiveServerEventbit;
                else
            p2p.receiveServerEvent += (V_receiveServerEvent);
            p2p.timeoutobjevent += P2p_timeoutobjevent;
            p2p.ErrorMge += (V_ErrorMge);
            if (p2p.start(Ip, Port, false))
            {
                return p2p;
            }
            else
            {
                if (EventMylog != null)
                    EventMylog("节点连接失败", "命令：" + Ip+":"+Port + ":节点连接失败，抛弃此节点");
            }
            return null;
        }


        string master="";
        private void P2p_timeoutobjevent(P2Pclient p2pobj)
        {
            P2Pclient Client = p2pobj;

            lab1100:
            if (!Client.Isline)
            {
                string ip = p2pobj.IP + ":" + p2pobj.PORT;

                
               
                string port = Client.localprot;
                if (EventMylog != null)
                    EventMylog("节点重新连接--:", Client.IP + ":" + Client.PORT);
                if (!Client.Restart(false))
                {
                    System.Threading.Thread.Sleep(1000);
                    goto lab1100;


                }
                else
                {
                   

                    try
                    {
                        EventMylog("节点重新连接-通知下线-:", Client.IP + ":" + Client.PORT); 
                    }
                    catch (Exception ee) { EventMylog("节点重新连接-Restart-:", ee.Message); }
                    //Client.send(0xff, "Restart|"+ port);
                    EventMylog("节点重新连接-Restart-:", Client.IP + ":" + Client.PORT);
                }
            }
        }

        protected void ReloadFliesway(object obj)
        {
            try
            {
                foreach (WayItem ci in WayItemS)
                {
                    ci.Client.stop();
                }
                WayItemS.Clear();
                XmlDocument xml = new XmlDocument();
                xml.Load("nodeway.xml");
                foreach (XmlNode xn in xml.FirstChild.ChildNodes)
                {
                    WayItem ci = new WayItem();
                    ci.Ip = xn.Attributes["ip"].Value;
                    ci.Port = Convert.ToInt32(xn.Attributes["port"].Value);
                    ci.Token = (xn.Attributes["token"].Value);
                    ci.Client = new P2Pclient(false);
                    ci.Client.receiveServerEvent += Client_receiveServerEvent;
                    ci.Client.timeoutobjevent += Client_timeoutobjevent;
                    
                    ci.Client.ErrorMge += Client_ErrorMge;
                    if (ci.Client.start(ci.Ip, ci.Port, false))
                    {
                        WeaveBase.WeaveSession oxff = new WeaveBase.WeaveSession();
                        oxff.Request = "token";
                        oxff.Root = ci.Token;
                        ci.Client.send(0xff, oxff.Getjson());
                        WayItemS.Add(ci);
                    }
                    else
                    {
                        if (EventMylog != null)
                            EventMylog("从网关连接失败", "从网关：" + ci.Ip + ":节点连接失败，抛弃此节点");
                    }
                }
                System.Threading.ThreadPool.QueueUserWorkItem(new System.Threading.WaitCallback(getwaynum));
            }
            catch (Exception ex)
            {
                if (EventMylog != null)
                    EventMylog("加载异常", ex.Message);
            }
        }

        private void Client_timeoutobjevent(P2Pclient p2pobj)
        {
            try
            {
               
                        if (p2pobj.Restart(false))
                        {
                            Client_timeoutobjevent(p2pobj);
                            System.Threading.Thread.Sleep(5000);
                        }
                  
            }
            catch (Exception ex)
            {
                if (EventMylog != null)
                    EventMylog("way节点重新连接", ex.Message);
                Client_timeoutobjevent(p2pobj);
                System.Threading.Thread.Sleep(5000);
            }
        }
        #endregion
        public void getwaynum(object obj)
        {
            while (true)
            {
                try
                {
                    int count = WayItemS.Count;
                    WayItem[] coobjs = new WayItem[count];
                    WayItemS.CopyTo(0, coobjs, 0, count);
                    foreach (WayItem wi in coobjs)
                    {
                        WeaveBase.WeaveSession oxff = new WeaveBase.WeaveSession();
                        oxff.Request = "getnum";
                        oxff.Root = wi.Token;
                        wi.Client.send(0xff, oxff.Getjson());
                    }
                }
                catch { }
                System.Threading.Thread.Sleep(2000);
            }
        }
        public int getnum()
        {
            return counttemp;
        }
        protected void Client_ErrorMge(int type, string error)
        {
        }
        #region 相互间路由的通信
        protected void P2psev2_receiveevent(byte command, string data, Socket soc)
        {
            try
            {
                WeaveSession _0x01 = Newtonsoft.Json.JsonConvert.DeserializeObject<WeaveSession>(data);
                if (command == 0xff)
                {
                    if (_0x01.Request == "token")
                    {
                        token = _0x01.Root;
                    }
                    else if (_0x01.Request == "getnum")
                    {
                        _0x01.Request = "setnum";
                        _0x01.Token = token;
                        _0x01.Root = getnum().ToString();
                        p2psev2.Send(soc, 0xff, _0x01.Getjson());
                    }
                }
            }
            catch { }
        }
        protected void P2psev2_EventUpdataConnSoc(Socket soc)
        {
        }
        protected void P2psev2_EventDeleteConnSoc(Socket soc)
        {
        }
        
        protected void Client_receiveServerEvent(byte command, string text)
        {
            try
            {
                WeaveSession _0x01 = Newtonsoft.Json.JsonConvert.DeserializeObject<WeaveSession>(text);
                if (_0x01.Request == "setnum")
                {
                    int count = WayItemS.Count;
                    WayItem[] coobjs = new WayItem[count];
                    WayItemS.CopyTo(0, coobjs, 0, count);
                    foreach (WayItem wi in coobjs)
                    {
                        if (wi.Token == _0x01.Token)
                            wi.Num = int.Parse(_0x01.Root);
                    }
                }
            }
            catch { }
        }
        #endregion
        #region 客户端连接与消息转发
        /// <summary>
        /// 这里写接收到服务器发送的消息转发给客户端
        /// </summary>
        /// <param name="command"></param>
        /// <param name="text"></param>
        protected void V_receiveServerEvent(byte command, string text)
        {
            try
            {
                string token = "";
                String ip = "";
                int port = 0;
                if (text == "")
                    return;
                //WeaveSession _0x01 = null;
                try
                {
                  
                    //  _0x01 = Newtonsoft.Json.JsonConvert.DeserializeObject<WeaveSession>(text);
                    if (text.IndexOf('{') > 0)
                    {
                        token = text.Substring(0, text.IndexOf('{'));
                        text = text.Substring(text.IndexOf('{'));
                        ip = token.Split(':')[0];
                        port = Convert.ToInt32(token.Split(':')[1]);
                    }
                    else { return; }
                }
                catch { return; }


            int temp = 0;
             
        
              ConnObj cobj=  GateHelper.GetConnItemlist(ConnItemlist, ip, port, Pipeline);
                if (cobj != null)
                {
                    int error = 0;
                    lb1122:
                    if (!p2psev.Send(cobj.Soc, command, text))
                    {
                        error += 1;
                        EventMylog("转发" + error, "ConnObjlist:发送失败：" + text);
                        if (error < 3) goto lb1122;
                    }
                }
                return;
            }
            catch (Exception ex) { EventMylog("转发", ex.Message+"112223333333333356464122313"+ text+"000000"); }
        }

        

        protected void p2psev_EventDeleteConnSoc(System.Net.Sockets.Socket soc)
        {
            try
            {
              
                counttemp--;
                IPEndPoint clientipe = (IPEndPoint)soc.RemoteEndPoint;
                GateHelper.removeConnItemlist(ConnItemlist, soc, Pipeline);
                List<String> listsercer = new List<string>();
                bool tempb = true;
                foreach (CommandItem ci in CommandItemS)
                {
                    tempb = true;
                    foreach (string ser in listsercer)
                    {
                        if (ser == (ci.Ip + ci.Port))
                        {
                            tempb = false;
                            goto lab882;
                        }
                    }
                    lab882:
                    if (tempb)
                    {
                        if (ci.Client[0, 0, 0, 0] != null)
                        {
                            listsercer.Add(ci.Ip + ci.Port);
                          String  tempip = ci.Ip + ":" + ci.Port;
                            ci.Client[0, 0, 0, Convert.ToInt32(clientipe.Port.ToString().Substring(clientipe.Port.ToString().Length - 1, 1))].send(0xff, "out|" + tempip);
                        }
                    }
                }

            }
            catch (Exception ex)
            {
                if (EventMylog != null)
                    EventMylog("移除用户", ex.Message);
            }
        } 
      
       
        protected void p2psev_EventUpdataConnSoc(System.Net.Sockets.Socket soc)
        {
            ConnObj cobj = new ConnObj();
            try {
            cobj.Soc = soc;
            IPEndPoint clientipe = (IPEndPoint)soc.RemoteEndPoint;
       
             
            counttemp++;
             //这里通过IP和PORT获取对象

            cobj.Token = clientipe.Address.ToString()+":"+ clientipe.Port;// EncryptDES(clientipe.Address.ToString() + "|" + DateTime.Now.ToString(), "lllssscc");
             
            cobj.Soc = soc;


            if (counttemp > max)
            {
                int mincount = int.MaxValue;
                string tempip = "";
                foreach (WayItem ci in WayItemS)
                {
                    if (ci.Num < mincount)
                    {
                        mincount = ci.Num;
                        tempip = ci.Ip + ":" + ci.Port;
                    }
                }
                if (Wptype == WeavePortTypeEnum.Bytes)
                    p2psev.Send(soc, 0xff, UTF8Encoding.UTF8.GetBytes("jump|" + tempip + ""));
                else
                    p2psev.Send(soc, 0xff, "jump|" + tempip + "");
                soc.Close();
                return;
            }
           
                //IPEndPoint clientipe = (IPEndPoint)soc.RemoteEndPoint;
                ////if (Wptype != WeavePortTypeEnum.Bytes)
                ////    p2psev.Send(soc, 0xff, "token|" + cobj.Token + "");
                
                 GateHelper.SetConnItemlist(ConnItemlist, cobj, Pipeline);
                
                //List<String> listsercer = new List<string>();
                //bool tempb = true;
                //foreach (CommandItem ci in CommandItemS)
                //{
                //    tempb = true;
                //    foreach (string ser in listsercer)
                //    {
                //        if (ser == (ci.Ip + ci.Port))
                //        {
                //            tempb = false;
                //            goto lab882;
                //        }
                //    }
                //    lab882:
                //    if (tempb)
                //    {
                //        if (ci.Client[0, 0, 0, 0] != null)
                //        {
                //            listsercer.Add(ci.Ip + ci.Port);
                //            ci.Client[0, 0, 0, Convert.ToInt32(clientipe.Port.ToString().Substring(clientipe.Port.ToString().Length - 1, 1))].send(0xff, "in|" + cobj.Token);
                //        }
                //    }
                //}
              
            }
            catch (Exception ex)
            {
                if (EventMylog != null)
                    EventMylog("EventUpdataConnSoc", ex.Message);
            }
        } 
       
      
        /// <summary>
        /// 收到客户端发来的消息，并转发到服务端中心
        /// </summary>
        /// <param name="command"></param>
        /// <param name="data"></param>
        /// <param name="soc"></param>
        protected void p2psev_receiveevent(byte command, string data, System.Net.Sockets.Socket soc)
        {
            try
            {
                // JSON.parse<_baseModel>(data);// 
                //WeaveBase.WeaveSession _0x01;
                //try
                //{
                //     _0x01 = Newtonsoft.Json.JsonConvert.DeserializeObject<WeaveSession>(data);
                //}
                //catch {
                //    EventMylog("JSON解析错误：", ""+ data);
                //    return; }
                //if (_0x01.Token == null)
                //{
                //    EventMylog("Token是NULL：", "" + data);
                //    return;
                //}
                
                IPEndPoint clientipe = (IPEndPoint)soc.RemoteEndPoint;
                
                int count = CommandItemS.Count;
                String Token = "";
                try
                {
                    // temp = _0x01.Token.Split(':');
                    //if (temp.Length < 2)
                    //    return;
                     Token = clientipe.Address.ToString() + ":" + clientipe.Port;
                }
                catch(Exception e)
                {
                    if (EventMylog != null)
                        EventMylog("p2psev_receiveevent", e.Message);
                    return;
                }
                
                foreach (CommandItem ci in CommandItemS)
                {
                    if (ci != null)
                    {
                        if (ci.CommName == command)
                        {
                            P2Pclient[,,,] client = ci.Client;
                            P2Pclient p2ptemp= GateHelper.GetP2Pclient(client, soc, Pipeline);
                            if (p2ptemp != null)
                            {
                                if (p2ptemp.Isline)
                                {
                                   
                                    if (!p2ptemp.send(command, Token + data))
                                    {
                                        p2psev.Send(soc, 0xff, "你所请求的服务暂不能使用，发送错误。");
                                    }
                                }

                            }
                        }
                    }
                }
            }
            catch (Exception ex){
                if (EventMylog != null)
                    EventMylog("p2psev_receiveevent----", ex.Message);
            }
        }
        #endregion

        #region 客户端连接与消息转发BYTE

        private void P2p_receiveServerEventbit(byte command, byte[] data)
        {

            try
            {
                if (data.Length < 6)
                    return;
                byte[] b = new byte[6];
                data.CopyTo(b, data.Length - b.Length);
                ConnObj cobj = GateHelper.GetConnItemlistByindex(ConnItemlist, b, Pipeline);
                if (cobj != null)
                {
                    int error = 0;
                    lb1122:
                    if (!p2psev.Send(cobj.Soc, command, data))
                    {
                        error += 1;
                        EventMylog("转发" + error, "ConnObjlist:发送失败：byte");
                        if (error < 3) goto lb1122;
                    }
                }
                else
                {
                    EventMylog("转发", "ConnObjlist:byte是空的");
                }
                return;
            }
            catch { }

        }
        /// <summary>
        /// 收到客户端发来的Byte消息，并转发到服务端中心
        /// </summary>
        /// <param name="command"></param>
        /// <param name="data"></param>
        /// <param name="soc"></param>
        private void P2psev_weaveReceiveBitEvent(byte command, byte[] data, Socket soc)
        {
            try
            {
                // JSON.parse<_baseModel>(data);// 


                IPEndPoint clientipe = (IPEndPoint)soc.RemoteEndPoint;

                int count = CommandItemS.Count;

                try
                {
                    // temp = _0x01.Token.Split(':');
                    //if (temp.Length < 2)
                    //    return;
                    //_0x01.Token = clientipe.Address.ToString() + ":" + clientipe.Port;
                }
                catch (Exception e)
                {
                    if (EventMylog != null)
                        EventMylog("p2psev_receiveevent", e.Message);
                    return;
                }

                foreach (CommandItem ci in CommandItemS)
                {
                    if (ci != null)
                    {
                        if (ci.CommName == command)
                        {
                            P2Pclient[,,,] client = ci.Client;
                            P2Pclient p2ptemp = GateHelper.GetP2Pclient(client, soc, Pipeline);
                            byte[] b = GateHelper.GetP2PclientIndex(client, soc, Pipeline);
                            if (p2ptemp != null)
                            {
                                if (!p2ptemp.Isline)
                                { p2psev.Send(soc, 0xff, "你所请求的服务暂不能使用，已断开连接！"); return; }
                                byte[] tempdata = new byte[data.Length + b.Length];
                                Array.Copy(data, 0, tempdata, 0, data.Length);
                                Array.Copy(b, 0, tempdata, data.Length - 1, b.Length);
                                if (!p2ptemp.send(command, tempdata))
                                {
                                    p2psev.Send(soc, 0xff, "你所请求的服务暂不能使用，发送错误。");
                                }

                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                if (EventMylog != null)
                    EventMylog("p2psev_receiveevent----", ex.Message);
            }
        }
        #endregion

        private byte[] Keys = { 0xEF, 0xAB, 0x56, 0x78, 0x90, 0x34, 0xCD, 0x12 };
        public int Proportion
        {
            get
            {
                return proportion;
            }
            set
            {
                proportion = value;
            }
        }

        public WeavePipelineTypeEnum Pipeline { get {  return pipeline; } set { pipeline = value; } }

        
    }
    
}
