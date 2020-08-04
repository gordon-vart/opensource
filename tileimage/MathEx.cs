using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace tileimage
{
    public static class MathEx
    {
        public static double Lerp(double a, double b, double alpha)
        {
            alpha = alpha.Clamp(0, 1);
            return a * (1 - alpha) + b * alpha;
        }

        public static double EaseIn(double v, double power = 3)
        {
            double k = Math.Pow(v, power);
            return k;
        }
    }
}
