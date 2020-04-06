using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace PhotoEditor
{
    public partial class ColorPickWindow : Window
    {
        private static readonly Regex REGEX = new Regex("[^0-9]+");

        public RGBPixel RGB { get; set; }

        public ColorPickWindow(RGBPixel rgbColor)
        {
            RGB = rgbColor;
            InitializeComponent();
            DataContext = RGB;
        }

        private void ValidateInput(object sender, TextCompositionEventArgs e)
        {
            e.Handled = REGEX.IsMatch(e.Text);
        }
    }
}
