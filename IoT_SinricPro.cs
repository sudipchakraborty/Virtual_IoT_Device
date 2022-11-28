using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Net.WebSockets;
using System.Runtime.Serialization;
using System.Security.Cryptography;
using System.Text;
using System.Threading;

using ConsoleExampleCore;

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

using SinricLibrary;
//using SinricLibrary.Devices;
using SuperSocket.ClientEngine;
using WebSocket4Net;


namespace Tools
{
    public class IoT_SinricPro
    {
        // The following Package need to be installed using NuGet Package Manager
        // <1> Newtonsoft.Json
        // <2> WebSocket4Net
        // The package not required now. In the previous module it was required
        // Microsoft.Extensions.DependencyInjection
        //Microsoft.Extensions.Configuration.Json
        //Microsoft.Extensions.Configuration.FileExtensions

        SinricMessage2 temp = new SinricMessage2();
        public string SinricAddress { get; set; } = "ws://ws.sinric.pro";
        private string SecretKey { get; set; }
        private WebSocket4Net.WebSocket WebSocket { get; set; }
        private Thread MainLoop { get; set; }
        private bool Running { get; set; }

        private ConcurrentQueue<SinricMessage2> IncomingMessages { get; } = new ConcurrentQueue<SinricMessage2>();
        private ConcurrentQueue<SinricMessage2> OutgoingMessages { get; } = new ConcurrentQueue<SinricMessage2>();
   //     private Dictionary<string, SinricDeviceBase> Devices { get; set; } = new Dictionary<string, SinricDeviceBase>(StringComparer.OrdinalIgnoreCase);
   //     public SinricSmartLock SmartLocks(string name) => (SinricSmartLock)Devices[name];
   //     public SinricContactSensor ContactSensors(string name) => (SinricContactSensor)Devices[name];

        public IoT_SinricPro()
        {
            SecretKey="8f41d53e-0173-48b9-bfc0-fc254e730d42-73c800c7-b08d-4938-b4d1-988585a20aa1";
            var headers3 = new List<KeyValuePair<string, string>>();

            KeyValuePair<string, string> app_key = new KeyValuePair<string, string>("appkey", "4605c221-5d30-494d-836f-42db372373de");
            KeyValuePair<string, string> dev_ids = new KeyValuePair<string, string>("deviceids", "6369fcc9333d12dd2ae9b09c;6369fc7eb8a7fefbd63538a5;6369fbc5b8a7fefbd63535fc");
            KeyValuePair<string, string> env = new KeyValuePair<string, string>("platform", "csharp");
            KeyValuePair<string, string> dev_state = new KeyValuePair<string, string>("restoredevicestates", "true");

            headers3.Add(app_key);
            headers3.Add(dev_ids);
            headers3.Add(env);
            headers3.Add(dev_state);

            WebSocket = new WebSocket4Net.WebSocket("ws://ws.sinric.pro", customHeaderItems: headers3);
            WebSocket.EnableAutoSendPing = true;
            WebSocket.AutoSendPingInterval = 60;

            WebSocket.Opened += WebSocketOnOpened;
            WebSocket.Error += WebSocketOnError;
            WebSocket.Closed += WebSocketOnClosed;
            WebSocket.MessageReceived += WebSocketOnMessageReceived;

            MainLoop = new Thread(MainLoopThread2);
            MainLoop.Start();
        }

        public void MainLoopThread2()
        {
            Running=true;
            while (Running)
            {
                // handle connection state changes if needed
                switch (WebSocket.State)
                {
                    case WebSocket4Net.WebSocketState.Closed:
                    case WebSocket4Net.WebSocketState.None:
                        WebSocket.Open();         //OpenAsync();
                        Debug.Print($"Websocket connecting to {SinricAddress}");
                        break;

                    case WebSocket4Net.WebSocketState.Open:
                        // check devices for new outgoing messages
                       // SignAndQueueOutgoingMessages();

                        // send any outgoing queued messages
                        while (OutgoingMessages.TryDequeue(out var message))
                        {
                            SendMessage(message);
                            Thread.Sleep(50);
                        }

                        break;

                    case WebSocket4Net.WebSocketState.Closing:
                        Debug.Print("Websocket is closing ...");
                        break;

                    case WebSocket4Net.WebSocketState.Connecting:
                        Debug.Print("Websocket is connecting ...");
                        break;
                }

                // give a few seconds grace time between attempts
                Thread.Sleep(3000);
            }

            MainLoop = null;
            Running = false;
        }


