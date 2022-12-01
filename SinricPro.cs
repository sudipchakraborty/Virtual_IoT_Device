using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Net.WebSockets;
using System.Runtime.Intrinsics.X86;
using System.Runtime.Serialization;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Windows.Forms;

using Json.Net;

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using SuperSocket.ClientEngine;

using Tools;

// The following Package need to be installed using NuGet Package Manager
// <1> Newtonsoft.Json
// <2> WebSocket4Net
//////////////////////
namespace IoT_SinricPro
{
    public class SinricPro
    {
       // private string SecretKey { get; set; }
        private ConcurrentQueue<Packet> IncomingMessages { get; } = new ConcurrentQueue<Packet>();
        private ConcurrentQueue<Packet> OutgoingMessages { get; } = new ConcurrentQueue<Packet>();
        WebSocketClient comm_channel;
        json_handler file;
        string uri, SecretKey, App_KEY, door, smart_bulb, wall_thermometer, dev_Ids;

        public SinricPro()
         {                
            file = new json_handler(@"..\..\..\settings.json");

            uri = file.Get("uri");
            SecretKey= file.Get("SecretKey");
            App_KEY = file.Get("App_Key");

            door = file.Get("DOOR");
            smart_bulb = file.Get("Smart_LED_Bulb");
            wall_thermometer= file.Get("Wall_Thermometer");
            dev_Ids=door+';'+smart_bulb+";"+wall_thermometer;    
             
            var headers = new List<KeyValuePair<string, string>>();

            KeyValuePair<string, string> app_key = new KeyValuePair<string, string>("appkey", App_KEY);           
            KeyValuePair<string, string> dev_ids = new KeyValuePair<string, string>("deviceids",dev_Ids);
            KeyValuePair<string, string> env = new KeyValuePair<string, string>("platform", "csharp");
            KeyValuePair<string, string> dev_state = new KeyValuePair<string, string>("restoredevicestates", "true");

            headers.Add(app_key);
            headers.Add(dev_ids);
            headers.Add(env);
            headers.Add(dev_state);

            comm_channel=new WebSocketClient(uri, headers);          
        }

        /// <summary>
        /// check command available, if received, read it
        /// </summary>
        /// <param name="commands"></param>
        /// <returns></returns>
        public bool command_available(ref List<KeyValuePair<string, string>> commands)
        {
            if(comm_channel.Packet_Available)
            {
              var receive_packets = comm_channel.Get_Packet();

                foreach(string s in receive_packets)
                {
                    var message = JsonConvert.DeserializeObject<Packet>(s);
                    if (message.Payload!=null)
                    {
                        string id = message.Payload.DeviceId.ToString();                      
                        var pld = message.Payload.Value;
                        var k = pld.Fields.Keys.FirstOrDefault();
                        var v = pld.Fields.Values.FirstOrDefault();
                        KeyValuePair<string, string> val = new KeyValuePair<string, string>(id,k.ToString()+","+ v.ToString());
                        commands.Add(val); 
                    }
                }
                    comm_channel.Packet_Available=false;
                    return true;
            }
            else
            {
                return false;
            }
        }

        public void send_ack_to_server(string device_id, string param, string value)
        {
            Packet pkt = Get_Packet(device_id, "SetPowerState", param, value);
            var payloadJson = JsonConvert.SerializeObject(pkt.Payload);
            pkt.RawPayload = new JRaw(payloadJson);
            pkt.Signature.Hmac = HmacSignature.Signature(payloadJson, SecretKey);

            var json = JsonConvert.SerializeObject(pkt);
            comm_channel.AddMessageToQueue(json);
        }

        public object Get_Json_Colour(string b, string g, string r)
        {
            color c = new color();
            c.b=Convert.ToByte(b);
            c.g=Convert.ToByte(g);
            c.r=Convert.ToByte(r);

            var jColour = JsonConvert.SerializeObject(c);
            return jColour;
        }

