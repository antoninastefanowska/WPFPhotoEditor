using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PhotoEditor
{
    public class P3ColorComponent : ColorComponent
    {
        public string StringValue { get; private set; }
        
        public P3ColorComponent(int maxValue) : base(maxValue)
        {
            StringValue = "";
        }

        public void AddCharacter(char input)
        {
            if (StringValue == null)
                StringValue = "";
            StringValue += input;
        }

        public override void CalculateValue()
        {
            FinalValue = convert(StringValue);
            IsReady = true;
        }
        
        private byte convert(string input)
        {
            int value = 0;
            try
            {
                value = Convert.ToInt32(input);
            }
            catch (FormatException) { }
            return scale(value);
        }
    }
}
