﻿using client;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using WeaveBase;

namespace cloud
{

    public class GateHelper
    {

        public static int ConvertToInt(byte[] list)
        {
            if (list[0] == 0 && list.Length>1)
                list = new byte[1] { list[1] };

                int ret = 0;
            int i = 0;
            foreach (byte item in list)
            {
                 
                    ret = ret + (item << i);
                    i = i + 8;
                
            }
           
            return ret;
        }
        public static byte[] ConvertToByteList(int v)
        {
            List<byte> ret = new List<byte>();
            int value = v;
            while (value != 0)
            {
                ret.Add((byte)value);
                value = value >> 8;
            }
            byte[] bb = new byte[0];
            if (ret.Count == 1)
            {
               bb = new byte[2] {0, ret[0]};
            }
            else
            {
                bb = new byte[ret.Count];
                ret.CopyTo(bb);
            }
            return bb;
        }
        public static P2Pclient GetP2Pclient(P2Pclient[,,,] P2Pclientlist, Socket soc, WeavePipelineTypeEnum pipeline)
        {
            IPEndPoint clientipe = (IPEndPoint)soc.RemoteEndPoint;

            if (pipeline == WeavePipelineTypeEnum.ten)
            {
                string t = clientipe.Port.ToString().Substring(clientipe.Port.ToString().Length - 1);
                return P2Pclientlist[0, 0, 0, Convert.ToInt32(t)];
            }
            else if (pipeline == WeavePipelineTypeEnum.hundred)
            {
                string t = clientipe.Port.ToString().Substring(clientipe.Port.ToString().Length - 2);
                return P2Pclientlist[0, 0, Convert.ToInt32(t.Substring(0, 1)), Convert.ToInt32(t.Substring(1, 1))];
            }
            else if (pipeline == WeavePipelineTypeEnum.thousand)
            {
                string t = clientipe.Port.ToString().Substring(clientipe.Port.ToString().Length - 2);
                string m = clientipe.Address.ToString().Substring(clientipe.Address.ToString().Length - 1);
                return P2Pclientlist[0, Convert.ToInt32(m), Convert.ToInt32(t.Substring(0, 1)), Convert.ToInt32(t.Substring(1, 1))];
            }
            else if (pipeline == WeavePipelineTypeEnum.ten_thousand)
            {
                string t = clientipe.Port.ToString().Substring(clientipe.Port.ToString().Length - 2);
                string m = clientipe.Address.ToString().Split('.')[3];
                if (m.Length < 2)
                    m = "0" + m;
                return P2Pclientlist[Convert.ToInt32(m.Substring(0, 1)), Convert.ToInt32(m.Substring(1, 1)), Convert.ToInt32(t.Substring(0, 1)), Convert.ToInt32(t.Substring(1, 1))];
            }
            return null;

        }
        public static byte[] GetP2PclientIndex(P2Pclient[,,,] P2Pclientlist, Socket soc, WeavePipelineTypeEnum pipeline)
        {
            IPEndPoint clientipe = (IPEndPoint)soc.RemoteEndPoint;
            byte[] b = new byte[6];
         
          byte [] V=  clientipe.Address.GetAddressBytes();
            for (int i = 0; i < V.Length; i++)
            {
                b[i] = V[i];
            }
            byte[] p = ConvertToByteList(clientipe.Port);
            b[4] = p[0];
            b[5] = p[1];
            return b;

        }
        /// <summary>
        /// 通过IP+PROT 得到连接的客户端
        /// </summary>
        /// <param name="ConnItemlist"></param>
        /// <param name="ip"></param>
        /// <param name="port"></param>
        /// <param name="pipeline"></param>
        /// <returns></returns>
        public static ConnObj GetConnItemlistByindex(clientItem[,,,] ConnItemlist, byte [] b, WeavePipelineTypeEnum pipeline)
        {
            byte[] v = new byte[4];
            Array.Copy(b, v, 4);
             IPAddress ipa = new IPAddress(v);

            String ip = ipa.ToString();
            byte[] p = new byte[2];
            Array.Copy(b,0, p, 3,2);
            int port = ConvertToInt(p);


            string temp = "";
            if (pipeline == WeavePipelineTypeEnum.ten)
            {
                string t = port.ToString().Substring(port.ToString().Length - 1);
                temp = t;


            }
            else if (pipeline == WeavePipelineTypeEnum.hundred)
            {
                string t = port.ToString().Substring(port.ToString().Length - 2);
                temp = t;


            }
            else if (pipeline == WeavePipelineTypeEnum.thousand)
            {
                string t = port.ToString().Substring(port.ToString().Length - 2);
                string m = ipa.ToString().Substring(ipa.ToString().Length - 1);
                temp = m + t;


            }
            else if (pipeline == WeavePipelineTypeEnum.ten_thousand)
            {
                string t = port.ToString().Substring(port.ToString().Length - 2);
                string m = ipa.ToString().Split('.')[3];
                if (m.Length < 2)
                    m = "0" + m;
                temp = m + t;


            }
            String tt = "";
            for (int i = 0; i < 4 - temp.Length; i++)
                tt += "0";
            temp = tt + temp;
          return  ConnItemlist[Convert.ToInt32(temp.Substring(0, 1)), Convert.ToInt32(temp.Substring(1, 1)), Convert.ToInt32(temp.Substring(2, 1)), Convert.ToInt32(temp.Substring(3, 1))].getconn(ip, port);
           
            

        }
        /// <summary>
        /// 通过IP+PROT 得到连接的客户端
        /// </summary>
        /// <param name="ConnItemlist"></param>
        /// <param name="ip"></param>
        /// <param name="port"></param>
        /// <param name="pipeline"></param>
        /// <returns></returns>
        public static ConnObj GetConnItemlist(clientItem[,,,] ConnItemlist,String ip,int port , WeavePipelineTypeEnum pipeline)
        {
            IPEndPoint clientipe = new IPEndPoint(IPAddress.Parse(ip), port);
          
            if (pipeline == WeavePipelineTypeEnum.ten)
            {
                string t = clientipe.Port.ToString().Substring(clientipe.Port.ToString().Length - 1);
                return ConnItemlist[0, 0, 0, Convert.ToInt32(t)].getconn(ip, port);
            }
            else if (pipeline == WeavePipelineTypeEnum.hundred)
            {
                string t = clientipe.Port.ToString().Substring(clientipe.Port.ToString().Length - 2);
                return ConnItemlist[0, 0, Convert.ToInt32(t.Substring(0, 1)), Convert.ToInt32(t.Substring(1, 1))].getconn(ip, port);
            }
            else if (pipeline == WeavePipelineTypeEnum.thousand)
            {
                string t = clientipe.Port.ToString().Substring(clientipe.Port.ToString().Length - 2);
                string m = clientipe.Address.ToString().Substring(clientipe.Address.ToString().Length - 1);
                return ConnItemlist[0, Convert.ToInt32(m), Convert.ToInt32(t.Substring(0, 1)), Convert.ToInt32(t.Substring(1, 1))].getconn(ip, port);
            }
            else if (pipeline == WeavePipelineTypeEnum.ten_thousand)
            {
                string t = clientipe.Port.ToString().Substring(clientipe.Port.ToString().Length - 2);
                string m = clientipe.Address.ToString().Split('.')[3];
                if (m.Length < 2)
                    m = "0" + m;
              return  ConnItemlist[Convert.ToInt32(m.Substring(0, 1)), Convert.ToInt32(m.Substring(1, 1)), Convert.ToInt32(t.Substring(0, 1)), Convert.ToInt32(t.Substring(1, 1))].getconn(ip, port);
            }
            return null;
            
        }
        /// <summary>
        /// 从列表中移除客户端对象
        /// </summary>
        /// <param name="ConnItemlist"></param>
        /// <param name="soc"></param>
        /// <param name="pipeline"></param>
        public static void removeConnItemlist(clientItem[,,,] ConnItemlist,Socket soc, WeavePipelineTypeEnum pipeline)
        {
          
                IPEndPoint clientipe = (IPEndPoint)soc.RemoteEndPoint;

                if (pipeline == WeavePipelineTypeEnum.ten)
                {
                    string t = clientipe.Port.ToString().Substring(clientipe.Port.ToString().Length - 1);
                    ConnItemlist[0, 0, 0, Convert.ToInt32(t)].removeconn(soc);
                }
                else if (pipeline == WeavePipelineTypeEnum.hundred)
                {
                    string t = clientipe.Port.ToString().Substring(clientipe.Port.ToString().Length - 2);
                    ConnItemlist[0, 0, Convert.ToInt32(t.Substring(0, 1)), Convert.ToInt32(t.Substring(1, 1))].removeconn(soc);
                }
                else if (pipeline == WeavePipelineTypeEnum.thousand)
                {
                    string t = clientipe.Port.ToString().Substring(clientipe.Port.ToString().Length - 2);
                    string m = clientipe.Address.ToString().Substring(clientipe.Address.ToString().Length - 1);
                    ConnItemlist[0, Convert.ToInt32(m), Convert.ToInt32(t.Substring(0, 1)), Convert.ToInt32(t.Substring(1, 1))].removeconn(soc);
                }
                else if (pipeline == WeavePipelineTypeEnum.ten_thousand)
                {
                    string t = clientipe.Port.ToString().Substring(clientipe.Port.ToString().Length - 2);
                    string m = clientipe.Address.ToString().Split('.')[3];
                    if (m.Length < 2)
                        m = "0" + m;
                    ConnItemlist[Convert.ToInt32(m.Substring(0, 1)), Convert.ToInt32(m.Substring(1, 1)), Convert.ToInt32(t.Substring(0, 1)), Convert.ToInt32(t.Substring(1, 1))].removeconn(soc);
                }

       
        }
        /// <summary>
        /// 将对象存入数组对象中
        /// </summary>
        /// <param name="ConnItemlist"></param>
        /// <param name="connb"></param>
        /// <param name="pipeline"></param>
        public static void SetConnItemlist(clientItem[,,,] ConnItemlist, ConnObj connb, WeavePipelineTypeEnum pipeline)
        {
            
            IPEndPoint clientipe = (IPEndPoint)connb.Soc.RemoteEndPoint;
            if (pipeline == WeavePipelineTypeEnum.ten)
            {
               string t= clientipe.Port.ToString().Substring(clientipe.Port.ToString().Length - 1);
                connb.Id = Convert.ToInt32(t);
                if (ConnItemlist[0, 0, 0, Convert.ToInt32(t)] == null)
                    ConnItemlist[0, 0, 0, Convert.ToInt32(t)] = new clientItem();
                  ConnItemlist[0, 0, 0, Convert.ToInt32(t)].setconn(connb);
               
            }
            else if (pipeline == WeavePipelineTypeEnum.hundred)
            {
                string t = clientipe.Port.ToString().Substring(clientipe.Port.ToString().Length - 2);
                connb.Id = Convert.ToInt32(t);
                if (ConnItemlist[0, 0, Convert.ToInt32(t.Substring(0, 1)), Convert.ToInt32(t.Substring(1, 1))] == null)
                    ConnItemlist[0, 0, Convert.ToInt32(t.Substring(0, 1)), Convert.ToInt32(t.Substring(1, 1))] = new clientItem();
                ConnItemlist[0, 0, Convert.ToInt32(t.Substring(0, 1)), Convert.ToInt32(t.Substring(1,1))].setconn(connb);
            }
            else if (pipeline == WeavePipelineTypeEnum.thousand)
            {
                string t = clientipe.Port.ToString().Substring(clientipe.Port.ToString().Length - 2);
                string m = clientipe.Address.ToString().Substring(clientipe.Address.ToString().Length - 1);
                connb.Id = Convert.ToInt32(m+t);
                if (ConnItemlist[0, Convert.ToInt32(m), Convert.ToInt32(t.Substring(0, 1)), Convert.ToInt32(t.Substring(1, 1))] == null)
                    ConnItemlist[0, Convert.ToInt32(m), Convert.ToInt32(t.Substring(0, 1)), Convert.ToInt32(t.Substring(1, 1))] = new clientItem();
                ConnItemlist[0, Convert.ToInt32(m), Convert.ToInt32(t.Substring(0, 1)), Convert.ToInt32(t.Substring(1, 1))].setconn(connb);
            }
            else if (pipeline == WeavePipelineTypeEnum.ten_thousand)
            {
                string t = clientipe.Port.ToString().Substring(clientipe.Port.ToString().Length - 2);
                string m = clientipe.Address.ToString().Split('.')[3];

                if (m.Length < 2)
                    m = "0" + m;
                connb.Id = Convert.ToInt32(m + t);
                if (ConnItemlist[Convert.ToInt32(m.Substring(0, 1)), Convert.ToInt32(m.Substring(1, 1)), Convert.ToInt32(t.Substring(0, 1)), Convert.ToInt32(t.Substring(1, 1))] == null)
                    ConnItemlist[Convert.ToInt32(m.Substring(0, 1)), Convert.ToInt32(m.Substring(1, 1)), Convert.ToInt32(t.Substring(0, 1)), Convert.ToInt32(t.Substring(1, 1))] = new clientItem();
                ConnItemlist[Convert.ToInt32(m.Substring(0, 1)), Convert.ToInt32(m.Substring(1,1)), Convert.ToInt32(t.Substring(0, 1)), Convert.ToInt32(t.Substring(1, 1))].setconn(connb);
            }

        
             
 
        }

    }
    public class CommandItem
    {
        byte commName;
        public byte CommName
        {
            get { return commName; }
            set { commName = value; }
        }
        public bool ismaster=false;
        public bool isline = true;
        public string Ip
        {
            get
            {
                return ip;
            }
            set
            {
                ip = value;
            }
        }
        public int Port
        {
            get
            {
                return port;
            }
            set
            {
                port = value;
            }
        }

