using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;

namespace PhotoEditor
{
    public class RGBPixel : INotifyPropertyChanged
    {
        public Point Position { get; set; }

        public int X { get { return (int)Position.X; } }

        public int Y { get { return (int)Position.Y; } }

        public byte oldRed;
        public byte oldGreen;
        public byte oldBlue;

        private byte red;
        public byte Red
        {
            get { return red; }
            set
            {
                red = value;
                OnPropertyChanged("Red");
                OnPropertyChanged("Brush");
            }
        }

        private byte green;
        public byte Green
        {
            get { return green; }
            set
            {
                green = value;
                OnPropertyChanged("Green");
                OnPropertyChanged("Brush");
            }
        }

        private byte blue;
        public byte Blue
        {
            get { return blue; }
            set
            {
                blue = value;
                OnPropertyChanged("Blue");
                OnPropertyChanged("Brush");
            }
        }

        public int ColorValue
        {
            get
            {
                int output = Red << 16;
                output |= Green << 8;
                output |= Blue;
                return output;
            }
        }

        public Brush Brush
        {
            get { return new SolidColorBrush(Color.FromRgb(Red, Green, Blue)); }
        }

        public RGBPixel() { }

        public RGBPixel(Point position, byte red, byte green, byte blue)
        {
            Position = position;
            Red = oldRed = red;
            Green = oldGreen = green;
            Blue = oldBlue = blue;
        }

        public RGBPixel(byte red, byte green, byte blue)
        {
            Red = oldRed = red;
            Green = oldGreen = green;
            Blue = oldBlue = blue;
        }

        public RGBPixel(Point position)
        {
            Position = position;
            Red = oldRed = 0;
            Green = oldGreen = 0;
            Blue = oldBlue = 0;
        }

        public event PropertyChangedEventHandler PropertyChanged;

        public void OnPropertyChanged(string name)
        {
            PropertyChangedEventHandler handler = PropertyChanged;
            if (handler != null)
                handler(this, new PropertyChangedEventArgs(name));
        }

        public void Add(RGBPixel value)
        {
            Red = (byte)(truncate(oldRed + value.Red));
            Green = (byte)(truncate(oldGreen + value.Green));
            Blue = (byte)(truncate(oldBlue + value.Blue));
        }

        public void Subtract(RGBPixel value)
        {
            Red = (byte)(truncate(oldRed - value.Red));
            Green = (byte)(truncate(oldGreen - value.Green));
            Blue = (byte)(truncate(oldBlue - value.Blue));
        }

        public void Multiply(RGBPixel value)
        {
            Red = (byte)(truncate(oldRed * value.Red));
            Green = (byte)(truncate(oldGreen * value.Green));
            Blue = (byte)(truncate(oldBlue * value.Blue));
        }

        public void Divide(RGBPixel value)
        {
            Red = value.Red != 0 ? (byte)(truncate(oldRed / value.Red)) : (byte)255;
            Green = value.Green != 0 ? (byte)(truncate(oldGreen / value.Green)) : (byte)255;
            Blue = value.Blue != 0 ? (byte)(truncate(oldBlue / value.Blue)) : (byte)255;
        }
        
        public void ChangeBrightness(int value)
        {
            Red = (byte)(truncate(value + oldRed));
            Green = (byte)(truncate(value + oldGreen));
            Blue = (byte)(truncate(value + oldBlue));
        }

        private int truncate(int value)
        {
            if (value > 255)
                return 255;
            else if (value < 0)
                return 0;
            else
                return value;
        }

        public void ToGrayscale1()
        {
            Red = Green = Blue = oldRed;
        }

        public void ToGrayscale2()
        {
            byte average = (byte)((oldRed + oldGreen + oldBlue) / 3);
            Red = Green = Blue = average;
        }

        public void ApplyFilter(Mask mask, RGBPixel[,] neighbours)
        {
            int size = mask.Size;
            int newRed = 0, newGreen = 0, newBlue = 0;
            for (int i = 0; i < size; i++)
            {
                for (int j = 0; j < size; j++)
                {
                    newRed += neighbours[i, j].oldRed * mask.Matrix[i, j];
                    newGreen += neighbours[i, j].oldGreen * mask.Matrix[i, j];
                    newBlue += neighbours[i, j].oldBlue * mask.Matrix[i, j];
                }
            }
            int maskSum = mask.Sum;
            if (maskSum == 0)
                maskSum = 1;
            Red = (byte)(truncate(newRed / maskSum));
            Green = (byte)(truncate(newGreen / maskSum));
            Blue = (byte)(truncate(newBlue / maskSum));
        }