        public void send_ack_to_server(string device_id, string state) 
        {
            Packet pkt = Get_Packet(device_id, "SetPowerState", "state", state);
            var payloadJson = JsonConvert.SerializeObject(pkt.Payload);
            pkt.RawPayload = new JRaw(payloadJson);
            pkt.Signature.Hmac = HmacSignature.Signature(payloadJson, SecretKey);

            var json = JsonConvert.SerializeObject(pkt);
            comm_channel.AddMessageToQueue(json);
        }
       
        public void send_to_server(string device_id, string action, string param, object val)
        {
            Packet pkt = Get_Packet(device_id, action, param, val);
            pkt.Payload.SetCause("type", "PERIODIC_POLL");
            pkt.Payload.Type = "event";
           
            var payloadJson = JsonConvert.SerializeObject(pkt.Payload);
            pkt.RawPayload = new JRaw(payloadJson);
            pkt.Signature.Hmac = HmacSignature.Signature(payloadJson, SecretKey);

            var json = JsonConvert.SerializeObject(pkt);
            comm_channel.AddMessageToQueue(json);
        }


















        /// <summary>
        /// Continous Update to IoT server 
        /// Example:-Periodic_Update("6369fbc5b8a7fefbd63535fc" ,"currentTemperature" , "temperature", 23);
        /// </summary>
        /// <param name="device_id"></param>
        /// <param name="action"></param>
        /// <param name="param">parameter to Update</param>
        /// <param name="val">parameter value</param>
        //public void Periodic_Update(string device_id, string action, string param, object val)
        //{
        //    var reply = new Packet();

        //    reply.TimestampUtc = DateTime.UtcNow;
        //    reply.Payload.CreatedAtUtc = DateTime.UtcNow;
        //    reply.Payload.Success = true;
        //    reply.Payload.SetCause("type", "PERIODIC_POLL");
        //    reply.Payload.ReplyToken = Util_IoT.MessageID();
        //    reply.Payload.Type = "event";
        //    reply.Payload.Message = "OK";

        //    reply.Payload.DeviceId = device_id;
        //    reply.Payload.Action = action;
        //    reply.Payload.SetValue(param, val);

        //    send_OK(reply);
        //}







        /// <summary>
        /// create a function template
        /// </summary>
        /// <param name="device_id"></param>
        /// <param name="state"></param>
        /// <param name="action"></param>
        /// <param name="type"></param>
        /// <param name="val"></param>
        /// <returns> Return packet object</returns>
        public Packet Get_Packet(string device_id, string action, string type, object val)
        {
            var reply = new Packet();

            reply.TimestampUtc = DateTime.UtcNow;
            reply.Payload.CreatedAtUtc = DateTime.UtcNow;
            reply.Payload.Success = true;
            reply.Payload.SetCause("type", "PHYSICAL_INTERACTION");
            reply.Payload.ReplyToken = Util_IoT.MessageID();
            reply.Payload.Type = "response";
            reply.Payload.Message = "OK";

            reply.Payload.DeviceId = device_id;
            reply.Payload.Action = action;
            reply.Payload.SetValue(type, val);

            return reply;
        }




        /// <summary>
        /// Called from the main thread
        ///// </summary>
        //public void SignAndQueueOutgoingMessages()
        //{
        //    //  foreach (var device in Devices.Values)
        //    // {
        //    //// take messages off the device queues
        //    //while (device.OutgoingMessages.TryDequeue(out var message))
        //    //{
        //    //    // sign them, and add to the outgoing queue for processing
        //    //    AddMessageToQueue(message);
        //    //}
        //    // }
        //}

        /// <summary>
        /// Send message immediately
        /// </summary>
        /// <param name="message"></param>
        //private void SendMessage(Packet message)
        //{
        //    try
        //    {
        //        // serialize the message to json
        //        var json = JsonConvert.SerializeObject(message);
        //        WebSocket.Send(json);
        //       // Debug.Print("Websocket message sent:\n" + json + "\n");
        //    }
        //    catch (Exception ex)
        //    {
        //       // Debug.Print("Websocket send exception: " + ex);
        //    }
        //}

