using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
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
        public string Text {
            get { return TheText.Text; }
            set { TheText.Text = value; }
         }
        public int Id { get; set; }
        public MyUserControl1()
        {
            this.InitializeComponent();
        }
        public MyUserControl1(string name, int row, int col, string text, Grid containerGrid)
        {
            this.InitializeComponent();
            Name = name;
            Row = row;
            Col = col;
            Text = text;
            ContainrerGrid = containerGrid;
        }

        public MyUserControl1(string name, int row, int col, string text, Grid containerGrid, Brush background)
        {
            this.InitializeComponent();
            Name = name;
            Row = row;
            Col = col;
            Text = text;
            ContainrerGrid = containerGrid;
            Border.Background = background;
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
                    Grid.SetColumn(this, Col);
                    Grid.SetRow(this, Row);
                    Grid.SetColumnSpan(this, ColSpan);
                    Grid.SetRowSpan(this, RowSpan);
            }
        }

        private void TheText_Tapped(object sender, TappedRoutedEventArgs e)
        {

        }
    }
}