        public void ApplyMedianFilter(RGBPixel[,] neighbours)
        {
            int size = 3, k = 0;
            RGBPixel[] array = new RGBPixel[size * size];

            for (int i = 0; i < size; i++)
                for (int j = 0; j < size; j++)
                    array[k++] = neighbours[i, j];
            
            Array.Sort(array, delegate(RGBPixel pixel1, RGBPixel pixel2)
            {
                return pixel1.ColorValue.CompareTo(pixel2.ColorValue);
            });

            RGBPixel median = array[size * size / 2];
            Red = median.Red;
            Green = median.Green;
            Blue = median.Blue;
        }

        public void ApplySobelFilter(RGBPixel[,] neighbours)
        {
            Mask vertical = Mask.SobelVertical;
            Mask horizontal = Mask.SobelHorizontal;

            int size = vertical.Size;
            int newRedVertical = 0, newGreenVertical = 0, newBlueVertical = 0;
            int newRedHorizontal = 0, newGreenHorizontal = 0, newBlueHorizontal = 0;
            for (int i = 0; i < size; i++)
            {
                for (int j = 0; j < size; j++)
                {
                    newRedVertical += neighbours[i, j].oldRed * vertical.Matrix[i, j];
                    newGreenVertical += neighbours[i, j].oldGreen * vertical.Matrix[i, j];
                    newBlueVertical += neighbours[i, j].oldBlue * vertical.Matrix[i, j];

                    newRedHorizontal += neighbours[i, j].oldRed * horizontal.Matrix[i, j];
                    newGreenHorizontal += neighbours[i, j].oldGreen * horizontal.Matrix[i, j];
                    newBlueHorizontal += neighbours[i, j].oldBlue * horizontal.Matrix[i, j];
                }
            }
            int newRed = (int)Math.Sqrt(newRedHorizontal * newRedHorizontal + newRedVertical * newRedVertical);
            int newGreen = (int)Math.Sqrt(newGreenHorizontal * newGreenHorizontal + newGreenVertical * newGreenVertical);
            int newBlue = (int)Math.Sqrt(newBlueHorizontal * newBlueHorizontal + newBlueVertical * newBlueVertical);

            Red = (byte)(truncate(newRed));
            Green = (byte)(truncate(newGreen));
            Blue = (byte)(truncate(newBlue));
        }

        public void Reset()
        {
            Red = oldRed;
            Green = oldGreen;
            Blue = oldBlue;
        }

        public void FromColorValue(int colorValue)
        {
            Red = (byte)((colorValue >> 16) & 0xFF);
            Green = (byte)((colorValue >> 8) & 0xFF);
            Blue = (byte)(colorValue & 0xFF);
        }

        public void ChangeColors(int red, int green, int blue)
        {
            Red = (byte)truncate(red);
            Green = (byte)truncate(green);
            Blue = (byte)truncate(blue);
        }

        public void ChangeColors(byte value)
        {
            Red = Green = Blue = value;
        }

        public void ApplyDilationFilter(BinaryMask mask, RGBPixel[,] neighbours)
        {
            int size = mask.Size;
            byte maxRed = oldRed, maxGreen = oldGreen, maxBlue = oldBlue;
            for (int i = 0; i < size; i++)
            {
                for (int j = 0; j < size; j++)
                {
                    if (mask.Matrix[i, j])
                    {
                        RGBPixel neighbour = neighbours[i, j];
                        if (neighbour.Red > maxRed)
                            maxRed = neighbour.oldRed;
                        if (neighbour.Green > maxGreen)
                            maxGreen = neighbour.oldGreen;
                        if (neighbour.Blue > maxBlue)
                            maxBlue = neighbour.oldBlue;
                    }
                }
            }
            Red = maxRed;
            Green = maxGreen;
            Blue = maxBlue;
        }

        public void ApplyErosionFilter(BinaryMask mask, RGBPixel[,] neighbours)
        {
            int size = mask.Size;
            byte minRed = oldRed, minGreen = oldGreen, minBlue = oldBlue;
            for (int i = 0; i < size; i++)
            {
                for (int j = 0; j < size; j++)
                {
                    if (mask.Matrix[i, j])
                    {
                        RGBPixel neighbour = neighbours[i, j];
                        if (neighbour.oldRed < minRed)
                            minRed = neighbour.oldRed;
                        if (neighbour.oldGreen < minGreen)
                            minGreen = neighbour.oldGreen;
                        if (neighbour.oldBlue < minBlue)
                            minBlue = neighbour.oldBlue;
                    }
                }
            }
            Red = minRed;
            Green = minGreen;
            Blue = minBlue;
        }