        //private void WebSocketOnMessageReceived(object sender, MessageReceivedEventArgs e)
        //{
        //    Console.WriteLine("Websocket message received:\n" + e.Message + "\n");
        //    try
        //    {
        //        var message = JsonConvert.DeserializeObject<Packet>(e.Message);

        //        //     if (!HmacSignature2.ValidateMessageSignature(message, SecretKey))
        //        //         throw new Exception(
        //        //            "Computed signature for the payload does not match the signature supplied in the message. Message may have been tampered with.");

        //        // add to the incoming message queue. caller will retrieve the messages on their own thread
        //        IncomingMessages.Enqueue(message);
        //    }
        //    catch (Exception ex)
        //    {
        //        Debug.Print("Error processing message from Sinric:\n" + ex + "\n");
        //    }
        //}

        //public void ProcessIncomingMessages()
        //{
        //    while (IncomingMessages.TryDequeue(out var message))
        //    {
        //        if (message.Payload == null)
        //            continue;

        //        try
        //        {



        //            //     var device = Devices.Values.FirstOrDefault(d => d.DeviceId == message.Payload.DeviceId);

        //            //     if (device == null)
        //            //         Debug.Print("Received message for unrecognized device:\n" + message.Payload.DeviceId);
        //            //     else
        //            //      {
        //            // pass in a pre-generated reply, default to fail
        //            //     var reply = CreateReplyMessage(message, SinricPayload.Result.Fail);

        //            /////////////////////////////////////////////
        //            var reply = new Packet();
        //            reply.TimestampUtc = DateTime.UtcNow;
        //            reply.Payload.CreatedAtUtc = DateTime.UtcNow;

        //            reply.Payload.Type = Payload.MessageType.Response;
        //            reply.Payload.Message = Payload.Messages.Ok;
        //            reply.Payload.DeviceId = message.Payload.DeviceId;
        //            reply.Payload.ReplyToken = message.Payload.ReplyToken;
        //            reply.Payload.Action = message.Payload.Action;
        //            reply.Payload.Success = false;
        //            ////////////////////////////////////////////////

        //            var id = message.Payload.DeviceId;
        //            var v = message.Payload.Action;
        //            var newState = message.Payload.GetValue<string>(SinricValue.State);

        //            reply.Payload.SetState(newState);
        //            reply.Payload.Success = true;

        //            Packet temp = new Packet();
        //            temp = message;

        //            // client will take an action and update the reply
        //            //    device.MessageReceived(message, reply2);

        //            // send the reply to the server
        //            AddMessageToQueue(reply);
        //            // }
        //        }
        //        catch (Exception ex)
        //        {
        //            Debug.Print($"SinricClient.ProcessNewMessages for device {message.Payload.DeviceId} exception: \n" + ex);
        //        }

        //        Thread.Sleep(50);
        //    }
        //}

        //public void send_to_server(string device_id, string state)
        //{
        //    var reply = new Packet();

        //    reply.TimestampUtc = DateTime.UtcNow;
        //    reply.Payload.CreatedAtUtc = DateTime.UtcNow;
        //    reply.Payload.Message = "Light is ON";// SinricPayload.Messages.
        //    reply.Payload.Success = true;


        //    reply.Payload.Action = "setPowerState";
        //    reply.Payload.SetCause("type", "PHYSICAL_INTERACTION");// = SinricCause.CauseType.GetType("type");        // = SinricCause.PhysicalInteraction.ToString();

        //    //   reply.Payload.CreatedAt = 0;
        //    reply.Payload.DeviceId = device_id;
        //    reply.Payload.ReplyToken = Util_IoT.MessageID();// "edf31f4e-4027-4f28-9b61-1a597f986ceb";// message.Payload.ReplyToken;
        //    reply.Payload.Type = Payload.MessageType.Event;
        //    reply.Payload.SetValue("state", state);
        //    ////////////////////////////////////////////////
        //    send_OK(reply);

        //}

        /// <summary>
        /// Create common packet structure
        /// </summary>
        /// <returns> return packet structure </returns>
        //public Packet Create_Packet()
        //{
        //    var pkt = new Packet();

