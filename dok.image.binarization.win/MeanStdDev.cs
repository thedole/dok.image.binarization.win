using System;
using OpenCvSharp.CPlusPlus;

namespace dok.image.binarization.win
{
    public class MeanStdDev
    {
        public Scalar Mean { get; set; }
        public Scalar StdDev { get; set; }

        public Tuple<double, double> this[int index]
        {
            get
            {
                if (index == 0)
                {
                    return Tuple.Create(Mean.Val0, StdDev.Val0);
                }
                if (index == 1)
                {
                    return Tuple.Create(Mean.Val1, StdDev.Val1);
                }
                if (index == 2)
                {
                    return Tuple.Create(Mean.Val2, StdDev.Val2);
                }
                if (index == 3)
                {
                    return Tuple.Create(Mean.Val3, StdDev.Val3);
                }

                return null;
            }
        }
    }
}
