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
        int iDefaultCellWidth = DefaultCellWidth;
        int iDefaultCellHeight = DefaultCellHeight;
        int iDefaultCellSpacing = DefaultCellSpacing;

        //public double Width_1 = 5;
        //public Thickness W1 = new Thickness(5);
        uc.MyUserControl1[][] buttons = new uc.MyUserControl1[0][];
        public MainPage()
        {
            //Resources.Add("GridLength1", new GridLength((double)10));
            //Resources.Add("GridLength2", new GridLength((double)120));
            //Resources.Add("GridRound", new CornerRadius((int)20));
            this.InitializeComponent();
            
            //buttons[1][1] = new uc.MyUserControl1()
            //{
            //    Name = "fred",
            //    Text = "Qwerty",
            //    Row = 1,
            //    Col = 1,
            //    ContainrerGrid=TheGrid
            //};
            //buttons[3][3] = new uc.MyUserControl1()
            //{
            //    Name = "fred",
            //    Text = "Qwerty2",
            //    Row = 3,
            //    Col = 3,
            //    ContainrerGrid = TheGrid
            //};

            //Brush red = new SolidColorBrush(Colors.Red);
            //AddMyUserControl1( 0, 0,"arc1", "First", red,123,50,1,2);
            //AddMyUserControl1(1, 1, "arc2", "Second", null, 124, 5, 2);
            //AddMyUserControl1(2, 2, "The quick brown fox jumps over the lazy dog",  "Third");

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
        }

        //private void MainPage_ButtonTapped(object sender, TappedRoutedEventArgs e)
        //{
        //    if (sender is Button)
        //    {
        //        Button but = (Button)sender;
        //        string name = but.Name;
        //    }
        //    //throw new NotImplementedException();
        //}

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
            for (int j = 0; j < y; j++)
            {
                ColumnDefinition cd2 = new ColumnDefinition();
                cd2.Width = new GridLength((double)Width);
                TheGrid.ColumnDefinitions.Add(cd2);
            }
        }

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