        public void ApplyHitOrMissFilter(Mask mask, RGBPixel[,] neighbours)
        {
            int size = mask.Size;
            byte MAX_COLOR = 255, MIN_COLOR = 0;
            bool fits = true;

            for (int i = 0; i < size; i++)
            {
                for (int j = 0; j < size; j++)
                {
                    RGBPixel neighbour = neighbours[i, j];
                    if (mask.Matrix[i, j] == 1)
                    {
                        if (neighbour.oldRed != MAX_COLOR || neighbour.oldGreen != MAX_COLOR || neighbour.oldBlue != MAX_COLOR)
                        {
                            fits = false;
                            break;
                        }
                    }
                    else if (mask.Matrix[i, j] == -1)
                    {
                        if (neighbour.oldRed != MIN_COLOR || neighbour.oldGreen != MIN_COLOR || neighbour.oldBlue != MIN_COLOR)
                        {
                            fits = false;
                            break;
                        }
                    }
                }
                if (!fits)
                    break;
            }

            if (fits)
            {
                Red = MAX_COLOR;
                Green = MAX_COLOR;
                Blue = MAX_COLOR;
            }
            else
            {
                Red = MIN_COLOR;
                Green = MIN_COLOR;
                Blue = MIN_COLOR;
            }
        }

        public bool Thin(Mask mask, RGBPixel[,] neighbours)
        {
            int size = mask.Size;
            byte MAX_COLOR = 255, MIN_COLOR = 0;
            bool fits = true;

            for (int i = 0; i < size; i++)
            {
                for (int j = 0; j < size; j++)
                {
                    RGBPixel neighbour = neighbours[i, j];
                    if (mask.Matrix[i, j] == 1)
                    {
                        if (neighbour.oldRed != MAX_COLOR || neighbour.oldGreen != MAX_COLOR || neighbour.oldBlue != MAX_COLOR)
                        {
                            fits = false;
                            break;
                        }
                    }
                    else if (mask.Matrix[i, j] == -1)
                    {
                        if (neighbour.oldRed != MIN_COLOR || neighbour.oldGreen != MIN_COLOR || neighbour.oldBlue != MIN_COLOR)
                        {
                            fits = false;
                            break;
                        }
                    }
                }
                if (!fits)
                    break;
            }

            if (fits)
            {
                Red = MIN_COLOR;
                Green = MIN_COLOR;
                Blue = MIN_COLOR;
            }
            return fits;
        }

        public bool Thicken(Mask mask, RGBPixel[,] neighbours)
        {
            int size = mask.Size;
            byte MAX_COLOR = 255, MIN_COLOR = 0;
            bool fits = true;

            for (int i = 0; i < size; i++)
            {
                for (int j = 0; j < size; j++)
                {
                    RGBPixel neighbour = neighbours[i, j];
                    if (mask.Matrix[i, j] == 1)
                    {
                        if (neighbour.oldRed != MAX_COLOR || neighbour.oldGreen != MAX_COLOR || neighbour.oldBlue != MAX_COLOR)
                        {
                            fits = false;
                            break;
                        }
                    }
                    else if (mask.Matrix[i, j] == -1)
                    {
                        if (neighbour.oldRed != MIN_COLOR || neighbour.oldGreen != MIN_COLOR || neighbour.oldBlue != MIN_COLOR)
                        {
                            fits = false;
                            break;
                        }
                    }
                }
                if (!fits)
                    break;
            }

            if (fits)
            {
                Red = MAX_COLOR;
                Green = MAX_COLOR;
                Blue = MAX_COLOR;
            }
            return fits;
        }

        public void Update()
        {
            oldRed = Red;
            oldGreen = Green;
            oldBlue = Blue;
        }

        public void Clear()
        {
            Red = oldRed = 0;
            Green = oldGreen = 0;
            Blue = oldBlue = 0;
        }

        public void LogicalSubtraction(RGBPixel value)
        {
            byte MAX_COLOR = 255, MIN_COLOR = 0;
            if (oldRed == MAX_COLOR && value.Red != MAX_COLOR)
            {
                Red = MAX_COLOR;
                Green = MAX_COLOR;
                Blue = MAX_COLOR;
            }
            else
            {
                Red = MIN_COLOR;
                Green = MIN_COLOR;
                Blue = MIN_COLOR;
            }
        }

        public RGBPixel GetCopy()
        {
            return new RGBPixel(Position, Red, Green, Blue);
        }

        public void Invert()
        {
            byte MAX_COLOR = 255, MIN_COLOR = 0;
            if (Red == MAX_COLOR)
                Red = MIN_COLOR;
            else if (Red == MIN_COLOR)
                Red = MAX_COLOR;
            Green = Blue = Red;
        }
    }
}
