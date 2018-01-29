﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
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

// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409

namespace roundedbox
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        const int DefaultCellWidth= 120;
        const int DefaultCellHeight = 120;
        const int DefaultCellSpacing = 10;
        const int TextSpan = 2;
        Brush Black = new SolidColorBrush(Colors.Black);
        Brush White = new SolidColorBrush(Colors.White);



        uc.MyUserControl1[][] buttons = new uc.MyUserControl1[0][];

        public MainPage()
        {
            this.InitializeComponent();

            DoCommands();

            int iCornerRadius = Commands.ElementConfigInt["iCornerRadius"];

            InitTheGrid(Commands.ElementConfigInt["iRows"], Commands.ElementConfigInt["iColumns"],
                Commands.ElementConfigInt["iHeight"], Commands.ElementConfigInt["iWidth"], 
                Commands.ElementConfigInt["iSpace"]);
            foreach (var menuItem in MainMenu)
            {
                AddMyUserControl1(menuItem.idTag.Row, menuItem.idTag.Col, menuItem.name,"",
                    null,-1,iCornerRadius);
            }

            uc.MyUserControl1.ButtonTapped += MainPage_ButtonTapped1;

        }

        private void AddMyUserControl1(int row, int col, string text, 
            //Optional parameters:
            //Name or Id should be unique
            string name = "", Brush background = null,
            int id = -1, int cnrRad = -1, int colSpan=1, int rowSpan=1)
        {
            buttons[row][col] = new uc.MyUserControl1(row,col,text,TheGrid,name,background,id,cnrRad, colSpan, rowSpan);
        }

        private void MainPage_ButtonTapped1(string sender, int args)
        {
            string name = sender;
            int id = args;
            listView1.Items.Insert(0, name);
        }

        public void InitTheGrid(int x, int y, int Height = DefaultCellHeight, int Width = DefaultCellWidth, int space = DefaultCellSpacing)
        {
            TheGrid.Children.Clear();
            TheGrid.RowSpacing = space;
            TheGrid.ColumnSpacing = space;
            buttons = new uc.MyUserControl1[x][];
            for (int i = 0; i<x;i++)
            {
                buttons[i] = new uc.MyUserControl1[y];
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
                IsEnabled = false,
                
            };
            listView1.Items.Add("Item 1");
            listView1.Items.Add("Item 2");
            listView1.Items.Add("Item 3");
            listView1.Items.Add("Item 4");
            listView1.Items.Add("Item 5");
            listView1.Items.Insert(0, "Front");
            bdr.Child = listView1;
        }

        ListView listView1;
        List<Commands> MainMenu;
        private void DoCommands()
        {
            GetCommands("ElementConfig");
            //Following settings are mandatory
            bool res = Commands.CheckKeys();
            //Next two are optional settings
            ////bool res2 = Commands.CheckComportIdSettingExists();
            ////res2 = Commands.CheckcIfComportConnectDeviceNoKeySettingExists();
            GetCommands("MainMenu");
            MainMenu = Commands.GetMenu("MainMenu");
        }


    }
}
