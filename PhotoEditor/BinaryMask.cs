using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PhotoEditor
{
    public class BinaryMask
    {
        public int Size { get; set; }

        public bool[,] Matrix { get; set; }

        public static BinaryMask Standard
        {
            get
            {
                return new BinaryMask(3, new bool[3, 3]
                {
                    {true, true, true},
                    {true, true, true},
                    {true, true, true}
                });
            }
        }

        public BinaryMask(int size)
        {
            if (size < 3 || size % 2 == 0)
                throw new ArgumentException();

            Size = size;
            Matrix = new bool[size, size];
        }

        public BinaryMask(int size, bool[,] matrix)
        {
            if (size < 3 || size % 2 == 0)
                throw new ArgumentException();

            Size = size;
            Matrix = matrix;
        }
    }
}
