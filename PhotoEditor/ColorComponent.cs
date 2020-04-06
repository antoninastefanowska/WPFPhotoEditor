using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PhotoEditor
{
    public abstract class ColorComponent
    {
        protected int maxValue;
        public bool IsReady { get; protected set; }
        public byte FinalValue { get; protected set; }

        public ColorComponent(int maxValue)
        {
            this.maxValue = maxValue;
            IsReady = false;
        }

        public abstract void CalculateValue();

        protected byte scale(int input)
        {
            return (byte)(255 * input / maxValue);
        }
    }
}