        //    pkt.TimestampUtc = DateTime.UtcNow;
        //    pkt.Payload.CreatedAtUtc = DateTime.UtcNow;
        //    pkt.Payload.Message = "";
        //    pkt.Payload.Success = true;

        //    pkt.Payload.Action = "";
        //    pkt.Payload.SetCause("type", "PHYSICAL_INTERACTION");// = SinricCause.CauseType.GetType("type");        // = SinricCause.PhysicalInteraction.ToString();

        //    pkt.Payload.DeviceId = "";
        //    pkt.Payload.ReplyToken = Util_IoT.MessageID();
        //    pkt.Payload.Type = "response";//  SinricPayload.MessageType.Event; //"event", "response", "request";
        //    pkt.Payload.SetValue("brightness", 5000);

        //    return pkt;
        //}



        /// <summary>
        /// Continous Update to IoT server 
        /// Example:-Periodic_Update("6369fbc5b8a7fefbd63535fc" ,"currentTemperature" , "temperature", 23);
        /// </summary>
        /// <param name="device_id"></param>
        /// <param name="action"></param>
        /// <param name="param">parameter to Update</param>
        /// <param name="val">parameter value</param>
        //public void Periodic_Update(string device_id, string action, string param, object val)
        //{
        //    var reply = new Packet();

        //    reply.TimestampUtc = DateTime.UtcNow;
        //    reply.Payload.CreatedAtUtc = DateTime.UtcNow;
        //    reply.Payload.Success = true;
        //    reply.Payload.SetCause("type", "PERIODIC_POLL");
        //    reply.Payload.ReplyToken = Util_IoT.MessageID();
        //    reply.Payload.Type = "event";
        //    reply.Payload.Message = "OK";

        //    reply.Payload.DeviceId = device_id;
        //    reply.Payload.Action = action;
        //    reply.Payload.SetValue(param, val);

        //    send_OK(reply);
        //}

        public void Smart_Bulb_Colour(string device_id, string action, string type, object val)
        {
            Packet reply = new Packet();

            reply.TimestampUtc = DateTime.UtcNow;
            reply.Payload.CreatedAtUtc = DateTime.UtcNow;
            reply.Payload.Success = true;
            reply.Payload.SetCause("type", "PHYSICAL_INTERACTION");
            reply.Payload.ReplyToken = Util_IoT.MessageID();
            reply.Payload.Type = "response";
            reply.Payload.Message = "OK";

            string s = reply.Payload.Value.ToString();
            reply.Payload.DeviceId = device_id;
            //      reply.Payload.Action = action;
            //      reply.Payload.SetValue(type, val);

           // send_OK(reply);
        }

        ///// <summary>
        ///// The data send to the remote server
        ///// </summary>
        ///// <param name="reply"></param>
        ///// <returns>if send success, return ok, else return false </returns>
        ////public bool send_OK(Packet reply)
        ////{
        ////    var payloadJson = JsonConvert.SerializeObject(reply.Payload);
        ////    reply.RawPayload = new JRaw(payloadJson);

        ////    // compute the signature using our secret key so that the service can verify authenticity
        ////    reply.Signature.Hmac = HmacSignature.Signature(payloadJson, SecretKey);

        ////    try
        ////    {
        ////        // serialize the message to json
        ////        var json = JsonConvert.SerializeObject(reply);
        ////        WebSocket.Send(json);
        ////        return true;
        ////    }
        ////    catch (Exception ex)
        ////    {
        ////        return false;
        ////    }
        ////}

        ///// <summary>
        ///// Enqueue message thread safe
        ///// </summary>
        ///// <param name="message"></param>
        //public void AddMessageToQueue(Packet message)
        //{
        //    var payloadJson = JsonConvert.SerializeObject(message.Payload);
        //    message.RawPayload = new JRaw(payloadJson);

        //    // compute the signature using our secret key so that the service can verify authenticity
        //    message.Signature.Hmac = HmacSignature.Signature(payloadJson, SecretKey);

        //    OutgoingMessages.Enqueue(message);
        //    Debug.Print("Queued websocket message for sending");
        //}

     

    }