        P2Pclient[,,,] client = new P2Pclient[10, 10, 10, 10];
        public P2Pclient[,,,] Client { get { return client; } set { client = value; } }

        public bool Ismaster { get ; set ; }

        String ip = "";
        int port;
     
    }

    public class clientItem
    {
        int count = 0;
        public void setconn(ConnObj cb)
        {
            IPEndPoint clientipe = (IPEndPoint)cb.Soc.RemoteEndPoint;
            llbb11:
            string key = clientipe.Address.ToString() + ":" + clientipe.Port;
            Connlist.Add(key,cb);
            if (Connlist.ContainsKey(key))
                return;
            else
                goto llbb11;
        }
        public void removeconn(Socket soc)
        {
            IPEndPoint clientipe = (IPEndPoint)soc.RemoteEndPoint;

            string key = clientipe.Address.ToString() + ":" + clientipe.Port;

            //这里通过IP和PORT获取对象
            if (Connlist.ContainsKey(key)) 
                Connlist.Remove(key);
            //ConnObj[] cobs = new ConnObj[Connlist.Count];
            //Connlist.CopyTo(0,cobs,0, cobs.Length);

            //for (int i=0;i< count; i++)
            //{
            //    if(cobs[i]!=null)
            //    if (cobs[i].Soc.Equals(soc))
            //        {
            //            try
            //            {
            //                cobs[i].Soc.Dispose();
            //            }
            //            catch { }
            //            try
            //            {
            //                Connlist.Remove(cobs[i]);
            //            }
            //            catch { }
            //        return;
            //    }
            //}
        }
        
