using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Net.WebSockets;
using System.Threading;
using SuperSocket.ClientEngine;
using WebSocket4Net;

namespace Tools
{
   public class WebSocketClient
    {
        private WebSocket4Net.WebSocket WebSocket { get; set; }
        private Thread MainLoop { get; set; }
        private bool Running { get; set; }
        private ConcurrentQueue<String> IncomingMessages { get; } = new ConcurrentQueue<String>();
        private ConcurrentQueue<String> OutgoingMessages { get; } = new ConcurrentQueue<String>();
        public string status = "";
        System.Windows.Threading.DispatcherTimer timer_data_read;
        private int Packet_Receive_TimeOut_Count=0;
        public bool Packet_Available=false;
        public int TimeOut = 20;

        public WebSocketClient(string uri, List<KeyValuePair<string, string>> headers)
        {
            WebSocket = new WebSocket4Net.WebSocket(uri, customHeaderItems: headers);
            WebSocket.EnableAutoSendPing = true;
            WebSocket.AutoSendPingInterval = 60;

            WebSocket.Opened +=             WebSocketOnOpened;
            WebSocket.Error +=              WebSocketOnError;
            WebSocket.Closed +=             WebSocketOnClosed;
            WebSocket.MessageReceived +=    WebSocketOnMessageReceived;

            MainLoop = new Thread(MainLoopThread);
            MainLoop.Start();

            timer_data_read = new System.Windows.Threading.DispatcherTimer();
            timer_data_read.Tick += timer_data_read_Tick;
            timer_data_read.Interval = new TimeSpan(0, 0, 0, 0, 1);
            timer_data_read.Stop();
        }

        private void timer_data_read_Tick(object sender, EventArgs e)
        {
            Packet_Receive_TimeOut_Count++;
            if (Packet_Receive_TimeOut_Count>TimeOut)
            {
                Packet_Available=true;
                if (timer_data_read.IsEnabled) timer_data_read.Stop();
            }
        }

        public void close()
        {
            Running=false;
            if (timer_data_read.IsEnabled) timer_data_read.Stop();
        }

        public void MainLoopThread()
        {
            Running = true;
            while (Running)
            {
                // handle connection state changes if needed
                switch (WebSocket.State)
                {
                    case WebSocket4Net.WebSocketState.Closed:
                    case WebSocket4Net.WebSocketState.None:
                        WebSocket.Open();
                        status="Websocket connecting to Webserver";
                        break;
                    //______________________________________________________
                    case WebSocket4Net.WebSocketState.Open:
                        // send any outgoing queued messages
                        while (OutgoingMessages.TryDequeue(out var message))
                        {
                            SendMessage(message);
                            Thread.Sleep(50);
                        }
                        break;
                    //______________________________________________________
                    case WebSocket4Net.WebSocketState.Closing:
                        status="Websocket is closing ...";
                        break;
                    //______________________________________________________
                    case WebSocket4Net.WebSocketState.Connecting:
                        status="Websocket is connecting ...";
                        break;
                    //______________________________________________________
                }
                // give a few seconds grace time between attempts
                Thread.Sleep(3000);
            }
            MainLoop = null;
            Running = false;
        }

        /// <summary>
        /// Send message immediately
        /// </summary>
        /// <param name="message"></param>
        private void SendMessage(string message)
        {
            try
            {
                WebSocket.Send(message);
                status="Websocket message sent:\n" + message + "\n";
            }
            catch (Exception ex)
            {
                status="Websocket send exception: " + ex.ToString();
            }
        }

        private void WebSocketOnMessageReceived(object sender, MessageReceivedEventArgs e)
        {
            status="Websocket message received:\n" + e.Message + "\n";
            try
            {
                IncomingMessages.Enqueue(e.Message);
                if(!timer_data_read.IsEnabled) timer_data_read.Start();
                Packet_Receive_TimeOut_Count=0;
            }
            catch (Exception ex)
            {
                status="Error processing message from Sinric:\n" + ex + "\n";
            }
        }

        /// <summary>
        /// // Get the received packet
        /// </summary>
        /// <returns> no. of received packets </returns>
        public List<string> Get_Packet()
        {
            List<string> packets=new List<string>();
            while (IncomingMessages.TryDequeue(out var message))
            {
                packets.Add(message.ToString());
            }           
            return packets;
        }

        /// <summary>
        /// Enqueue message thread safe
        /// </summary>
        /// <param name="message"></param>
        public void AddMessageToQueue(string message)
        {
            OutgoingMessages.Enqueue(message);
            status="Queued websocket message for sending";
        }

        private void WebSocketOnClosed(object sender, EventArgs e)
        {
            status="Websocket connection closed";
        }

        private void WebSocketOnOpened(object sender, EventArgs e)
        {
            status="Websocket connection opened";
        }

        private void WebSocketOnError(object sender, ErrorEventArgs e)
        {
            status="Websocket connection error:\n" + e.Exception + "\n";
            if (WebSocket.State ==WebSocket4Net.WebSocketState.Open)
               WebSocket.Close();
        }

    }
}
