using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PhotoEditor
{
    public class Mask
    {
        public int Size { get; private set; }

        public int[,] Matrix { get; set; }

        public int Sum
        {
            get
            {
                int sum = 0;
                for (int i = 0; i < Size; i++)
                    for (int j = 0; j < Size; j++)
                        sum += Matrix[i, j];
                return sum;
            }
        }

        public static Mask LowPass
        {
            get
            {
                return new Mask(3, new int[3, 3]
                {
                    {1, 1, 1},
                    {1, 1, 1},
                    {1, 1, 1}
                });
            }
        }

        public static Mask SobelVertical
        {
            get
            {
                return new Mask(3, new int[3, 3]
                {
                    {-1, 0, 1},
                    {-2, 0, 2},
                    {-1, 0, 1}
                });
            }
        }

        public static Mask SobelHorizontal
        {
            get
            {
                return new Mask(3, new int[3, 3]
                {
                    {1, 2, 1},
                    {0, 0, 0},
                    {-1, -2, -1}
                });
            }
        }

        public static Mask HighPass
        {
            get
            {
                return new Mask(3, new int[3, 3]
                {
                    {-1, -1, -1},
                    {-1, 9, -1},
                    {-1, -1, -1}
                });
            }
        }

        public static Mask Gauss
        {
            get
            {
                return new Mask(3, new int[3, 3]
                {
                    {1, 2, 1},
                    {2, 4, 2},
                    {1, 2, 1}
                });
            }
        }

        public static Mask BottomLeftCorner
        {
            get
            {
                return new Mask(3, new int[3, 3]
                {
                    {0, -1, 0},
                    {1, -1, -1},
                    {1, 1, 0}
                });
            }
        }

        public static Mask BottomRightCorner
        {
            get
            {
                return new Mask(3, new int[3, 3]
                {
                    {0, -1, 0},
                    {-1, -1, 1},
                    {0, 1, 1}
                });
            }
        }

        public static Mask TopRightCorner
        {
            get
            {
                return new Mask(3, new int[3, 3]
                {
                    {0, 1, 1},
                    {-1, -1, 1},
                    {0, -1, 0}
                });
            }
        }

        public static Mask TopLeftCorner
        {
            get
            {
                return new Mask(3, new int[3, 3]
                {
                    {1, 1, 0},
                    {1, -1, -1},
                    {0, -1, 0}
                });
            }
        }

        public static Mask Thin1
        {
            get
            {
                return new Mask(3, new int[3, 3]
                {
                    {-1, -1, -1},
                    {0, 1, 0},
                    {1, 1, 1}
                });
            }
        }

        public static Mask Thin2
        {
            get
            {
                return new Mask(3, new int[3, 3]
                {
                    {0, -1, -1},
                    {1, 1, -1},
                    {0, 1, 0}
                });
            }
        }

        public static Mask Thicken1
        {
            get
            {
                return new Mask(3, new int[3, 3]
                {
                    {1, 1, 0},
                    {1, -1, 0},
                    {1, 0, -1}
                });
            }
        }

        public static Mask Thicken2
        {
            get
            {
                return new Mask(3, new int[3, 3]
                {
                    {0, 1, 1},
                    {0, -1, 1},
                    {-1, 0, 1}
                });
            }
        }

        public Mask(int size)
        {
            if (size < 3 || size % 2 == 0)
                throw new ArgumentException();

            Size = size;
            Matrix = new int[size, size];
        }

        public Mask(int size, int[,] matrix)
        {
            if (size < 3 || size % 2 == 0)
                throw new ArgumentException();

            Size = size;
            Matrix = matrix;
        }

        public Mask RotateRight()
        {
            int[,] newMatrix = new int[Size, Size];
            for (int i = 0; i < Size; i++)
                for (int j = 0; j < Size; j++)
                    newMatrix[i, j] = Matrix[Size - j - 1, i];
            return new Mask(Size, newMatrix);
        }

        public Tuple<BinaryMask, BinaryMask> SeparateMask()
        {
            BinaryMask whiteMask = new BinaryMask(Size);
            BinaryMask blackMask = new BinaryMask(Size);
            for (int i = 0; i < Size; i++)
            {
                for (int j = 0; j < Size; j++)
                {
                    if (Matrix[i, j] == 1)
                    {
                        whiteMask.Matrix[i, j] = true;
                        blackMask.Matrix[i, j] = false;
                    }
                    else if (Matrix[i, j] == -1)
                    {
                        whiteMask.Matrix[i, j] = false;
                        blackMask.Matrix[i, j] = true;
                    }
                    else
                    {
                        whiteMask.Matrix[i, j] = false;
                        blackMask.Matrix[i, j] = false;
                    }
                }
            }
            return new Tuple<BinaryMask, BinaryMask>(whiteMask, blackMask);
        }
    }
}