       public ConnObj getconn(String ip, int port)
        {
            string key = ip + ":" + port;

            //这里通过IP和PORT获取对象
            if (Connlist.ContainsKey(key))
                return Connlist[key];
            else
                return null;
            //ConnObj[] cobs = new ConnObj[Connlist.Count];
            //Connlist.CopyTo(0, cobs, 0, cobs.Length);

            //foreach (ConnObj cb in cobs)
            //{
            //    if (cb != null)
            //    {
            //        try
            //        {
            //            if (cb.Soc.RemoteEndPoint != null)
            //            {
            //                IPEndPoint clientipe = (IPEndPoint)cb.Soc.RemoteEndPoint;
            //                if ((clientipe.Address.ToString() + ":" + clientipe.Port) == (ip + ":" + port))
            //                    return cb;
            //            }
            //        }
            //        catch { }
            //    }
            //}
            //return null;
        }
        public ConnObj getconn(Socket soc)
        {
            IPEndPoint clientipe = (IPEndPoint)soc.RemoteEndPoint;

            string key = clientipe.Address.ToString() + ":" + clientipe.Port;

            //这里通过IP和PORT获取对象
            if (Connlist.ContainsKey(key))
                return Connlist[key];
            else
                return null;
            //ConnObj[] cobs = new ConnObj[Connlist.Count];
            //Connlist.CopyTo(0, cobs, 0, cobs.Length);
            //foreach (ConnObj cb in cobs)
            //{
            //    try
            //    {
            //        if(cb!=null)
            //        if (cb.Soc.Equals(soc))
            //            return cb;
            //    }
            //    catch { }
            //}
            //return null;
        }
        Dictionary<String, ConnObj> _Connlist = new Dictionary<String, ConnObj>();

