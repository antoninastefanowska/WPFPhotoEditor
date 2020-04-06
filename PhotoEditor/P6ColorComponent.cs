using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PhotoEditor
{
    public class P6ColorComponent : ColorComponent
    {
        public int IntegerValue { get; private set; }

        public P6ColorComponent(int maxValue) : base(maxValue)
        {
            IntegerValue = 0;
        }

        public void AddByte(byte input)
        {
            IntegerValue <<= 8;
            IntegerValue |= input;
            
        }

        public override void CalculateValue()
        {
            FinalValue = scale(IntegerValue);
            IsReady = true;
        }
    }
}