        /// <summary>
        /// Called from the main thread
        /// </summary>
        public void SignAndQueueOutgoingMessages()
        {
          //  foreach (var device in Devices.Values)
           // {
                //// take messages off the device queues
                //while (device.OutgoingMessages.TryDequeue(out var message))
                //{
                //    // sign them, and add to the outgoing queue for processing
                //    AddMessageToQueue(message);
                //}
           // }
        }

        /// <summary>
        /// Send message immediately
        /// </summary>
        /// <param name="message"></param>
        private void SendMessage(SinricMessage2 message)
        {
            try
            {
                // serialize the message to json
                var json = JsonConvert.SerializeObject(message);
                WebSocket.Send(json);
                Debug.Print("Websocket message sent:\n" + json + "\n");
            }
            catch (Exception ex)
            {
                Debug.Print("Websocket send exception: " + ex);
            }
        }

        private void WebSocketOnMessageReceived(object sender, MessageReceivedEventArgs e)
        {
            Console.WriteLine("Websocket message received:\n" + e.Message + "\n");
            try
            {
                var message = JsonConvert.DeserializeObject<SinricMessage2>(e.Message);

           //     if (!HmacSignature2.ValidateMessageSignature(message, SecretKey))
           //         throw new Exception(
            //            "Computed signature for the payload does not match the signature supplied in the message. Message may have been tampered with.");

                // add to the incoming message queue. caller will retrieve the messages on their own thread
                IncomingMessages.Enqueue(message);
            }
            catch (Exception ex)
            {
                Debug.Print("Error processing message from Sinric:\n" + ex + "\n");
            }
        }

        public void ProcessIncomingMessages()
        {
            while (IncomingMessages.TryDequeue(out var message))
            {
                if (message.Payload == null)
                    continue;

                try
                {



               //     var device = Devices.Values.FirstOrDefault(d => d.DeviceId == message.Payload.DeviceId);

               //     if (device == null)
               //         Debug.Print("Received message for unrecognized device:\n" + message.Payload.DeviceId);
               //     else
              //      {
                        // pass in a pre-generated reply, default to fail
                        //     var reply = CreateReplyMessage(message, SinricPayload.Result.Fail);

                        /////////////////////////////////////////////
                        var reply2 = new SinricMessage2();
                        reply2.TimestampUtc = DateTime.UtcNow;
                        reply2.Payload.CreatedAtUtc = DateTime.UtcNow;

                        reply2.Payload.Type = SinricPayload.MessageType.Response;
                        reply2.Payload.Message = SinricPayload.Messages.Ok;
                        reply2.Payload.DeviceId = message.Payload.DeviceId;
                        reply2.Payload.ReplyToken = message.Payload.ReplyToken;
                        reply2.Payload.Action = message.Payload.Action;
                        reply2.Payload.Success = false;
                        ////////////////////////////////////////////////
                     
                        var id = message.Payload.DeviceId;
                        var v = message.Payload.Action;
                        var newState = message.Payload.GetValue<string>(SinricValue.State);
                     
                        reply2.Payload.SetState(newState);
                        reply2.Payload.Success = true;

                        temp=message;

                        // client will take an action and update the reply
                        //    device.MessageReceived(message, reply2);

                    // send the reply to the server
                    AddMessageToQueue2(reply2);
                   // }
                }
                catch (Exception ex)
                {
                    Debug.Print($"SinricClient.ProcessNewMessages for device {message.Payload.DeviceId} exception: \n" + ex);
                }

                Thread.Sleep(50);
            }
        }

        public void send_to_server(string device_id, string state)
        {
            var reply = new SinricMessage2();
 
            reply.TimestampUtc = DateTime.UtcNow;
            reply.Payload.CreatedAtUtc = DateTime.UtcNow;
            reply.Payload.Message = "Light is ON";// SinricPayload.Messages.
            reply.Payload.Success = true;


            reply.Payload.Action = "setPowerState";
            reply.Payload.SetCause("type", "PHYSICAL_INTERACTION");// = SinricCause.CauseType.GetType("type");        // = SinricCause.PhysicalInteraction.ToString();

            //   reply.Payload.CreatedAt = 0;
            reply.Payload.DeviceId = device_id;
            reply.Payload.ReplyToken =Utility.MessageID();// "edf31f4e-4027-4f28-9b61-1a597f986ceb";// message.Payload.ReplyToken;
            reply.Payload.Type = SinricPayload.MessageType.Event;
            reply.Payload.SetValue("state", state);
            ////////////////////////////////////////////////
            send_OK(reply);
           
        }

