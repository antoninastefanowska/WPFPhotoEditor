using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.IO;
using System.Diagnostics;
using System.Text.RegularExpressions;
using System.ComponentModel;

namespace PhotoEditor
{
    public partial class MainWindow : Window, INotifyPropertyChanged
    {
        private class FinishedException : Exception
        {
            public FinishedException() : base() {  }
            public FinishedException(string message) : base(message) { }
        }

        private enum Format
        {
            P3,
            P6
        }

        private static readonly int BUFFER_SIZE = 1024;
        private static readonly double SCALE_RATE = 1.1;

        private int width, height, maxColor;
        private Format fileFormat;
        private WriteableBitmap outBitmap;
        private BitmapSource originalBitmap;
        private int cursorX, cursorY;

        private ColorPickWindow colorPickWindow;
        private ValuePickWindow valuePickWindow;
        private RGBPixel rgbColor;
        private RGBPixel[,] pixelMatrix;

        private int[] grayscaleHistogram = null;

        public event PropertyChangedEventHandler PropertyChanged;

        private int quality;
        public int Quality
        {
            get { return quality; }
            set
            {
                quality = value;
                OnPropertyChanged("Quality");
            }
        }
        
        public MainWindow()
        {
            InitializeComponent();
            Quality = 100;
            DataContext = this;
        }

        public void OnPropertyChanged(string name)
        {
            PropertyChangedEventHandler handler = PropertyChanged;
            if (handler != null)
                handler(this, new PropertyChangedEventArgs(name));
        }

        public void ImportButton_OnClick(object sender, RoutedEventArgs e)
        {
            OpenFileDialog dialog = new OpenFileDialog();
            dialog.Filter = "PPM File (*.ppm;*.pbm)|*.ppm;*.pbm|JPEG File (*.jpg;*.jpeg)|*.jpg;*.jpeg";

            if (dialog.ShowDialog() == true)
            {
                Canvas.Children.Clear();
                string path = dialog.FileName;
                string extension = Path.GetExtension(path);
                FileNameLabel.Content = Path.GetFileName(path);

                if (extension.Equals(".ppm") || extension.Equals(".pbm"))
                {
                    try
                    {
                        LoadPPMFile(path);
                    }
                    catch (FormatException)
                    {
                        ShowMessage("Nieprawidłowy format pliku.");
                    }
                }
                else if (extension.Equals(".jpeg") || extension.Equals(".jpg"))
                {
                    LoadJPGFile(path);
                }
            }
        }

        public void ExportButton_OnClick(object sender, RoutedEventArgs e)
        {
            SaveFileDialog dialog = new SaveFileDialog();
            dialog.Filter = "JPEG File (*.jpg)|*.jpg";

            if (dialog.ShowDialog() == true)
            {
                string path = dialog.FileName;
                FileStream stream = new FileStream(path, FileMode.Create);
                JpegBitmapEncoder encoder = new JpegBitmapEncoder();
                encoder.QualityLevel = Quality;
                Image image = Canvas.Children[0] as Image;
                encoder.Frames.Add(BitmapFrame.Create(image.Source as BitmapSource));
                encoder.Save(stream);
                stream.Close();
            }
        }

        public void Window_MouseWheel(object sender, MouseWheelEventArgs e)
        {
            if (e.Delta > 0)
            {
                CanvasScale.ScaleX *= SCALE_RATE;
                CanvasScale.ScaleY *= SCALE_RATE;
            }
            else
            {
                CanvasScale.ScaleX /= SCALE_RATE;
                CanvasScale.ScaleY /= SCALE_RATE;
            }
        }

        public void LoadJPGFile(string path)
        {
            FileStream stream = new FileStream(path, FileMode.Open, FileAccess.Read);
            JpegBitmapDecoder decoder = new JpegBitmapDecoder(stream, BitmapCreateOptions.None, BitmapCacheOption.Default);
            BitmapSource bitmap = decoder.Frames[0];

            Image image = new Image();
            height = bitmap.PixelHeight;
            width = bitmap.PixelWidth;
            image.Source = bitmap;
            Canvas.Children.Add(image);
            originalBitmap = bitmap;
            ReadPixelMatrix();
        }

        public void LoadPPMFile(string path)
        {
            FileStream stream = new FileStream(path, FileMode.Open, FileAccess.Read);
            byte[] buffer = new byte[BUFFER_SIZE], chunk;

            int bytesToRead = (int)stream.Length;
            int bytesRead = 0;
            int n;

            LoadedPixelColor unfinishedColor = null;

            cursorX = cursorY = 0;

            n = stream.Read(buffer, 0, BUFFER_SIZE);
            chunk = initializePPM(buffer);

            try
            {
                if (fileFormat == Format.P3)
                    unfinishedColor = processP3Data(chunk, (P3PixelColor)unfinishedColor);
                else
                    unfinishedColor = processP6Data(chunk, (P6PixelColor)unfinishedColor);
            }
            catch (FinishedException) { }

            do
            {
                bytesRead += n;
                bytesToRead -= n;

                n = stream.Read(buffer, 0, BUFFER_SIZE);
                if (n == 0)
                    break;

                try
                {
                    if (fileFormat == Format.P3)
                        unfinishedColor = processP3Data(buffer, (P3PixelColor)unfinishedColor);
                    else
                        unfinishedColor = processP6Data(buffer, (P6PixelColor)unfinishedColor);
                }
                catch (FinishedException) { break; }
            } while (bytesToRead > 0);
            stream.Close();
            CreateImage();
        }

