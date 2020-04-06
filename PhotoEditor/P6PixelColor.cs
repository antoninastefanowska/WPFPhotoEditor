using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PhotoEditor
{
    public class P6PixelColor : LoadedPixelColor
    {
        private bool isSingleByte, last;

        public P6PixelColor(int maxColor)
        {
            Red = new P6ColorComponent(maxColor);
            Green = new P6ColorComponent(maxColor);
            Blue = new P6ColorComponent(maxColor);
            IsReady = false;
            currentColorComponent = Red;

            isSingleByte = maxColor < 256;
            last = false;
        }

        public void AddByte(byte input)
        {
            ((P6ColorComponent)currentColorComponent).AddByte(input);
        }

        public override void FinalizeSingleComponent()
        {
            if (!isSingleByte)
            {
                if (last)
                {
                    last = false;
                    base.FinalizeSingleComponent();
                }
                else
                    last = true;
            }
            else
                base.FinalizeSingleComponent();
        }
    }
}
