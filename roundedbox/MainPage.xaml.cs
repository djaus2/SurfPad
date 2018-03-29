using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Socket;
using USBSerial;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI;
using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409

namespace roundedbox
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

        public enum TerminalModes { none, BT, USBSerial, Socket, RFCOMM_Chat };
        public static TerminalModes TerminalMode = TerminalModes.none;

        public ListView clientListBox;



        uc.RoundedButtonControl[][] buttons = new uc.RoundedButtonControl[0][];

        public static MainPage MP;
        public static Bluetooth.BluetoothSerialTerminalPage BTTerminalPage;
        public static USBSerial.USBSerialTerminalPage USBSerialTerminalPage { get; internal set; }
        public static SocketTerminalPage SocketTerminalPage { get; internal set; }
        public static RFCOMM_ChatServer RFCOMM_ChatPage {get; internal set;}

        public MainPage()
        {
            this.InitializeComponent();

            MP = this;

            Setup("");
            uc.RoundedButtonControl.ButtonTapped += MainPage_ButtonTapped1;

            this.NavigationCacheMode = Windows.UI.Xaml.Navigation.NavigationCacheMode.Enabled;

        }

        private void Setup(string jsonData)
        {
            DoCommands(jsonData);

            int iCornerRadius = Commands.ElementConfigInt["iCornerRadius"];

            InitTheGrid(Commands.ElementConfigInt["iRows"], Commands.ElementConfigInt["iColumns"],
                Commands.ElementConfigInt["iHeight"], Commands.ElementConfigInt["iWidth"],
                Commands.ElementConfigInt["iSpace"]);
            //uc.MyUserControl1[][] buttons = new uc.MyUserControl1[0][]; ... Is inited in the InitTheGrid(), row by row
            foreach (var menuItem in MainMenu)
            {
                AddMyUserControl1(menuItem.idTag.Row, menuItem.idTag.Col, menuItem.name, "",
                    null, -1, iCornerRadius);
            }

            
        }

        private void AddMyUserControl1(int row, int col, string text, 
            //Optional parameters:
            //Name or Id should be unique
            string name = "", Brush background = null,
            int id = -1, int cnrRad = -1, int colSpan=1, int rowSpan=1)
        {
            buttons[row][col] = new uc.RoundedButtonControl(row,col,text,TheGrid,name,background,id,cnrRad, colSpan, rowSpan);
        }

        private  async void MainPage_ButtonTapped1(string sender, int args)
        {
            string name = sender;
            int id = args;
            bool notDone = false;
            //listView1.Items.Insert(0, name);
            if (args == 0)
            {
                if (TerminalMode == TerminalModes.none)
                {
                    TerminalMode = TerminalModes.BT;
                    Frame.Navigate(typeof(Bluetooth.BluetoothSerialTerminalPage));
                }
                else
                {
                    notDone = true;
                }
            }
            else if (args == 1)
            { 
                char ch = '!';
                if (TerminalMode == TerminalModes.BT)
                {
                    if (BTTerminalPage != null)
                        BTTerminalPage.SendCh(ch);
                }
                else if (TerminalMode == TerminalModes.USBSerial)
                {
                    if (USBSerialTerminalPage != null)
                        USBSerialTerminalPage.SendCh(ch);
                }
                else if (TerminalMode == TerminalModes.Socket)
                {
                    if (SocketTerminalPage != null)
                        SocketTerminalPage.CloseSocket();
                    TerminalMode = TerminalModes.none;
                }
            }
            else if (args == 2)
            {
                if (TerminalMode == TerminalModes.none)
                {
                    TerminalMode = TerminalModes.USBSerial;
                    Frame.Navigate(typeof(USBSerial.USBSerialTerminalPage));
                }
                else
                {
                    notDone = true;
                }
            }
            else if (args == 3)
            {


                if (TerminalMode == TerminalModes.none)
                {
                    TerminalMode = TerminalModes.Socket;
                    Frame.Navigate(typeof(Socket.SocketTerminalPage));
                }
                else
                {
                    notDone = true;
                }
            }

            else if (args == 4)
            {


                if (TerminalMode == TerminalModes.none)
                {
                    TerminalMode = TerminalModes.RFCOMM_Chat;
                    Frame.Navigate(typeof(RFCOMM_ChatServer));
                }
                else
                {
                    notDone = true;
                }
            }

            if ((args > 4) || notDone)
            {
                //characters as code for keys are A..Z and a..z
                char ch = (char)((int)'A' + id);
                if (ch > 'Z')
                    ch = (char)((int)'a' + id - 26);
                if (TerminalMode == TerminalModes.BT)
                {
                    if (BTTerminalPage != null)
                        BTTerminalPage.SendCh(ch);
                }
                else if (TerminalMode == TerminalModes.USBSerial)
                {
                    if (USBSerialTerminalPage != null)
                        USBSerialTerminalPage.SendCh(ch);
                }
                else if (TerminalMode == TerminalModes.Socket)
                {
                    if (SocketTerminalPage != null)
                        await SocketTerminalPage.SendCh(ch);
                }
                else if (TerminalMode == TerminalModes.RFCOMM_Chat)
                {
                    if (SocketTerminalPage != null)
                        RFCOMM_ChatPage.SendCh(ch);
                }

            }
        }

        public void InitTheGrid(int x, int y, int Height = DefaultCellHeight, int Width = DefaultCellWidth, int space = DefaultCellSpacing)
        {
            TheGrid.Children.Clear();
            TheGrid.RowSpacing = space;
            TheGrid.ColumnSpacing = space;
            buttons = new uc.RoundedButtonControl[x][];
            for (int i = 0; i<x;i++)
            {
                buttons[i] = new uc.RoundedButtonControl[y];
                RowDefinition rd2 = new RowDefinition();
                rd2.Height = new GridLength((double)Height);
                TheGrid.RowDefinitions.Add(rd2);
            }
            for (int j = 0; j < y+ TextSpan; j++)
            {
                ColumnDefinition cd2 = new ColumnDefinition();
                cd2.Width = new GridLength((double)Width);
                TheGrid.ColumnDefinitions.Add(cd2);
            }
            var bdr =
                new Border
                {
                    BorderThickness = new Thickness(1),
                    BorderBrush = Black,            
                    Background = White,
                    Padding = new Thickness(0),
                    CornerRadius = new CornerRadius(5)
                };
            TheGrid.Children.Add(bdr);
            Grid.SetColumn(bdr, y);
            Grid.SetRow(bdr, 0);
            Grid.SetColumnSpan(bdr, TextSpan);
            Grid.SetRowSpan(bdr, y+1);
            //var tb =
            //    new TextBlock
            //    {
            //        Text = "bla bla",
            //        TextWrapping = TextWrapping.Wrap,
            //        TextAlignment = TextAlignment.Left
            //    };
            //bdr.Child = tb;
            listView1 = new ListView()
            {
                Name="MyList",
                IsEnabled = false,
                
            };
            bdr.Child = listView1;
            clientListBox = listView1;
        }

        ListView listView1;
        List<Commands> MainMenu;
        private void DoCommands(string jsonData)
        {
            Commands.Init();
            GetCommands("Config", jsonData);
            //Following settings are mandatory
            bool res = Commands.CheckKeys();
            //Next two are optional settings
            ////bool res2 = Commands.CheckComportIdSettingExists();
            ////res2 = Commands.CheckcIfComportConnectDeviceNoKeySettingExists();
            GetCommands("MainMenu", jsonData);
            MainMenu = Commands.GetMenu("MainMenu");
        }


        private string config = "";
        internal async Task UpdateTextAsync(string recvdtxt)
        {
            if(recvdtxt.Substring(recvdtxt.Length - 1,1) == EOStringStr)
            {
                if (recvdtxt.Substring(0, "{\"Config\":".Length) == "{\"Config\":")
                {
                    config = recvdtxt;
                    config = config.Replace(EOStringStr, "");
                }
                else if (recvdtxt.Substring(0, "{\"MainMenu\":".Length) == "{\"MainMenu\":")
                {
                    string menus = recvdtxt;
                    menus = menus.Replace(EOStringStr, "");
                    string jsonData = "[ " + config + " , " + menus + " ]";
                    Setup(jsonData);
                }
                else
                {
                    await Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
                    {
                        listView1.Items.Insert(0, recvdtxt.Substring(0, recvdtxt.Length - 1));
                    });
                }

            }
            else
            {
                char ch = recvdtxt[0];
                //Indexes are A.. Z and a.. z 
                int index = ch - ((int)'A');
                if (ch >'Z')
                    index = 26 + ch - ((int)'a');
                foreach (uc.RoundedButtonControl[] buts in buttons)
                {
                    var but = from n in buts where n.Id == index select n.Text;

                    if (but.Count() != 0)
                    {
                        await Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
                        {
                            listView1.Items.Insert(0, but.First());
                        });
                        break;
                    }
                }
            }

        }

        private void Button_Tapped(object sender, TappedRoutedEventArgs e)
        {
            Frame.Navigate(typeof(Bluetooth.BluetoothSerialTerminalPage));
        }
    }
}
