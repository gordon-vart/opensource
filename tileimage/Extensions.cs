using FreeImageAPI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace tileimage
{
    public static class Extensions
    {
        public static bool NearlyEqual(this double a, double b, double delta = 1)
        {
            if (Math.Abs(a - b) < delta)
            {
                // Values are within specified tolerance of each other....
                return true;
            }
            return false;
        }

        public static T Clamp<T>(this T val, T min, T max) where T : IComparable<T>
        {
            if (val.CompareTo(min) < 0) return min;
            else if (val.CompareTo(max) > 0) return max;
            else return val;
        }

        public static ushort GetHeight(this FIBITMAP dib, PointXY p)
        {
            uint ImageHeight = FreeImage.GetHeight(dib);
            uint ImageWidth = FreeImage.GetWidth(dib);

            int scanline = (int)(ImageHeight - 1 - p.y);
            if (scanline < 0 || scanline >= ImageHeight)
            {
                throw new Exception($"Invalid Y coordinate {p.y}");
            }
            if (p.x < 0 || p.x >= ImageWidth)
            {
                throw new Exception($"Invalid X coordinate {p.x}");
            }
            Scanline<ushort> line = new Scanline<ushort>(dib, scanline);

            return line.Data[(int)p.x];
        }

        public static void SetHeight(this FIBITMAP dib, PointXY p, ushort height)
        {
            uint ImageHeight = FreeImage.GetHeight(dib);
            uint ImageWidth = FreeImage.GetWidth(dib);

            int scanline = (int)(ImageHeight - 1 - p.y);
            if (scanline < 0 || scanline >= ImageHeight)
            {
                //throw new Exception($"Invalid Y coordinate {p.y}");
                return;
            }
            if (p.x < 0 || p.x >= ImageWidth)
            {
                //throw new Exception($"Invalid X coordinate {p.x}");
                return;
            }
            Scanline<ushort> line = new Scanline<ushort>(dib, scanline);
            ushort[] pixels = line.Data;
            pixels[(int)p.x] = height;
            line.Data = pixels;
        }
    }
}