        private byte[] initializePPM(byte[] buffer)
        {
            string block = Encoding.Default.GetString(buffer);
            block = Regex.Replace(block, @"#.*", "");

            string[] segments = block.Split(new char[0], 5, StringSplitOptions.RemoveEmptyEntries);

            string type = segments[0];
            width = Convert.ToInt32(segments[1]);
            height = Convert.ToInt32(segments[2]);
            maxColor = Convert.ToInt32(segments[3]);

            outBitmap = new WriteableBitmap(width, height, 96, 96, PixelFormats.Bgr32, null);
            outBitmap.Lock();

            if (type.Equals("P3"))
                fileFormat = Format.P3;
            else if (type.Equals("P6"))
                fileFormat = Format.P6;

            return Encoding.Default.GetBytes(segments[4]);
        }

        public void CreateImage()
        {
            outBitmap.Unlock();
            Image image = new Image();
            image.Source = outBitmap;
            RenderOptions.SetBitmapScalingMode(image, BitmapScalingMode.NearestNeighbor);
            Canvas.Children.Add(image);
            originalBitmap = outBitmap;
            ReadPixelMatrix();
        }

        public void UpdateImage()
        {
            outBitmap.Unlock();
            Image image = Canvas.Children[0] as Image;
            image.Source = outBitmap;
        }

        public void UpdatePixelMatrix()
        {
            for (int x = 0; x < width; x++)
                for (int y = 0; y < height; y++)
                    pixelMatrix[x, y].Update();
        }

        public void ClearModifications()
        {
            Image image = Canvas.Children[0] as Image;
            image.Source = originalBitmap;
            ReadPixelMatrix();
        }

        private P3PixelColor processP3Data(byte[] chunk, P3PixelColor unfinishedColor)
        {
            string block = Encoding.Default.GetString(chunk);
            bool next = false;
            P3PixelColor currentColor = unfinishedColor == null ? new P3PixelColor(maxColor) : unfinishedColor;

            foreach (char character in block)
            {
                if (currentColor.IsReady)
                {
                    DrawPixel(currentColor);
                    currentColor = new P3PixelColor(maxColor);
                }
                if (!Char.IsWhiteSpace(character))
                {
                    next = false;
                    currentColor.AddCharacter(character);
                }
                else if (!next)
                {
                    next = true;
                    currentColor.FinalizeSingleComponent();
                }
            }

            return currentColor;
        }

        private P6PixelColor processP6Data(byte[] chunk, P6PixelColor unfinishedColor)
        {
            P6PixelColor currentColor = unfinishedColor == null ? new P6PixelColor(maxColor) : unfinishedColor;

            foreach (byte value in chunk)
            {
                if (currentColor.IsReady)
                {
                    DrawPixel(currentColor);
                    currentColor = new P6PixelColor(maxColor);
                }
                currentColor.AddByte(value);
                currentColor.FinalizeSingleComponent();
            }

            return currentColor;
        }

        public void DrawPixel(LoadedPixelColor color)
        {
            Int32Rect rectangle = new Int32Rect(cursorX, cursorY, 1, 1);
            unsafe
            {
                int colorValue = color.CalculateColor();
                IntPtr backBuffer = outBitmap.BackBuffer;
                backBuffer += cursorY * outBitmap.BackBufferStride;
                backBuffer += cursorX * 4;
                *((int*)backBuffer) = colorValue;
            }
            outBitmap.AddDirtyRect(rectangle);
            updateCursor();
        }

        public void DrawPixel(RGBPixel pixel)
        {
            Int32Rect rectangle = new Int32Rect(pixel.X, pixel.Y, 1, 1);
            unsafe
            {
                int colorValue = pixel.ColorValue;
                IntPtr backBuffer = outBitmap.BackBuffer;
                backBuffer += pixel.Y * outBitmap.BackBufferStride;
                backBuffer += pixel.X * 4;
                *((int*)backBuffer) = colorValue;
            }
            outBitmap.AddDirtyRect(rectangle);
        }

        private void updateCursor()
        {
            cursorX = (cursorX + 1) % width;
            if (cursorX == 0)
                cursorY++;
            if (cursorY >= height)
                throw new FinishedException();
        }

        public void ShowMessage(string message)
        {
            MessageBox.Show(message, "Błąd", MessageBoxButton.OK, MessageBoxImage.Error, MessageBoxResult.No);
        }

        private void Dialog_OnClosed(object sender, EventArgs e)
        {
            UpdatePixelMatrix();
        }

