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
    public partial class MaskValuesWindow : Window
    {
        private static readonly Regex REGEX = new Regex("[^0-9.-]+");

        public Mask Mask { get; set; }

        public MaskValuesWindow(int size)
        {
            InitializeComponent();
            Mask = new Mask(size);
        }

        private void Window_OnLoaded(object sender, RoutedEventArgs e)
        {
            for (int i = 0; i < Mask.Size; i++)
            {
                for (int j = 0; j < Mask.Size; j++)
                {
                    ColumnDefinition column = new ColumnDefinition();
                    column.Width = new GridLength(1, GridUnitType.Auto);
                    ValuesGrid.ColumnDefinitions.Add(column);

                    TextBox textBox = CreateTextBox();
                    textBox.Text = Convert.ToString(Mask.Matrix[i, j]);

                    ValuesGrid.Children.Add(textBox);
                    Grid.SetRow(textBox, i);
                    Grid.SetColumn(textBox, j);
                }
                RowDefinition row = new RowDefinition();
                row.Height = new GridLength(1, GridUnitType.Auto);
                ValuesGrid.RowDefinitions.Add(row);
            }
        }

        private TextBox CreateTextBox()
        {
            TextBox textBox = new TextBox();
            
            textBox.Margin = new Thickness(5);
            textBox.Padding = new Thickness(5);
            textBox.Width = 50;
            textBox.HorizontalContentAlignment = HorizontalAlignment.Center;
            textBox.VerticalContentAlignment = VerticalAlignment.Center;
            textBox.PreviewTextInput += ValidateInput;
            textBox.TextChanged += GridTextBox_OnTextChanged;
            
            return textBox;
        }

        private void GridTextBox_OnTextChanged(object sender, TextChangedEventArgs e)
        {
            TextBox textBox = sender as TextBox;
            
            int column = Grid.GetColumn(textBox);
            int row = Grid.GetRow(textBox);

            int result = 0;
            int.TryParse(textBox.Text, out result);
            Mask.Matrix[row, column] = result;
        }

        private void ValidateInput(object sender, TextCompositionEventArgs e)
        {
            e.Handled = REGEX.IsMatch(e.Text);
        }

        private void ButtonCancel_OnClick(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        private void ButtonOK_OnClick(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
            Close();
        }
    }
}