    public class color
    {
        [JsonProperty("b")]
        public byte b { get; set; }

        [JsonProperty("g")]
        public byte g { get; set; }

        [JsonProperty("r")]
        public byte r { get; set; }
    }




        public class Packet
    {
        [JsonProperty("timestamp")]
        public uint Timestamp { get; set; }

        [JsonIgnore]
        public DateTime TimestampUtc // { get; set; }
        {
            get => Util_IoT.ConvertFromUnixTimestamp(Timestamp);
            set => Timestamp = Util_IoT.ConvertToUnixTimestamp(DateTime.Now);
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
        public Payload Payload { get; set; } = new Payload();

        [OnDeserialized]
        private void OnDeserializedMethod(StreamingContext context)
        {
            Payload = JsonConvert.DeserializeObject<Payload>(RawPayload?.Value as string ?? "");
        }

        [JsonProperty("signature")]
        public Signature Signature { get; set; } = new Signature();
    }



    public static class HmacSignature
    {
        public static string Signature(string payload, string secret)
        {
            var hmac256 = new HMACSHA256(Encoding.UTF8.GetBytes(secret));
            var hash = hmac256.ComputeHash(Encoding.UTF8.GetBytes(payload));

            return Convert.ToBase64String(hash);
        }

        internal static bool ValidateMessageSignature(Packet message, string secretKey)
        {
            var payloadString = message.RawPayload?.Value as string;

            if (!string.IsNullOrEmpty(payloadString))
            {
                // if the message contains a payload then we need to validate its signature

                // todo validate timestamp of message, must be within X seconds of local clock, and must be > than the last message time received to protect against replay attacks

                // compute a local signature from the raw payload using our secret key:
                var signature = Signature(payloadString, secretKey);

                // compare the locally computed signature with the one supplied in the message:
                return signature == message.Signature.Hmac;
            }

            return true;
        }
    }






    public class Payload
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
            get => Util_IoT.ConvertFromUnixTimestamp(CreatedAt);
            set => CreatedAt = Util_IoT.ConvertToUnixTimestamp(DateTime.Now);
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

        public Payload SetValue(string key, object value)
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

        public Payload SetCause(string key, object cause)
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
    /// //////////////////////////////////////////////////////////////////////////////////////
    /// </summary>

    public class Signature
    {
        [JsonProperty("HMAC")]
        public string Hmac { get; set; }
    }
 

    public class SinricCause
    {
        public const string PhysicalInteraction = "PHYSICAL_INTERACTION";
        public const string CauseType = "type";

        // misc fields
        [JsonExtensionData]
        public IDictionary<string, JToken> Fields { get; set; } = new Dictionary<string, JToken>();
    }



    public class SinricHeader
    {
        [JsonProperty("payloadVersion")]
        public int PayloadVersion { get; set; } = 2;

        [JsonProperty("signatureVersion")]
        public int SignatureVersion { get; set; } = 1;
    }




    public class SinricValue
    {
        public const string State = "state";

        // misc fields
        [JsonExtensionData]
        public IDictionary<string, JToken> Fields { get; set; } = new Dictionary<string, JToken>();
    }











    public static class Util_IoT
    {

        // https://stackoverflow.com/questions/11743160/how-do-i-encode-and-decode-a-base64-string
        public static string Base64Encode(this string plainText)
        {
            var plainTextBytes = Encoding.UTF8.GetBytes(plainText);
            return Convert.ToBase64String(plainTextBytes);
        }

        public static string Base64Decode(this string base64EncodedData)
        {
            var base64EncodedBytes = Convert.FromBase64String(base64EncodedData);
            return Encoding.UTF8.GetString(base64EncodedBytes);
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
                if (i == 6) { _id += "-"; rnd = (byte)(0x40 | 0x0F & rnd); } // 0100xxxx to set version 4
                if (i == 8) { _id += "-"; rnd = (byte)(0x80 | 0x3F & rnd); } // 10xxxxxx to set reserved bits
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
            return (uint)Math.Floor(diff.TotalSeconds);
        }
    }
















}// ns