        /// <summary>
        /// Create common packet structure
        /// </summary>
        /// <returns> return packet structure </returns>
        public SinricMessage2 Create_Packet()
        {
            var pkt = new SinricMessage2();

            pkt.TimestampUtc = DateTime.UtcNow;
            pkt.Payload.CreatedAtUtc = DateTime.UtcNow;
            pkt.Payload.Message ="";
            pkt.Payload.Success = true;

            pkt.Payload.Action = "";
            pkt.Payload.SetCause("type", "PHYSICAL_INTERACTION");// = SinricCause.CauseType.GetType("type");        // = SinricCause.PhysicalInteraction.ToString();

            pkt.Payload.DeviceId = "";
            pkt.Payload.ReplyToken = Utility.MessageID(); 
            pkt.Payload.Type = "response";//  SinricPayload.MessageType.Event; //"event", "response", "request";
            pkt.Payload.SetValue("brightness", 5000);

            return pkt;
        }

        /// <summary>
        /// create a function template
        /// </summary>
        /// <param name="device_id"></param>
        /// <param name="state"></param>
        /// <param name="action"></param>
        /// <param name="type"></param>
        /// <param name="val"></param>
        /// <returns> Return packet object</returns>
        public SinricMessage2 Get_Packet(string device_id,string action,string type, object val)      
        {
            var reply = new SinricMessage2();

            reply.TimestampUtc = DateTime.UtcNow;
            reply.Payload.CreatedAtUtc = DateTime.UtcNow;
            reply.Payload.Success = true;
            reply.Payload.SetCause("type", "PHYSICAL_INTERACTION");
            reply.Payload.ReplyToken = Utility.MessageID();
            reply.Payload.Type = "response";
            reply.Payload.Message = "OK"; 
            
            reply.Payload.DeviceId = device_id;
            reply.Payload.Action = action;
            reply.Payload.SetValue(type, val);
           
            return reply;           
    }

        /// <summary>
        /// Continous Update to IoT server 
        /// Example:-Periodic_Update("6369fbc5b8a7fefbd63535fc" ,"currentTemperature" , "temperature", 23);
        /// </summary>
        /// <param name="device_id"></param>
        /// <param name="action"></param>
        /// <param name="param">parameter to Update</param>
        /// <param name="val">parameter value</param>
        public void Periodic_Update(string device_id, string action, string param, object val)
        {
            var reply = new SinricMessage2();

            reply.TimestampUtc = DateTime.UtcNow;
            reply.Payload.CreatedAtUtc = DateTime.UtcNow;
            reply.Payload.Success = true;
            reply.Payload.SetCause("type", "PERIODIC_POLL");
            reply.Payload.ReplyToken = Utility.MessageID();
            reply.Payload.Type = "event";
            reply.Payload.Message = "OK";

            reply.Payload.DeviceId = device_id;
            reply.Payload.Action = action;
            reply.Payload.SetValue(param, val);

            send_OK(reply);
        }
       
        public void Smart_Bulb_Colour(string device_id, string action, string type, object val)
        {
            var reply = temp;

            reply.TimestampUtc = DateTime.UtcNow;
            reply.Payload.CreatedAtUtc = DateTime.UtcNow;
            reply.Payload.Success = true;
            reply.Payload.SetCause("type", "PHYSICAL_INTERACTION");
            reply.Payload.ReplyToken = Utility.MessageID();
            reply.Payload.Type = "response";
            reply.Payload.Message = "OK";

            string s = reply.Payload.Value.ToString();
            reply.Payload.DeviceId = device_id;
      //      reply.Payload.Action = action;
      //      reply.Payload.SetValue(type, val);

            send_OK(reply);
        }

