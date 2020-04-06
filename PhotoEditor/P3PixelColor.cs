using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PhotoEditor
{
    public class P3PixelColor : LoadedPixelColor
    {
        public P3PixelColor(int maxColor)
        {
            Red = new P3ColorComponent(maxColor);
            Green = new P3ColorComponent(maxColor);
            Blue = new P3ColorComponent(maxColor);
            IsReady = false;
            currentColorComponent = Red;
        }

        public void AddCharacter(char input)
        {
            ((P3ColorComponent)currentColorComponent).AddCharacter(input);
        }
    }
}