        private void MenuAdd_OnClick(object sender, RoutedEventArgs e)
        {
            rgbColor = new RGBPixel(0, 0, 0);
            rgbColor.PropertyChanged += AddColorPicker_OnChanged;
            colorPickWindow = new ColorPickWindow(rgbColor);
            colorPickWindow.Closed += Dialog_OnClosed;
            colorPickWindow.Owner = this;
            colorPickWindow.Show();
        }

        private void MenuSubtract_OnClick(object sender, RoutedEventArgs e)
        {
            rgbColor = new RGBPixel(0, 0, 0);
            rgbColor.PropertyChanged += SubtractColorPicker_OnChanged;
            colorPickWindow = new ColorPickWindow(rgbColor);
            colorPickWindow.Closed += Dialog_OnClosed;
            colorPickWindow.Owner = this;
            colorPickWindow.Show();
        }

        private void MenuMultiply_OnClick(object sender, RoutedEventArgs e)
        {
            rgbColor = new RGBPixel(1, 1, 1);
            rgbColor.PropertyChanged += MultiplyColorPicker_OnChanged;
            colorPickWindow = new ColorPickWindow(rgbColor);
            colorPickWindow.Closed += Dialog_OnClosed;
            colorPickWindow.Owner = this;
            colorPickWindow.Show();
        }

        private void MenuDivide_OnClick(object sender, RoutedEventArgs e)
        {
            rgbColor = new RGBPixel(1, 1, 1);
            rgbColor.PropertyChanged += DivideColorPicker_OnChanged;
            colorPickWindow = new ColorPickWindow(rgbColor);
            colorPickWindow.Closed += Dialog_OnClosed;
            colorPickWindow.Owner = this;
            colorPickWindow.Show();
        }

        private void MenuChangeBrightness_OnClick(object sender, RoutedEventArgs e)
        {
            valuePickWindow = new ValuePickWindow();
            valuePickWindow.PropertyChanged += BrightenValuePicker_OnChanged;
            valuePickWindow.Closed += Dialog_OnClosed;
            valuePickWindow.Owner = this;
            valuePickWindow.Show();
        }

        private void MenuGrayscale1_OnClick(object sender, RoutedEventArgs e)
        {
            GrayscaleOperation1();
        }

        private void MenuGrayscale2_OnClick(object sender, RoutedEventArgs e)
        {
            GrayscaleOperation2();
        }

        private void MenuLowPass_OnClick(object sender, RoutedEventArgs e)
        {
            FilterOperation(Mask.LowPass);
        }

        private void MenuMedian_OnClick(object sender, RoutedEventArgs e)
        {
            MedianFilterOperation();
        }

        private void MenuSobel_OnClick(object sender, RoutedEventArgs e)
        {
            SobelFilterOperation();
        }

        private void MenuHighPass_OnClick(object sender, RoutedEventArgs e)
        {
            FilterOperation(Mask.HighPass);
        }

        private void MenuGauss_OnClick(object sender, RoutedEventArgs e)
        {
            FilterOperation(Mask.Gauss);
        }

        private void MenuCustom_OnClick(object sender, RoutedEventArgs e)
        {
            MaskSizeWindow maskSizeWindow = new MaskSizeWindow();
            if (maskSizeWindow.ShowDialog() == true)
            {
                if (maskSizeWindow.DialogResult == true)
                {
                    int maskSize = maskSizeWindow.MaskSize;
                    if (maskSize < 0 || maskSize % 2 == 0)
                        ShowMessage("Nieprawidłowy rozmiar maski");
                    else
                    {
                        MaskValuesWindow maskValuesWindow = new MaskValuesWindow(maskSize);
                        if (maskValuesWindow.ShowDialog() == true)
                        {
                            if (maskValuesWindow.DialogResult == true)
                                FilterOperation(maskValuesWindow.Mask);
                        }
                    }
                }
            }
        }

        private void MenuStretchHistogram_OnClick(object sender, RoutedEventArgs e)
        {
            StretchHistogramOperation();
        }

        private void MenuEqualizeHistogram_OnClick(object sender, RoutedEventArgs e)
        {
            EqualizeHistogramOperation();
        }

        private void MenuThresholdBinarization_OnClick(object sender, RoutedEventArgs e)
        {
            valuePickWindow = new ValuePickWindow(0, 255);
            valuePickWindow.PropertyChanged += BinarizationThresholdValuePicker_OnChanged;
            valuePickWindow.Closed += Dialog_OnClosed;
            valuePickWindow.Owner = this;
            valuePickWindow.Show();
        }

        private void MenuPercentageBinarization_OnClick(object sender, RoutedEventArgs e)
        {
            valuePickWindow = new ValuePickWindow(0, 100);
            valuePickWindow.PropertyChanged += BinarizationPercentageValuePicker_OnChanged;
            valuePickWindow.Closed += Dialog_OnClosed;
            valuePickWindow.Owner = this;
            valuePickWindow.Show();
        }

        private void MenuMeanBinarization_OnClick(object sender, RoutedEventArgs e)
        {
            BinarizationMeanIterativeSelectionOperation();
        }

        private void MenuMinimumErrorBinarization_OnClick(object sender, RoutedEventArgs e)
        {
            BinarizationMinimumErrorOperation();
        }