        /// <summary>
        /// The data send to the remote server
        /// </summary>
        /// <param name="reply"></param>
        /// <returns>if send success, return ok, else return false </returns>
        public bool send_OK(SinricMessage2 reply)
        {
            var payloadJson = JsonConvert.SerializeObject(reply.Payload);
            reply.RawPayload = new JRaw(payloadJson);

            // compute the signature using our secret key so that the service can verify authenticity
            reply.Signature.Hmac = HmacSignature.Signature(payloadJson, SecretKey);

            try
            {
                // serialize the message to json
                var json = JsonConvert.SerializeObject(reply);
                WebSocket.Send(json);
                return true;
            }
            catch (Exception ex)
            {
                return false;
            }
        }

        /// <summary>
        /// Enqueue message thread safe
        /// </summary>
        /// <param name="message"></param>
        public void AddMessageToQueue2(SinricMessage2 message)
        {
            var payloadJson = JsonConvert.SerializeObject(message.Payload);
            message.RawPayload = new JRaw(payloadJson);

            // compute the signature using our secret key so that the service can verify authenticity
            message.Signature.Hmac = HmacSignature.Signature(payloadJson, SecretKey);

            OutgoingMessages.Enqueue(message);
            Debug.Print("Queued websocket message for sending");
        }

        private void WebSocketOnClosed(object sender, EventArgs e)
        {
            Debug.Print("Websocket connection closed");
        }

        private void WebSocketOnOpened(object sender, EventArgs e)
        {
            Debug.Print("Websocket connection opened");
        }

        private void WebSocketOnError(object sender, ErrorEventArgs e)
        {
            Debug.Print("Websocket connection error:\n" + e.Exception + "\n");

         //   if (WebSocket.State == WebSocketState.Open)
             //   WebSocket.Close();
        }

    }// class: myIoT

}// ns



















namespace ConsoleExampleCore
{



    public static class HmacSignature
    {
        public static string Signature(string payload, string secret)
        {
            var hmac256 = new HMACSHA256(Encoding.UTF8.GetBytes(secret));
            var hash = hmac256.ComputeHash(Encoding.UTF8.GetBytes(payload));

            return Convert.ToBase64String(hash);
        }

        internal static bool ValidateMessageSignature(SinricMessage2 message, string secretKey)
        {
            var payloadString = message.RawPayload?.Value as string;

            if (!string.IsNullOrEmpty(payloadString))
            {
                // if the message contains a payload then we need to validate its signature

                // todo validate timestamp of message, must be within X seconds of local clock, and must be > than the last message time received to protect against replay attacks

                // compute a local signature from the raw payload using our secret key:
                var signature = HmacSignature.Signature(payloadString, secretKey);

                // compare the locally computed signature with the one supplied in the message:
                return signature == message.Signature.Hmac;
            }

            return true;
        }
    }



    public class SinricValue
    {
        public const string State = "state";

        // misc fields
        [JsonExtensionData]
        public IDictionary<string, JToken> Fields { get; set; } = new Dictionary<string, JToken>();
    }

    public class SinricCause
    {
        public const string PhysicalInteraction = "PHYSICAL_INTERACTION";
        public const string CauseType = "type";

        // misc fields
        [JsonExtensionData]
        public IDictionary<string, JToken> Fields { get; set; } = new Dictionary<string, JToken>();
    }









    public class SinricPayload
    {
        public class Client
        {
            public const string Csharp = "csharp";
        }

        public class Messages
        {
            public const string Ok = "OK";
        }

        public class Result
        {
            public const bool Fail = false;
            public const bool Success = true;
        }

        public class MessageType
        {
            public const string Event = "event";
            public const string Response = "response";
            public const string Request = "request";
        }

        [JsonProperty("type")]
        public string Type { get; set; }

        [JsonProperty("action")]
        public string Action { get; set; }

        [JsonProperty("clientId")]
        public string ClientId { get; set; } = Client.Csharp;

        [JsonProperty("createdAt")]
        public uint CreatedAt { get; set; }

        [JsonIgnore]
        public DateTime CreatedAtUtc
        {
            get => Utility.ConvertFromUnixTimestamp(CreatedAt);
            set => CreatedAt = Utility.ConvertToUnixTimestamp(DateTime.Now);
        }

        [JsonProperty("deviceAttributes")]
        public List<object> DeviceAttributes { get; set; } = new List<object>();

        [JsonProperty("deviceId")]
        public string DeviceId { get; set; }

        [JsonProperty("replyToken")]
        public string ReplyToken { get; set; }

        [JsonProperty("value")]
        public SinricValue Value { get; set; } = new SinricValue();

        [JsonProperty("message")]
        public string Message { get; set; }

