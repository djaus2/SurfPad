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

// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409

namespace roundedbox
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        //public double Width_1 = 5;
        public Thickness W1 = new Thickness(5);
        uc.MyUserControl1[] buttons = new uc.MyUserControl1[10];
        public MainPage()
        {
            Resources.Add("GridLength1", new GridLength((double)10));
            Resources.Add("GridLength2", new GridLength((double)120));
            Resources.Add("GridRound", new CornerRadius((int)20));
            this.InitializeComponent();
            buttons[0] = new uc.MyUserControl1()
            {
                Name = "fred",
                Id = 0,
                Text = "Qwerty",
                Row = 3,
                Col = 3,
                ContainrerGrid=TheGrid
            };
            buttons[1] = new uc.MyUserControl1()
            {
                Name = "fred",
                Id = 1,
                Text = "Qwerty2",
                Row = 7,
                Col = 5,
                ContainrerGrid = TheGrid
            };

            Brush red = new SolidColorBrush(Colors.Beige);

            buttons[2] = new uc.MyUserControl1("azx", 5, 7, "Arc", TheGrid, red);

        }

        private void TextBlock_Tapped(object sender, TappedRoutedEventArgs e)
        {

        }

        private void Border_Tapped(object sender, TappedRoutedEventArgs e)
        {

        }
    }
}
