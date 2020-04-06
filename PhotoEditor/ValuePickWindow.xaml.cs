using System;
using System.Collections.Generic;
using System.ComponentModel;
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
    public partial class ValuePickWindow : Window, INotifyPropertyChanged
    {
        private static readonly Regex REGEX = new Regex("[^0-9]+");

        private int value;
        public int Value
        {
            get { return value; }
            set
            {
                this.value = value;
                OnPropertyChanged("Value");
            }
        }

        public int Minimum { get; set; }
        public int Maximum { get; set; }

        public ValuePickWindow()
        {
            Value = 0;
            InitializeComponent();
            Minimum = -255;
            Maximum = 255;
            DataContext = this;
        }

        public ValuePickWindow(int minimum, int maximum)
        {
            Value = 0;
            InitializeComponent();
            Minimum = minimum;
            Maximum = maximum;
            DataContext = this;
        }

        public event PropertyChangedEventHandler PropertyChanged;

        private void ValidateInput(object sender, TextCompositionEventArgs e)
        {
            e.Handled = REGEX.IsMatch(e.Text);
        }

        public void OnPropertyChanged(string name)
        {
            PropertyChangedEventHandler handler = PropertyChanged;
            if (handler != null)
                handler(this, new PropertyChangedEventArgs(name));
        }
    }
}