        [JsonProperty("success")]
        public bool Success { get; set; }

        /// <summary>
        /// Returns the requested value from the available list, if present on the message.
        /// </summary>
        /// <typeparam name="T">Expected data type</typeparam>
        /// <param name="key">The name of the value to retrieve</param>
        /// <returns>The value of type T if exists; otherwise null.</returns>
        public T GetValue<T>(string key) where T : class
        {
            Value.Fields.TryGetValue(key, out var value);

            return value?.Value<T>();
        }

        public SinricPayload SetValue(string key, object value)
        {
            if (value != null)
                Value.Fields[key] = JToken.FromObject(value);
            else
                Value.Fields[key] = null;

            return this;
        }

        public void SetState(string newState)
        {
            SetValue(SinricValue.State, newState);
        }

        [JsonProperty("cause")]
        public SinricCause Cause { get; set; } = new SinricCause();


        /// <summary>
        /// Returns the requested value from the available list, if present on the message.
        /// </summary>
        /// <typeparam name="T">Expected data type</typeparam>
        /// <param name="key">The name of the value to retrieve</param>
        /// <returns>The value of type T if exists; otherwise null.</returns>
        public T GetCause<T>(string key) where T : class
        {
            Cause.Fields.TryGetValue(key, out var cause);

            return cause?.Value<T>();
        }

        public SinricPayload SetCause(string key, object cause)
        {
            if (cause != null)
                Cause.Fields[key] = JToken.FromObject(cause);
            else
                Cause.Fields[key] = null;

            return this;
        }


        public static DateTime UnixTimeStampToDateTime(double unixTimeStamp)
        {
            // Unix timestamp is seconds past epoch
            DateTime dateTime = new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc);
            dateTime = dateTime.AddSeconds(unixTimeStamp).ToLocalTime();
            return dateTime;
        }


    }






    /// <summary>
    /// ////////////////////////////////////////////////////////////////////////////////////////////
    /// </summary>
    public class SinricMessage2
    {

        [JsonProperty("timestamp")]
        public uint Timestamp { get; set; }

        [JsonIgnore]
        public DateTime TimestampUtc // { get; set; }
        {
            get => Utility.ConvertFromUnixTimestamp(Timestamp);
            set => Timestamp = Utility.ConvertToUnixTimestamp(DateTime.Now);
        }

        // other fields
        [JsonExtensionData]
        public IDictionary<string, JToken> Fields { get; set; } = new Dictionary<string, JToken>();

        [JsonProperty("header")]
        public SinricHeader Header { get; set; } = new SinricHeader();

        // Raw payload needed for computing signature
        [JsonProperty("payload")]
        public JRaw RawPayload { get; set; }

        // Deserialized version of payload
        [JsonIgnore]
        public SinricPayload Payload { get; set; } = new SinricPayload();

        [OnDeserialized]
        private void OnDeserializedMethod(StreamingContext context)
        {
            Payload = JsonConvert.DeserializeObject<SinricPayload>(RawPayload?.Value as string ?? "");
        }

        [JsonProperty("signature")]
        public SinricSignature Signature { get; set; } = new SinricSignature();
    }

    public class SinricSignature
    {
        [JsonProperty("HMAC")]
        public string Hmac { get; set; }
    }


    public class SinricHeader
    {
        [JsonProperty("payloadVersion")]
        public int PayloadVersion { get; set; } = 2;

        [JsonProperty("signatureVersion")]
        public int SignatureVersion { get; set; } = 1;
    }
}









namespace SinricLibrary
{
    public static class Utility
    {
        // https://stackoverflow.com/questions/11743160/how-do-i-encode-and-decode-a-base64-string
        public static string Base64Encode(this string plainText)
        {
            var plainTextBytes = Encoding.UTF8.GetBytes(plainText);
            return System.Convert.ToBase64String(plainTextBytes);
        }

        public static string Base64Decode(this string base64EncodedData)
        {
            var base64EncodedBytes = Convert.FromBase64String(base64EncodedData);
            return System.Text.Encoding.UTF8.GetString(base64EncodedBytes);
        }

