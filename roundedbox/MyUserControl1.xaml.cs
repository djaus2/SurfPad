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

        //Enable wrapped text on button
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
        public int Row { get; set; } = 0;
        public int Col { get; set; } = 0;
        public int RowSpan { get; set; } = 1;
        public int ColSpan { get; set; } = 1;

        public MyUserControl1(int row, int col, string text, Grid containerGrid, 
            //Optional parameters:
            //Name or Id should be unique
            string name="", Brush background = null, 
            int id = FlagForDefaultVal, int cnrRad= FlagForDefaultVal,
            int colSpan= FlagForDefaultVal, int rowSpan = FlagForDefaultVal
            )
        {
            this.InitializeComponent();
            Name = name;
            Row = row;
            Col = col;
            Text = text;
            

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
                Id = containerGrid.ColumnDefinitions.Count * row + col;

            if (colSpan == FlagForDefaultVal)
                ColSpan=1;
            else
                ColSpan=colSpan;

            if (rowSpan == FlagForDefaultVal)
                RowSpan = 1;
            else
               RowSpan = rowSpan;

            TheText.Width = containerGrid.ColumnDefinitions[0].Width.Value;
            TheText.Height = containerGrid.RowDefinitions[0].Height.Value;

            ContainrerGrid = containerGrid;
        }


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

        //Send back button Name and Id
        public static event TypedEventHandler<string,int> ButtonTapped;
        private void TheText_Tapped(object sender, TappedRoutedEventArgs e)
        {
            if (ButtonTapped != null)
            {
                ButtonTapped(this.Name, this.Id);
            }
        }
    }
}
