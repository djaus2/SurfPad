﻿using SurfPadIoT.Pages;
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

        public enum TerminalModes { none, BT, USBSerial, Socket, RFCOMMChat };
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
            //TerminalMode = TerminalModes.RFCOMMChat;
            InitGPIO();
        }

        private void InitGPIO()

        {
            GpioController gpio = GpioController.GetDefault();
            {
                if (gpio == null)

                {
                    Status.Text = "There is no GPIO controller on this device.";
                    TerminalMode = TerminalModes.RFCOMMChat;
                    Status.Text = "RFCOMM Chat Mode";
                    return;
                }
                //Can set in MainPage()
                if (TerminalMode == TerminalModes.none)
                { 
                using (var pin2 = gpio.OpenPin(2)) { //Physical Pin 3
                    using (var pin3 = gpio.OpenPin(3)) { //Physical Pin 5
                        using (var pin4 = gpio.OpenPin(4)) { //Physical Pin 7
                                using (var pin17 = gpio.OpenPin(17)) //Physical Pin 11
                                {
                                    //Nb: Physical Pin 1 = Vcc(3.3) & Physical Pin 9 = Gnd
                                    //Need to ground all but the required pinX input

                                    //Nb: pinX X is the GPIO number not the physical pin
                                    // eg. pin2 is GPIO2 = physical pin 3 etc.

                                    //Seems that all bar GPIO17 float high.
                                    //So might get away with only defintely setting its state and driving other ones low as required
                                    pin2.SetDriveMode(GpioPinDriveMode.Input);
                                    pin3.SetDriveMode(GpioPinDriveMode.Input);
                                    pin4.SetDriveMode(GpioPinDriveMode.Input);
                                    pin17.SetDriveMode(GpioPinDriveMode.Input);

                                    var a0 = pin2.Read();
                                    var a1 = pin3.Read();
                                    var a2 = pin4.Read();
                                    var a3 = pin17.Read();
                                    if (a0 == GpioPinValue.Low)
                                    {
                                        TerminalMode = TerminalModes.BT;
                                        Status.Text = "BT Mode";
                                    }
                                    else if (a1 == GpioPinValue.Low)
                                    {
                                        TerminalMode = TerminalModes.USBSerial;
                                        Status.Text = "USB Serial Mode";
                                    }
                                    else if (a2 == GpioPinValue.Low)
                                    {
                                        TerminalMode = TerminalModes.Socket;
                                        Status.Text = "Socket Mode";
                                    }
                                    else if (a3 == GpioPinValue.Low)
                                    {
                                        TerminalMode = TerminalModes.RFCOMMChat;
                                        Status.Text = "RFCOMM Chat Mode";
                                    }
                                    else
                                    {
                                        TerminalMode = TerminalModes.none;
                                        Status.Text = "Need to set one of GPIO 2,3,4, 17 to High. Pins 3,5,7,11 respectively";
                                    }
                                }
                            }
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
                        case "RFCOMM Chat":
                            if (TerminalMode == TerminalModes.none)
                            {
                                TerminalMode = TerminalModes.RFCOMMChat;
                                Frame.Navigate(typeof(RFCOMM_ChatClientPage));
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
                case TerminalModes.RFCOMMChat:
                    Frame.Navigate(typeof(RFCOMM_ChatClientPage));
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