        public static string aDescriptionAttr<T>(this T source)
        {
            if (source is Type sourceType)
            {
                // attribute for a class, struct or enum
                var attributes = (DescriptionAttribute[])sourceType.GetCustomAttributes(typeof(DescriptionAttribute), false);

                if (attributes.Length > 0)
                    return attributes[0].Description;
            }
            else
            {
                // attribute for a member field
                var fieldInfo = source.GetType().GetField(source.ToString());
                var attributes = (DescriptionAttribute[])fieldInfo.GetCustomAttributes(typeof(DescriptionAttribute), false);

                if (attributes.Length > 0)
                    return attributes[0].Description;
            }

            return source.ToString();
        }

        public static string MessageID()
        {
            string _id = "";
            for (byte i = 0; i < 16; i++)
            {
                Random random = new Random();
                byte rnd = (byte)random.Next(1, 255);

                if (i == 4) _id += "-";
                if (i == 6) { _id += "-"; rnd = (byte)(0x40 | (0x0F & rnd)); } // 0100xxxx to set version 4
                if (i == 8) { _id += "-"; rnd = (byte)(0x80 | (0x3F & rnd)); } // 10xxxxxx to set reserved bits
                if (i == 10) _id += "-";
                byte high_nibble = (byte)(rnd >> 4);
                byte low_nibble = (byte)(rnd & 0x0f);
                _id += "0123456789abcdef"[high_nibble];
                _id += "0123456789abcdef"[low_nibble];
            }
            return _id;
        }

        public static DateTime ConvertFromUnixTimestamp(uint timestamp)
        {
            DateTime origin = new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc);
            return origin.AddSeconds(timestamp);
        }

        public static uint ConvertToUnixTimestamp(DateTime date)
        {
            DateTime origin = new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc);
            TimeSpan diff = date.ToUniversalTime() - origin;
            return (uint)(Math.Floor(diff.TotalSeconds));
        }
    }
}

































//using System;
//using System.Collections.Generic;
//using System.ComponentModel;
//using System.Data;
//using System.Drawing;
//using System.Linq;
//using System.Text;
//using System.Threading;
//using System.Threading.Tasks;
//using System.Windows.Forms;

//using ConsoleExampleCore;

//namespace IoT
//{
//    public partial class Form1 : Form
//    {
//        myIoT iot = new myIoT();
//        public Form1()
//        {
//            InitializeComponent();
//        }

//        private void Form1_Load(object sender, EventArgs e)
//        {

//            //    iot.ProcessIncomingMessages();
//            //   Thread.Sleep(75);

//            //if (i==50)
//            //{
//            //    i=0;

//            //   it.send_OK(it.Get_Packet("6369fc7eb8a7fefbd63538a5", "setBrightness", "brightness", 20));
//            // it.send_OK(it.Get_Packet("6369fc7eb8a7fefbd63538a5", "setPowerState", "state", "Off"));
//            //     it.send_OK(it.Get_Packet("6369fc7eb8a7fefbd63538a5", "setPowerState", "state", "On"));

//            //     it.send_OK(it.Get_Packet("6369fc7eb8a7fefbd63538a5", "setColorTemperature", "colorTemperature", 1200));

//            //string ss = "{"+
//            //            '"'+"b"+'"'+": "+"1"+','+
//            //            '"'+"g"+'"'+": "+"2"+','+
//            //            '"'+"r"+'"'+": "+"3"+
//            //            "}";


//            //     it.send_OK(it.Get_Packet("6369fc7eb8a7fefbd63538a5", "setColor", "color",ss));

//            //it.Smart_Bulb_Colour("6369fc7eb8a7fefbd63538a5", "setColor", "color", "b:0,g:0,r:2");


//            //     it.Periodic_Update("6369fbc5b8a7fefbd63535fc", "currentTemperature", "humidity", val);
//            //    iot.Periodic_Update("6369fbc5b8a7fefbd63535fc", "currentTemperature", "temperature", 23);
//            //    val++;

//            //  it.send_OK(it.Get_Packet("6369fbc5b8a7fefbd63535fc", "setTEMPERATURESENSOR", "TEMPERATURESENSOR", "10"));


//            // it.send_to_server2("", "");
//            //it.send_to_server("6369fcc9333d12dd2ae9b09c", s);

//            //if (s=="On") s="Off"; else s= "On";

//        }

//        private void timer1_Tick(object sender, EventArgs e)
//        {
//            iot.ProcessIncomingMessages();

//            iot.Periodic_Update("6369fbc5b8a7fefbd63535fc", "currentTemperature", "temperature", 23);
//        }
//    }
//    /////////////////////////////////////////


//}

