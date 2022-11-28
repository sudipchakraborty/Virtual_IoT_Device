using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

using Tools;

namespace Virtual_IoT_Device
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        IoT_SinricPro iot;

        public MainWindow()
        {
            InitializeComponent();           
            txt_value_B.Text="255";
            txt_value_G.Text="255";
            txt_value_R.Text="255";
            txt_wall_temp.Text="0.0";
            Smart_bulb_update();
            // iot=new IoT_SinricPro();
            update_wall_thermometer(txt_wall_temp.Text);
        }

        private void btn_door_open_Click(object sender, EventArgs e)
        {
            img_door.Source=new BitmapImage(new Uri("Image/door_open.jpg", UriKind.Relative));           
        }

        private void btn_door_close_Click(object sender, EventArgs e)
        {
            img_door.Source=new BitmapImage(new Uri("Image/door_close.jpg", UriKind.Relative));           
        }

        private void btn_smart_bulb_on_Click(object sender, EventArgs e)
        {
            Smart_bulb_update();
        }

        private void btn_smart_bulb_off_Click(object sender, EventArgs e)
        {
            smart_bulb_bg.Fill=new SolidColorBrush(Color.FromRgb(125, 125, 125));
        }

        private void txt_value_R_KeyDown(object sender, KeyEventArgs e)
        {
            if(e.Key==Key.Enter)
            {
                Smart_bulb_update();
            }
        }

        private void txt_value_G_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key==Key.Enter)
            {
                Smart_bulb_update();
            }        
        }

        private void txt_value_B_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key==Key.Enter)
            {
                Smart_bulb_update();
            }         
        }

        private void txt_wall_temp_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key==Key.Enter)
            {
                update_wall_thermometer(txt_wall_temp.Text);    
            }         
        }

        private void Smart_bulb_update()
        {
            byte r=Convert.ToByte(txt_value_R.Text);
            byte g = Convert.ToByte(txt_value_G.Text);
            byte b = Convert.ToByte(txt_value_B.Text);
            smart_bulb_bg.Fill=new SolidColorBrush(Color.FromRgb(r, g, b));
        }

        private void update_wall_thermometer(String temperature)
        {
            double temp = Convert.ToDouble(temperature);
            if(temp>100) temp = 100;
            if(temp<0) temp = 0;
            double running_val = -2.36* temp + 236;

            Canvas canvasPanel = temp_progress;

            Rectangle Erase= new Rectangle();
            Erase.Width = 5;
            Erase.Height =270;
            Erase.Fill = new SolidColorBrush(Colors.Black);
            Canvas.SetLeft(Erase, 5);
            Canvas.SetTop(Erase, 0);
            canvasPanel.Children.Add(Erase);
            ///////////////////////////////////////////
            Rectangle redRectangle = new Rectangle();         
            redRectangle.Width = 5;
            redRectangle.Height =270- running_val;
            redRectangle.Fill = new SolidColorBrush(Colors.Red);   
            Canvas.SetLeft(redRectangle, 5);
            Canvas.SetTop(redRectangle, running_val);  
            canvasPanel.Children.Add(redRectangle); 
        }


    }
}
