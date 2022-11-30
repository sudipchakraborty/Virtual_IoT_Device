using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web; 
using System.IO; // for File operation 
using Json.Net;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Windows.Forms;
using System.Drawing;
/// <summary>
/// //////////////////////////////////////////////////////////////////////////////////////////////
/// </summary>

// from Nuget install "Newtonsoft.Json" and "Json.Net" 

namespace Tools
{
   public class json_handler
    {
        string file_name = "";
        //_____________________________________________________________________________________________________
        public json_handler (string file_name)
        {
            this.file_name = file_name;
        }
        //______________________________________________________________________________________________________
        public string Get(string param)
        {
            if (file_name == "") return "";
            string readResult = string.Empty;
            using (StreamReader r = new StreamReader(file_name))
            {
                var json = r.ReadToEnd();
                var jobj = JObject.Parse(json);
                readResult = jobj.ToString();
                foreach (var item in jobj.Properties())
                {
                    string prm = item.Name;
                    string val = item.Value.ToString();
                    if(prm==param)
                    {
                        return val;
                    }                   
                }
            }
            return "";
        }

        public Int16 Get_int(string param)
        {
            if (file_name == "") return 0;
            string readResult = string.Empty;
            using (StreamReader r = new StreamReader(file_name))
            {
                var json = r.ReadToEnd();
                var jobj = JObject.Parse(json);
                readResult = jobj.ToString();
                foreach (var item in jobj.Properties())
                {
                    string prm = item.Name;
                    string val = item.Value.ToString();
                    if (prm==param)
                    {
                        return Convert.ToInt16(val);
                    }
                }
            }
            return 0;
        }

        public Color Get_Colour(string param)
        {        
            string s= Get(param);
            string[] st = new string[4]; 
            st=s.Split(",");
            int  red=  Convert.ToByte(st[0]);
            int  green=    Convert.ToByte(st[1]);
            int  blue=  Convert.ToByte(st[2]);
            int  alpha =   Convert.ToByte(st[3]);
            Color c = Color.FromArgb(alpha, red, green, blue);
            return c;
            
        }

        public Size Get_Size(string param)
        {
            string s = Get(param);
            string[] st = new string[4];
            st=s.Split(",");
            int Width = Convert.ToInt16(st[0]);
            int Height = Convert.ToInt16(st[1]);
            Size size = new Size(Width, Height);
            return size;
        }

        public Point Get_Location(string param)
        {
            string s = Get(param);
            string[] st = new string[4];
            st=s.Split(",");
            int x = Convert.ToInt16(st[0]);
            int y = Convert.ToInt16(st[1]);
            Point pt = new Point(x, y);
            return pt;

        }

        public void set_PictureBox_WHXY(string param, PictureBox pb)
        {
            string s = Get(param);
            string[] st = new string[4];
            st=s.Split(",");
            int Width = Convert.ToInt16(st[0]);
            int Height = Convert.ToInt16(st[1]);
            Size sz=new Size(Width, Height);
            int x = Convert.ToInt16(st[2]);
            int y = Convert.ToInt16(st[3]);
            Point pt = new Point(x, y);
            pb.Location = pt;
            pb.Size = sz;
        }

        public void set_Button_WHXY(string param, Button btn) 
        {
            string s = Get(param);
            string[] st = new string[4];
            st=s.Split(",");
            int Width = Convert.ToInt16(st[0]);
            int Height = Convert.ToInt16(st[1]);
            Size sz = new Size(Width, Height);
            int x = Convert.ToInt16(st[2]);
            int y = Convert.ToInt16(st[3]);
            Point pt = new Point(x, y);
            btn.Location = pt;
            btn.Size = sz;
        }

        public void set_TextBox_WHXY(string param, TextBox tb)
        {
            string s = Get(param);
            string[] st = new string[4];
            st=s.Split(",");
            int Width = Convert.ToInt16(st[0]);
            int Height = Convert.ToInt16(st[1]);
            Size sz = new Size(Width, Height);
            int x = Convert.ToInt16(st[2]);
            int y = Convert.ToInt16(st[3]);
            Point pt = new Point(x, y);
            tb.Location = pt;
            tb.Size = sz;
        }

        public void set_Panel_WHXY(string param, Panel pnl)
        {
            string s = Get(param);
            string[] st = new string[4];
            st=s.Split(",");
            int Width = Convert.ToInt16(st[0]);
            int Height = Convert.ToInt16(st[1]);
            Size sz = new Size(Width, Height);
            int x = Convert.ToInt16(st[2]);
            int y = Convert.ToInt16(st[3]);
            Point pt = new Point(x, y);
            pnl.Location = pt;
            pnl.Size = sz;
        }

        public string Get_Value_from_Nested(string parent,string param)
            {
            if (file_name == "") return "";
            string readResult = string.Empty;
            using (StreamReader r = new StreamReader(file_name))
            {
                var json = r.ReadToEnd();
                JObject obj = JObject.Parse(json);
                var attributes = obj[parent][param];
                return attributes.ToString();               
            }
        }
        
        public string[] Get_Array_from_Nested(string parent, string param)
        {
            if (file_name == "") return null;
            string readResult = string.Empty;
            using (StreamReader r = new StreamReader(file_name))
            {
                var json = r.ReadToEnd();
                JObject obj = JObject.Parse(json);
                var attributes = obj[parent][param];

                var k = attributes.ToString();

                k=k.Trim();
                k = k.Replace(" ", String.Empty);

                string pkt = k.Trim(new Char[] { ' ', '[',']', '.','\r','\n' });
                pkt = pkt.Replace("\r","");
                pkt = pkt.Replace("\n", "");

                string[] ss=pkt.Split(",");
                return ss;
            }
        }
      
        public void put(string param, string value)
        {
            if (file_name == "") return;

            string readResult = string.Empty;
            string writeResult = string.Empty;
            using (StreamReader r = new StreamReader(file_name))
            {
                var json = r.ReadToEnd();
                var jobj = JObject.Parse(json);
                readResult = jobj.ToString();
                foreach (var item in jobj.Properties())
                {
                    string prm = item.Name;
                    if(prm==param)
                    {
                        item.Value = value;
                    }
                }
                writeResult = jobj.ToString();
            }
            File.WriteAllText(file_name, writeResult);
        }
        //___________________________________________________________________________________________________
        public void update(string prev, string current) 
        {
            if (file_name == "") return;

            string readResult = string.Empty;
            string writeResult = string.Empty;
            using (StreamReader r = new StreamReader(file_name))
            {
                var json = r.ReadToEnd();
                var jobj = JObject.Parse(json);
                readResult = jobj.ToString();
                foreach (var item in jobj.Properties())
                {
                    item.Value = item.Value.ToString().Replace(prev, current);
                }
                writeResult = jobj.ToString();
            }
            File.WriteAllText(file_name, writeResult);             
        }
        //___________________________________________________________________________________________________
    }
}
