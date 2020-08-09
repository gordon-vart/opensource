using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Security.Cryptography;
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

        public static IEnumerable<Vector2> GetPointsOnLine(Vector2 a, Vector2 b)
        {
            int dx = (int)Math.Abs(b.X - a.X);
            int sx = a.X < b.X ? 1 : -1;
            int dy = (int)Math.Abs(b.Y - a.Y);
            int sy = a.Y < b.Y ? 1 : -1;
            int err = (dx > dy ? dx : -dy) / 2;
            int e2;
            int cx = (int)a.X;
            int cy = (int)a.Y;
            for (; ; )
            {
                yield return new Vector2(cx, cy);
                if (cx == b.X && cy == b.Y) break;
                e2 = err;
                if (e2 > -dx)
                {
                    err -= dy;
                    cx += sx;
                }
                if (e2 < dy)
                {
                    err += dx; cy += sy;
                }
            }
        }
    }
}
