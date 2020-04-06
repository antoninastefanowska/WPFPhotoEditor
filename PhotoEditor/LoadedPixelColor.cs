using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PhotoEditor
{
    public abstract class LoadedPixelColor
    {
        public ColorComponent Red { get; set; }
        public ColorComponent Green { get; set; }
        public ColorComponent Blue { get; set; }

        protected ColorComponent currentColorComponent;

        protected enum ColorComponentType
        {
            RED,
            GREEN,
            BLUE
        }

        public bool IsReady { get; protected set; }

        public virtual void FinalizeSingleComponent()
        {
            currentColorComponent.CalculateValue();

            if (currentColorComponent == Red)
                currentColorComponent = Green;
            else if (currentColorComponent == Green)
                currentColorComponent = Blue;
            else if (currentColorComponent == Blue)
            {
                currentColorComponent = null;
                IsReady = true;
            }
        }

        public int CalculateColor()
        {
            int output = Red.FinalValue << 16;
            output |= Green.FinalValue << 8;
            output |= Blue.FinalValue;
            return output;
        }
    }
}
