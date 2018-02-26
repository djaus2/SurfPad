using SurfPadIoT.Pages;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Windows.Devices.Gpio;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;


// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=234238

namespace SurfPadIoT
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        const int DefaultCellWidth = 120;
        public const uint cBAUD = 115200;
        const int DefaultCellHeight = 120;
        const int DefaultCellSpacing = 10;
        const int TextSpan = 2;
        public const string EOStringStr = "~";
        public const char EOStringChar = '~';
        Brush Black = new SolidColorBrush(Colors.Black);
        Brush White = new SolidColorBrush(Colors.White);

        public enum TerminalModes { none, BT, USBSerial, Socket };
        public static TerminalModes TerminalMode = TerminalModes.Socket;

        public ListView clientListBox;




        public static MainPage MP;
        //public static Bluetooth.BluetoothSerialTerminalPage BTTerminalPage;
        public static USBSerialTerminalPage USBSerialTerminalPage { get; internal set; }
        public static SocketServerTerminalPage SocketTerminalPage { get; internal set; }
        public static BluetoothSerialTerminalPage BTTerminalPage { get; internal set; }

        public MainPage()
        {
            this.InitializeComponent();
            MP = this;
            this.NavigationCacheMode = Windows.UI.Xaml.Navigation.NavigationCacheMode.Enabled;
            TerminalMode = TerminalModes.none;
            InitGPIO();
        }

        private void InitGPIO()

        {
            GpioController gpio = GpioController.GetDefault();
            {
                if (gpio == null)

                {
                    Status.Text = "There is no GPIO controller on this device.";
                    return;
                }

                using (var pin2 = gpio.OpenPin(2)) {
                    using (var pin3 = gpio.OpenPin(3)) {
                        using (var pin4 = gpio.OpenPin(4)) {



                            pin2.SetDriveMode(GpioPinDriveMode.Input);
                            pin3.SetDriveMode(GpioPinDriveMode.Input);
                            pin4.SetDriveMode(GpioPinDriveMode.Input);

                            var a0 = pin2.Read();
                            var a1 = pin3.Read();
                            var a2 = pin4.Read();
                            if (a0 == GpioPinValue.High)
                            {
                                TerminalMode = TerminalModes.BT;
                                Status.Text = "BT Mode";
                            }
                            else if (a1 == GpioPinValue.High)
                            {
                                TerminalMode = TerminalModes.USBSerial;
                                Status.Text = "USB Serial Mode";
                            }
                            else if (a2 == GpioPinValue.High)
                            {
                                TerminalMode = TerminalModes.Socket;
                                Status.Text = "Socket Mode";
                            }
                            else
                            {
                                TerminalMode = TerminalModes.none;
                                Status.Text = "Need to set one of GPIO 2,3,4 to High. Pins 3.5.7 respectively";
                            }
                            //switch (TerminalMode)
                            //{
                            //    case TerminalModes.Socket:
                            //            Frame.Navigate(typeof(SocketServerTerminalPage));
                            //        break;
                            //    case TerminalModes.USBSerial:
                            //            Frame.Navigate(typeof(USBSerialTerminalPage));
                            //        break;
                            //    case TerminalModes.BT:
                            //            Frame.Navigate(typeof(BluetoothSerialTerminalPage));
                            //        break;
                            //    case TerminalModes.none:
                            //        break;
                            //}
                        }
                    }
                }
            }
        }



        private void button_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button)
            {
                string content = (string)((Button)sender).Content;
                if (content != "")
                {
                    switch (content)
                    {
                        case "Socket Server":
                            if (TerminalMode == TerminalModes.none)
                            {
                                TerminalMode = TerminalModes.Socket;
                                Frame.Navigate(typeof(SocketServerTerminalPage));
                            }
                            break;
                        case "USB Serial":
                            if (TerminalMode == TerminalModes.none)
                            {
                                TerminalMode = TerminalModes.USBSerial;
                                Frame.Navigate(typeof(SurfPadIoT.Pages.USBSerialTerminalPage));
                            }
                            break;
                        case "Bluetooth":
                            if (TerminalMode == TerminalModes.none)
                            {
                                TerminalMode = TerminalModes.BT;
                                Frame.Navigate(typeof(BluetoothSerialTerminalPage));
                            }
                            break;
                        case "Reset":
                            TerminalMode = TerminalModes.none;
                            break;
                    }
                }
            }
        }

        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            switch (TerminalMode)
            {
                case TerminalModes.Socket:
                    Frame.Navigate(typeof(SocketServerTerminalPage));
                    break;
                case TerminalModes.USBSerial:
                    Frame.Navigate(typeof(USBSerialTerminalPage));
                    break;
                case TerminalModes.BT:
                    Frame.Navigate(typeof(BluetoothSerialTerminalPage));
                    break;
            }
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            if (e.NavigationMode == NavigationMode.Back)
            {
                TerminalMode = TerminalModes.none;
            }
            else
            {

            }
        }

        internal Task UpdateTextAsync(string recvdtxt)
        {
            throw new NotImplementedException();
        }
    }
}
