using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
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

// The User Control item template is documented at https://go.microsoft.com/fwlink/?LinkId=234236

namespace roundedbox.uc
{
    public  partial class MyUserControl1 : UserControl
    {
        static readonly  Brush DefaultBackground =  new SolidColorBrush (Colors.Beige);
        const int FlagForDefaultVal = -1;
        const int CornerRadiusVal = 5;

        public CornerRadius DefaultCornerRadius
        {
            get {
                return  new CornerRadius(CornerRadiusVal);
            }
        }
        public string Text {
            set { TheText.Content =
                    new TextBlock
                    {
                        Text = value,
                        TextWrapping = TextWrapping.Wrap,
                        TextAlignment = TextAlignment.Center,
                    };
            }
         }
        public int Id { get; set; }
        public MyUserControl1()
        {
            this.InitializeComponent();
            TheText.Width = 100;
            TheText.Height = 110;
        }
        //public MyUserControl1(string name, int row, int col, string text, Grid containerGrid)
        //{
        //    this.InitializeComponent();
        //    Name = name;
        //    Row = row;
        //    Col = col;
        //    Text = text;
        //    ContainrerGrid = containerGrid;
        //    Id = containerGrid.ColumnDefinitions.Count * row + col;
        //    TheText.Width = 100;
        //    TheText.Height = 110;
        //}

        //public MyUserControl1(string name, int row, int col, string text, Grid containerGrid, Brush background)
        //{
        //    this.InitializeComponent();
        //    Name = name;
        //    Row = row;
        //    Col = col;
        //    Text = text;
        //    ContainrerGrid = containerGrid;
        //    Borderx.Background = background;
        //    Id = (containerGrid.ColumnDefinitions.Count/2) * row + col;
        //    TheText.Width = 100;
        //    TheText.Height = 110;
        //}

        //public MyUserControl1(string name,  int row, int col, string text, Grid containerGrid, int id = -1 )
        //{
        //    this.InitializeComponent();
        //    Name = name;
        //    Row = row;
        //    Col = col;
        //    Text = text;
        //    ContainrerGrid = containerGrid;
        //    if (id != -1)
        //        Id = id;
        //    else
        //        Id = (containerGrid.ColumnDefinitions.Count/2) * row + col;
        //    TheText.Width = containerGrid.ColumnDefinitions[0].Width.Value;
        //    TheText.Height = containerGrid.RowDefinitions[0].Height.Value;
        //}

        public MyUserControl1(int row, int col, string text, Grid containerGrid, 
            //Optional parameters:
            //Name or Id should be unique
            string name="", Brush background = null, int id = FlagForDefaultVal, int cnrRad= FlagForDefaultVal)
        {
            this.InitializeComponent();
            Name = name;
            Row = row;
            Col = col;
            Text = text;
            ContainrerGrid = containerGrid;

            if (cnrRad == FlagForDefaultVal)
                Borderx.CornerRadius = DefaultCornerRadius;
            else
                Borderx.CornerRadius = new CornerRadius(cnrRad);

            if (background == null)
                Borderx.Background = DefaultBackground;
            else
                Borderx.Background = background;

            if (id != FlagForDefaultVal)
                Id = id;
            else
                Id = (containerGrid.ColumnDefinitions.Count/2) * row + col;

            TheText.Width = containerGrid.ColumnDefinitions[1].Width.Value;
            TheText.Height = containerGrid.RowDefinitions[1].Height.Value;

        }

        public int Row { get; set; } = 0;
        public int Col { get; set; } = 0;

        public int RowSpan { get; set; } = 1;
        public int ColSpan { get; set; } = 1;

        public Grid ContainrerGrid
        {
            set
            {
                    value.Children.Add(this);
                    Grid.SetColumn(this, 2*Col+1);
                    Grid.SetRow(this, 2*Row+1);
                    Grid.SetColumnSpan(this, ColSpan);
                    Grid.SetRowSpan(this, RowSpan);
            }
        }

        public static event TypedEventHandler<string,int> ButtonTapped;

        private void TheText_Tapped(object sender, TappedRoutedEventArgs e)
        {

            if (ButtonTapped != null)
            {
                EventArgs ev = new EventArgs();

                ButtonTapped(this.Name, this.Id);
            }
        }
    }
}