        private void MenuEntropyBinarization_OnClick(object sender, RoutedEventArgs e)
        {
            BinarizationEntropyOperation();
        }

        private void MenuDilation_OnClick(object sender, RoutedEventArgs e)
        {
            DilationOperation(BinaryMask.Standard);
        }

        private void MenuErosion_OnClick(object sender, RoutedEventArgs e)
        {
            ErosionOperation(BinaryMask.Standard);
        }

        private void MenuOpening_OnClick(object sender, RoutedEventArgs e)
        {
            OpeningOperation(BinaryMask.Standard);
        }

        private void MenuClosing_OnClick(object sender, RoutedEventArgs e)
        {
            ClosingOperation(BinaryMask.Standard);
        }

        private void MenuThin_OnClick(object sender, RoutedEventArgs e)
        {
            ThinOperation();
        }

        private void MenuThicken_OnClick(object sender, RoutedEventArgs e)
        {
            ThickenOperation();
        }

        private void AddColorPicker_OnChanged(object sender, PropertyChangedEventArgs e)
        {
            AddOperation(colorPickWindow.RGB);
        }

        private void SubtractColorPicker_OnChanged(object sender, PropertyChangedEventArgs e)
        {
            SubtractOperation(colorPickWindow.RGB);
        }

        private void MultiplyColorPicker_OnChanged(object sender, PropertyChangedEventArgs e)
        {
            MultiplyOperation(colorPickWindow.RGB);
        }

        private void DivideColorPicker_OnChanged(object sender, PropertyChangedEventArgs e)
        {
            DivideOperation(colorPickWindow.RGB);
        }

        private void BrightenValuePicker_OnChanged(object sender, PropertyChangedEventArgs e)
        {
            ChangeBrightnessOperation(valuePickWindow.Value);
        }

        private void BinarizationThresholdValuePicker_OnChanged(object sender, PropertyChangedEventArgs e)
        {
            BinarizationOperation(valuePickWindow.Value);
        }

        private void BinarizationPercentageValuePicker_OnChanged(object sender, PropertyChangedEventArgs e)
        {
            BinarizationPercentBlackSelectionOperation((double)valuePickWindow.Value / 100);
        }

        private void Clear_OnClick(object sender, RoutedEventArgs e)
        {
            ClearModifications();
        }

        public RGBPixel GetPixel(Point position)
        {
            Image image = Canvas.Children[0] as Image;
            BitmapSource source = image.Source as BitmapSource;
            CroppedBitmap fragment = new CroppedBitmap(source, new Int32Rect((int)position.X, (int)position.Y, 1, 1));
            byte[] color = new byte[source.Format.BitsPerPixel / 8];
            fragment.CopyPixels(color, source.Format.BitsPerPixel / 8, 0);

            return new RGBPixel(position, color[2], color[1], color[0]);
        }

        public void ReadPixelMatrix()
        {
            int x = 0, y = 0;
            int stride = (originalBitmap.PixelWidth * originalBitmap.Format.BitsPerPixel + 7) / 8;
            byte[] pixelArray = new byte[originalBitmap.PixelHeight * stride];
            originalBitmap.CopyPixels(pixelArray, stride, 0);

            pixelMatrix = new RGBPixel[width, height];
        
            for (int i = 0; i <= pixelArray.Length - 4; i += 4)
            {
                Point position = new Point(x, y);
                RGBPixel pixel = new RGBPixel(position, pixelArray[i + 2], pixelArray[i + 1], pixelArray[i]);
                pixelMatrix[x, y] = pixel;
                x = (x + 1) % width;
                if (x == 0)
                    y++;
            }
            grayscaleHistogram = null;
        }

