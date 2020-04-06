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
    public partial class MaskSizeWindow : Window
    {
        private static readonly Regex REGEX = new Regex("[^0-9]+");

        public int MaskSize { get; set; }

        public MaskSizeWindow()
        {
            InitializeComponent();
        }

        private void Window_OnLoaded(object sender, RoutedEventArgs e)
        {
            MaskSizeTextBox.Text = "3";
        }

        private void ValidateInput(object sender, TextCompositionEventArgs e)
        {
            e.Handled = REGEX.IsMatch(e.Text);
        }

        private void ButtonOK_OnClick(object sender, RoutedEventArgs e)
        {
            MaskSize = Convert.ToInt32(MaskSizeTextBox.Text);
            DialogResult = true;
            Close();
        }

        private void ButtonCancel_OnClick(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}
