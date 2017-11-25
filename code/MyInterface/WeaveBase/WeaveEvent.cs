﻿using System.Net;
using System.Net.Sockets;
namespace WeaveBase
{
    public  class WeaveEvent
        {
            byte command;
            public byte Command
            {
                get { return command; }
                set { command = value; }
            }
            byte[] masks;
            string data;
            public string Data
            {
                get { return data; }
                set { data = value; }
            }
            Socket soc;
            public Socket Soc
            {
                get { return soc; }
                set { soc = value; }
            }
            public byte[] Masks
            {
                get
                {
                    return masks;
                }
                set
                {
                    masks = value;
                }
            }
            public byte[] Databit
            {
                get
                {
                    return databit;
                }
                set
                {
                    databit = value;
                }
            }
            byte[] databit;
        public EndPoint Ep { get; set; }
    }
}