        public Dictionary<string, ConnObj> Connlist { get{ return _Connlist; } set{ _Connlist = value; } }
    }
    public class WayItem
    {
        string _Token;
        int num;
        public string Ip
        {
            get
            {
                return ip;
            }
            set
            {
                ip = value;
            }
        }
        public int Port
        {
            get
            {
                return port;
            }
            set
            {
                port = value;
            }
        }
        public P2Pclient Client
        {
            get
            {
                return client;
            }
            set
            {
                client = value;
            }
        }
        public int Num
        {
            get
            {
                return num;
            }
            set
            {
                num = value;
            }
        }
        public string Token
        {
            get
            {
                return _Token;
            }
            set
            {
                _Token = value;
            }
        }
        String ip = "";
        int port;
        P2Pclient client;
    }
    [Serializable]
    public class ConnObj
    {
        bool _Validation = false;
        int id = 0;
        byte[] makes;
        public byte[] Makes
        {
            get
            {
                return makes;
            }
            set
            {
                makes = value;
            }
        }
        string _Token;
        Socket soc;
        public Socket Soc
        {
            get
            {
                return soc;
            }
            set
            {
                soc = value;
            }
        }
        public string Token
        {
            get
            {
                return _Token;
            }
            set
            {
                _Token = value;
            }
        }
        public int Id
        {
            get
            {
                return id;
            }
            set
            {
                id = value;
            }
        }
        public bool Validation
        {
            get
            {
                return _Validation;
            }
            set
            {
                _Validation = value;
            }
        }
    }
}