        public void AddOperation(RGBPixel value)
        {
            outBitmap = new WriteableBitmap(originalBitmap);
            outBitmap.Lock();

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    RGBPixel pixel = pixelMatrix[x, y];
                    pixel.Add(value);
                    DrawPixel(pixel);
                }
            }
            UpdateImage();
        }

        public void SubtractOperation(RGBPixel value)
        {
            outBitmap = new WriteableBitmap(originalBitmap);
            outBitmap.Lock();
            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    RGBPixel pixel = pixelMatrix[x, y];
                    pixel.Subtract(value);
                    DrawPixel(pixel);
                }
            }
            UpdateImage();
        }

        public void MultiplyOperation(RGBPixel value)
        {
            outBitmap = new WriteableBitmap(originalBitmap);
            outBitmap.Lock();
            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    RGBPixel pixel = pixelMatrix[x, y];
                    pixel.Multiply(value);
                    DrawPixel(pixel);
                }
            }
            UpdateImage();
        }

        public void DivideOperation(RGBPixel value)
        {
            outBitmap = new WriteableBitmap(originalBitmap);
            outBitmap.Lock();
            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    RGBPixel pixel = pixelMatrix[x, y];
                    pixel.Divide(value);
                    DrawPixel(pixel);
                }
            }
            UpdateImage();
        }

        public void ChangeBrightnessOperation(int value)
        {
            outBitmap = new WriteableBitmap(originalBitmap);
            outBitmap.Lock();
            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    RGBPixel pixel = pixelMatrix[x, y];
                    pixel.ChangeBrightness(value);
                    DrawPixel(pixel);
                }
            }
            UpdateImage();
        }

        public void GrayscaleOperation1()
        {
            outBitmap = new WriteableBitmap(originalBitmap);
            outBitmap.Lock();
            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    RGBPixel pixel = pixelMatrix[x, y];
                    pixel.ToGrayscale1();
                    DrawPixel(pixel);
                }
            }
            UpdateImage();
            UpdatePixelMatrix();
        }

        public void GrayscaleOperation2()
        {
            outBitmap = new WriteableBitmap(originalBitmap);
            outBitmap.Lock();
            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    RGBPixel pixel = pixelMatrix[x, y];
                    pixel.ToGrayscale2();
                    DrawPixel(pixel);
                }
            }
            UpdateImage();
            UpdatePixelMatrix();
        }

        public void FilterOperation(Mask mask)
        {
            outBitmap = new WriteableBitmap(originalBitmap);
            outBitmap.Lock();
            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    RGBPixel pixel = pixelMatrix[x, y];
                    RGBPixel[,] neighbours = GetNeighbours(pixel, mask.Size);
                    pixel.ApplyFilter(mask, neighbours);
                    DrawPixel(pixel);
                }
            }
            UpdateImage();
            UpdatePixelMatrix();
        }

        public void MedianFilterOperation()
        {
            outBitmap = new WriteableBitmap(originalBitmap);
            outBitmap.Lock();
            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    RGBPixel pixel = pixelMatrix[x, y];
                    RGBPixel[,] neighbours = GetNeighbours(pixel, 3);
                    pixel.ApplyMedianFilter(neighbours);
                    DrawPixel(pixel);
                }
            }
            UpdateImage();
            UpdatePixelMatrix();
        }

        public void SobelFilterOperation()
        {
            outBitmap = new WriteableBitmap(originalBitmap);
            outBitmap.Lock();
            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    RGBPixel pixel = pixelMatrix[x, y];
                    RGBPixel[,] neighbours = GetNeighbours(pixel, 3);
                    pixel.ApplySobelFilter(neighbours);
                    DrawPixel(pixel);
                }
            }
            UpdateImage();
            UpdatePixelMatrix();
        }

        public RGBPixel[,] GetNeighbours(RGBPixel pixel, int maskSize)
        {
            RGBPixel[,] neighbours = new RGBPixel[maskSize, maskSize];
            for (int i = maskSize / 2; i >= -maskSize / 2; i--)
            {
                for (int j = maskSize / 2; j >= -maskSize / 2; j--)
                {
                    if (pixel.X - i < 0 || pixel.X - i >= width || pixel.Y - j < 0 || pixel.Y - j >= height)
                        neighbours[i + maskSize / 2, j + maskSize / 2] = new RGBPixel(0, 0, 0);
                    else
                        neighbours[i + maskSize / 2, j + maskSize / 2] = pixelMatrix[pixel.X - i, pixel.Y - j];
                }
            }
            return neighbours;
        }

        public byte[] GetMinMax()
        {
            byte rMin, rMax, gMin, gMax, bMin, bMax;
            rMin = rMax = pixelMatrix[0, 0].Red;
            gMin = gMax = pixelMatrix[0, 0].Green;
            bMin = bMax = pixelMatrix[0, 0].Blue;
            
            for (int x = 1; x < width; x++)
            {
                for (int y = 1; y < height; y++)
                {
                    RGBPixel pixel = pixelMatrix[x, y];
                    if (pixel.Red < rMin)
                        rMin = pixel.Red;
                    else if (pixel.Red > rMax)
                        rMax = pixel.Red;

                    if (pixel.Green < gMin)
                        gMin = pixel.Green;
                    else if (pixel.Green > gMax)
                        gMax = pixel.Green;

                    if (pixel.Blue < bMin)
                        bMin = pixel.Blue;
                    else if (pixel.Blue > bMax)
                        bMax = pixel.Blue;
                }
            }
            byte[] output = new byte[6];
            output[0] = rMin;
            output[1] = rMax;
            output[2] = gMin;
            output[3] = gMax;
            output[4] = bMin;
            output[5] = bMax;

            return output;
        }

        public Tuple<int[], int[], int[]> GetHistogram()
        {
            byte MAX_COLOR = 0xFF;
            int[] rH = new int[MAX_COLOR + 1], gH = new int[MAX_COLOR + 1], bH = new int[MAX_COLOR + 1];
            
            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    RGBPixel pixel = pixelMatrix[x, y];
                    rH[pixel.Red]++;
                    gH[pixel.Green]++;
                    bH[pixel.Blue]++;
                }
            }
            return new Tuple<int[], int[], int[]>(rH, gH, bH);
        }

        public int[] GetGrayscaleHistogram()
        {
            byte MAX_COLOR = 0xFF;
            int[] h = new int[MAX_COLOR + 1];
            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    RGBPixel pixel = pixelMatrix[x, y];
                    pixel.ToGrayscale2();
                    h[pixel.Red]++;
                }
            }
            return h;
        }

        public void ApplyLUT(int[] rLUT, int[] gLUT, int[] bLUT)
        {
            outBitmap = new WriteableBitmap(originalBitmap);
            outBitmap.Lock();
            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    RGBPixel pixel = pixelMatrix[x, y];
                    pixel.ChangeColors(rLUT[pixel.Red], gLUT[pixel.Green], bLUT[pixel.Blue]);
                    DrawPixel(pixel);
                }
            }
            UpdateImage();
            UpdatePixelMatrix();
        }

        public void StretchHistogramOperation()
        {
            byte[] minMax = GetMinMax();
            byte rMin = minMax[0], rMax = minMax[1];
            byte gMin = minMax[2], gMax = minMax[3];
            byte bMin = minMax[4], bMax = minMax[5];
            
            byte MAX_COLOR = 0xFF;
            int[] rLUT = new int[MAX_COLOR + 1], gLUT = new int[MAX_COLOR + 1], bLUT = new int[MAX_COLOR + 1];

            for (int i = 0; i < MAX_COLOR + 1; i++)
            {
                rLUT[i] = (int)Math.Round(((double)MAX_COLOR / (rMax - rMin)) * (i - rMin));
                gLUT[i] = (int)Math.Round(((double)MAX_COLOR / (gMax - gMin)) * (i - gMin));
                bLUT[i] = (int)Math.Round(((double)MAX_COLOR / (bMax - bMin)) * (i - bMin)); 
            }
            ApplyLUT(rLUT, gLUT, bLUT);
        }

        public void EqualizeHistogramOperation()
        {
            Tuple<int[], int[], int[]> histogram = GetHistogram();
            int[] rH = histogram.Item1, gH = histogram.Item2, bH = histogram.Item3;
            byte MAX_COLOR = 0xFF;
            
            int rSum = 0, gSum = 0, bSum = 0;
            double[] rD = new double[MAX_COLOR + 1], gD = new double[MAX_COLOR + 1], bD = new double[MAX_COLOR + 1];
            double rD0 = 0, gD0 = 0, bD0 = 0;
            int[] rLUT = new int[MAX_COLOR + 1], gLUT = new int[MAX_COLOR + 1], bLUT = new int[MAX_COLOR + 1];
            int pixelNumber = height * width;
            

            for (int i = 0; i < MAX_COLOR + 1; i++)
            {
                rSum += rH[i];
                gSum += gH[i];
                bSum += bH[i];

                rD[i] = (double)rSum / pixelNumber;
                gD[i] = (double)gSum / pixelNumber;
                bD[i] = (double)bSum / pixelNumber;

                if (rD0 == 0 && rD[i] != 0.0)
                    rD0 = rD[i];
                if (gD0 == 0 && gD[i] != 0.0)
                    gD0 = gD[i];
                if (bD0 == 0 && bD[i] != 0.0)
                    bD0 = bD[i];
            }

            for (int i = 0; i < MAX_COLOR + 1; i++)
            {
                rLUT[i] = (int)Math.Round(((rD[i] - rD0) / (1.0 - rD0)) * MAX_COLOR);
                gLUT[i] = (int)Math.Round(((gD[i] - gD0) / (1.0 - gD0)) * MAX_COLOR);
                bLUT[i] = (int)Math.Round(((bD[i] - bD0) / (1.0 - bD0)) * MAX_COLOR);
            }
            ApplyLUT(rLUT, gLUT, bLUT);
        }

        public void BinarizationOperation(int threshold)
        {
            outBitmap = new WriteableBitmap(originalBitmap);
            outBitmap.Lock();
            byte MAX_COLOR = 0xFF;
            byte[] LUT = new byte[MAX_COLOR + 1];

            for (int i = threshold; i < MAX_COLOR + 1; i++)
                LUT[i] = MAX_COLOR;

            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    RGBPixel pixel = pixelMatrix[x, y];
                    pixel.ToGrayscale2();
                    pixel.ChangeColors(LUT[pixel.Red]);
                    DrawPixel(pixel);
                }
            }
            UpdateImage();
        }

        public void BinarizationPercentBlackSelectionOperation(double percentage)
        {
            if (grayscaleHistogram == null)
                grayscaleHistogram = GetGrayscaleHistogram();
            int sum = 0, pixelNumber = width * height;
            byte MAX_COLOR = 0xFF;

            for (int i = 0; i < MAX_COLOR + 1; i++)
            {
                if (sum >= pixelNumber * percentage)
                {
                    BinarizationOperation(i);
                    break;
                }
                sum += grayscaleHistogram[i];
            }
        }

        public void BinarizationMeanIterativeSelectionOperation()
        {
            if (grayscaleHistogram == null)
                grayscaleHistogram = GetGrayscaleHistogram();
            
            byte MAX_COLOR = 0xFF;
            double meanB = 0, meanW = 0;
            int sumB = 0, sumW = 0;
            int threshold = 0, newThreshold = 0;
            do
            {
                threshold = newThreshold;
                meanB = meanW = 0;
                sumB = sumW = 0;
                for (int j = 0; j < threshold; j++)
                {
                    meanB += (grayscaleHistogram[j] * j);
                    sumB += grayscaleHistogram[j];
                }
                if (sumB != 0)
                    meanB /= sumB;

                for (int j = threshold; j < MAX_COLOR + 1; j++)
                {
                    meanW += (grayscaleHistogram[j] * j);
                    sumW += grayscaleHistogram[j];
                }
                if (sumW != 0)
                    meanW /= sumW;

                newThreshold = (int)((meanB + meanW) / 2);
            } while (newThreshold != threshold);
            BinarizationOperation(threshold);
            UpdatePixelMatrix();
        }

        public void BinarizationMinimumErrorOperation()
        {
            if (grayscaleHistogram == null)
                grayscaleHistogram = GetGrayscaleHistogram();

            byte MAX_COLOR = 0xFF;
            int pixelNumber = height * width;
            double[] h = new double[MAX_COLOR + 1];

            for (int i = 0; i < MAX_COLOR + 1; i++)
                h[i] = (double)grayscaleHistogram[i] / pixelNumber;

            int T, Tmin = 0;
            double P1 = 0, P2 = 0, mean1 = 0, mean2 = 0, sigma1 = 0, sigma2 = 0, Jmin = 0;

            for (T = 0; T < MAX_COLOR + 1; T++)
            {
                P1 = P2 = mean1 = mean2 = sigma1 = sigma2 = 0;

                for (int i = 0; i < T; i++)
                    P1 += h[i];
                for (int i = T; i < MAX_COLOR + 1; i++)
                    P2 += h[i];

                for (int i = 0; i < T; i++)
                    mean1 += h[i] * i;
                mean1 /= P1;

                for (int i = T; i < MAX_COLOR + 1; i++)
                    mean2 += h[i] * i;
                mean2 /= P2;

                for (int i = 0; i < T; i++)
                    sigma1 += h[i] * (i - mean1) * (i - mean1);
                sigma1 = Math.Sqrt(sigma1 / P1);

                for (int i = T; i < MAX_COLOR + 1; i++)
                    sigma2 += h[i] * (i - mean2) * (i - mean2);
                sigma2 = Math.Sqrt(sigma2 / P2);

                double J = 0;
                if (sigma1 > 0 && sigma2 > 0)
                {
                    J = 1 + 2 * (P1 * Math.Log(sigma1) + P2 * Math.Log(sigma2)) - 2 * (P1 * Math.Log(P1) + P2 * Math.Log(P2));

                    if (Tmin == 0 || J < Jmin)
                    {
                        Jmin = J;
                        Tmin = T;
                    }
                }
            }
            BinarizationOperation(Tmin);
            UpdatePixelMatrix();
        }

        public void BinarizationEntropyOperation()
        {
            if (grayscaleHistogram == null)
                grayscaleHistogram = GetGrayscaleHistogram();

            byte MAX_COLOR = 0xFF;
            int pixelNumber = height * width;

            double[] p = new double[MAX_COLOR + 1];

            for (int i = 0; i < MAX_COLOR + 1; i++)
                p[i] = (double)grayscaleHistogram[i] / pixelNumber;

            int T, Tmax = 0;
            double pSum1, pSum2, pLogSum1, pLogSum2, E, Emax = 0;
            for (T = 0; T < MAX_COLOR + 1; T++)
            {
                pSum1 = pSum2 = pLogSum1 = pLogSum2 = 0;

                for (int i = 0; i < T; i++)
                {
                    pSum1 += p[i];
                    if (p[i] != 0)
                        pLogSum1 += p[i] * Math.Log(p[i]);
                }

                for (int i = T; i < MAX_COLOR + 1; i++)
                {
                    pSum2 += p[i];
                    if (p[i] != 0)
                        pLogSum2 += p[i] * Math.Log(p[i]);
                }

                if (pSum1 != 0 && pSum2 != 0)
                {
                    E = Math.Log(pSum1) + Math.Log(pSum2) - (pLogSum1 / pSum1) - (pLogSum2 / pSum2);
                    if (Tmax == 0 || E > Emax)
                    {
                        Emax = E;
                        Tmax = T;
                    }
                }
            }
            BinarizationOperation(Tmax);
            UpdatePixelMatrix();
        }

        public void DilationOperation(BinaryMask mask)
        {
            outBitmap = new WriteableBitmap(originalBitmap);
            outBitmap.Lock();
            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    RGBPixel pixel = pixelMatrix[x, y];
                    RGBPixel[,] neighbours = GetNeighbours(pixel, mask.Size);
                    pixel.ApplyDilationFilter(mask, neighbours);
                    DrawPixel(pixel);
                }
            }
            UpdateImage();
            UpdatePixelMatrix();
        }

        public void ErosionOperation(BinaryMask mask)
        {
            outBitmap = new WriteableBitmap(originalBitmap);
            outBitmap.Lock();
            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    RGBPixel pixel = pixelMatrix[x, y];
                    RGBPixel[,] neighbours = GetNeighbours(pixel, mask.Size);
                    pixel.ApplyErosionFilter(mask, neighbours);
                    DrawPixel(pixel);
                }
            }
            UpdateImage();
            UpdatePixelMatrix();
        }

        public void OpeningOperation(BinaryMask mask)
        {
            outBitmap = new WriteableBitmap(originalBitmap);
            outBitmap.Lock();
            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    RGBPixel pixel = pixelMatrix[x, y];
                    RGBPixel[,] neighbours = GetNeighbours(pixel, mask.Size);
                    pixel.ApplyErosionFilter(mask, neighbours);
                }
            }
            for (int x = 0; x < width; x++)
                for (int y = 0; y < height; y++)
                    pixelMatrix[x, y].Update();

            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    RGBPixel pixel = pixelMatrix[x, y];
                    RGBPixel[,] neighbours = GetNeighbours(pixel, mask.Size);
                    pixel.ApplyDilationFilter(mask, neighbours);
                    DrawPixel(pixel);
                }
            }
            UpdateImage();
            UpdatePixelMatrix();
        }

        public void ClosingOperation(BinaryMask mask)
        {
            outBitmap = new WriteableBitmap(originalBitmap);
            outBitmap.Lock();
            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    RGBPixel pixel = pixelMatrix[x, y];
                    RGBPixel[,] neighbours = GetNeighbours(pixel, mask.Size);
                    pixel.ApplyDilationFilter(mask, neighbours);
                }
            }
            for (int x = 0; x < width; x++)
                for (int y = 0; y < height; y++)
                    pixelMatrix[x, y].Update();

            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    RGBPixel pixel = pixelMatrix[x, y];
                    RGBPixel[,] neighbours = GetNeighbours(pixel, mask.Size);
                    pixel.ApplyErosionFilter(mask, neighbours);
                    DrawPixel(pixel);
                }
            }
            UpdateImage();
            UpdatePixelMatrix();
        }

        public void HitOrMissOperation()
        {
            outBitmap = new WriteableBitmap(originalBitmap);
            outBitmap.Lock();
            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    RGBPixel pixel = pixelMatrix[x, y];
                    RGBPixel[,] neighbours = GetNeighbours(pixel, 3);
                    pixel.ApplyHitOrMissFilter(Mask.BottomLeftCorner, neighbours);
                    DrawPixel(pixel);
                }
            }
            UpdateImage();
            UpdatePixelMatrix();
        }

        public RGBPixel[,] CopyPixelMatrix()
        {
            RGBPixel[,] newMatrix = new RGBPixel[width, height];
            for (int x = 0; x < width; x++)
                for (int y = 0; y < height; y++)
                    newMatrix[x, y] = pixelMatrix[x, y].GetCopy();
            return newMatrix;
        }

        public void ClearPixelMatrix()
        {
            for (int x = 0; x < width; x++)
                for (int y = 0; y < height; y++)
                    pixelMatrix[x, y].Clear();
        }

        public void ThinOperation()
        {
            outBitmap = new WriteableBitmap(originalBitmap);
            outBitmap.Lock();

            bool changed;
            Mask[] masks = new Mask[8];
            
            masks[0] = Mask.Thin1;
            masks[1] = Mask.Thin2;

            for (int i = 2; i < 8; i++)
                masks[i] = masks[i - 2].RotateRight();

            do
            {
                changed = false;
                for (int i = 0; i < 8; i++)
                {
                    for (int x = 0; x < width; x++)
                    {
                        for (int y = 0; y < height; y++)
                        {
                            RGBPixel pixel = pixelMatrix[x, y];
                            RGBPixel[,] neighbours = GetNeighbours(pixel, masks[0].Size);
                            if (pixel.Thin(masks[i], neighbours))
                                changed = true;
                            pixel.Update();
                        }
                    }
                }
            } while (changed);
            for (int x = 0; x < width; x++)
                for (int y = 0; y < height; y++)
                    DrawPixel(pixelMatrix[x, y]);
            UpdateImage();
        }

        public void ThickenOperation()
        {
            outBitmap = new WriteableBitmap(originalBitmap);
            outBitmap.Lock();

            bool changed;
            Mask[] masks = new Mask[8];

            masks[0] = Mask.Thicken1;
            masks[1] = Mask.Thicken2;

            for (int i = 2; i < 8; i++)
                masks[i] = masks[i - 2].RotateRight();

            do
            {
                changed = false;
                for (int i = 0; i < 8; i++)
                {
                    for (int x = 0; x < width; x++)
                    {
                        for (int y = 0; y < height; y++)
                        {
                            RGBPixel pixel = pixelMatrix[x, y];
                            RGBPixel[,] neighbours = GetNeighbours(pixel, masks[0].Size);
                            if (pixel.Thicken(masks[i], neighbours))
                                changed = true;
                            pixel.Update();
                        }
                    }
                }
            } while (changed);
            for (int x = 0; x < width; x++)
                for (int y = 0; y < height; y++)
                    DrawPixel(pixelMatrix[x, y]);
            UpdateImage();
        }
    }
}
