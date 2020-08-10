using FreeImageAPI;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace tileimage
{
    public static class Extensions
    {
        public static bool NearlyEqual(this float a, float b, float delta = 1)
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



        public static ushort GetHeight(this FIBITMAP dib, Vector2 p)
        {
            uint ImageHeight = FreeImage.GetHeight(dib);
            uint ImageWidth = FreeImage.GetWidth(dib);

            int scanline = (int)(ImageHeight - 1 - p.Y);
            if (scanline < 0 || scanline >= ImageHeight)
            {
                throw new Exception($"Invalid Y coordinate {p.Y}");
            }
            if (p.X < 0 || p.X >= ImageWidth)
            {
                throw new Exception($"Invalid X coordinate {p.X}");
            }
            Scanline<ushort> line = new Scanline<ushort>(dib, scanline);

            return line.Data[(int)p.X];
        }

        public static void SetHeight(this FIBITMAP dib, Vector2 p, ushort height)
        {
            uint ImageHeight = FreeImage.GetHeight(dib);
            uint ImageWidth = FreeImage.GetWidth(dib);

            int scanline = (int)(ImageHeight - 1 - p.Y);
            if (scanline < 0 || scanline >= ImageHeight)
            {
                //throw new Exception($"Invalid Y coordinate {p.Y}");
                return;
            }
            if (p.X < 0 || p.X >= ImageWidth)
            {
                //throw new Exception($"Invalid X coordinate {p.X}");
                return;
            }
            Scanline<ushort> line = new Scanline<ushort>(dib, scanline);
            ushort[] pixels = line.Data;
            pixels[(int)p.X] = height;
            line.Data = pixels;
        }

        public static bool ValidForImage(this Vector2 v, int width, int height)
        {
            return v.X >= 0 && v.Y >= 0 && v.X < width && v.Y < height;
        }

        public static Vector2[] Neighbors(this Vector2 v, int dist)
        {
            List<Vector2> n = new List<Vector2>();

            for (int i = dist * -1; i < dist + 1; i++)
            {
                var tmp = new Vector2(v.X + i, v.Y);
                if (!tmp.Equals(v))
                {
                    n.Add(tmp);
                }

                tmp = new Vector2(v.X, v.Y + i);
                if (!tmp.Equals(v))
                {
                    n.Add(tmp);
                }
            }

            return n.ToArray();
        }
        public static Vector2[] AllNeighbors(this Vector2 v, int dist)
        {
            List<Vector2> n = new List<Vector2>();

            for (int x = dist * -1; x < dist + 1; x++)
            {
                for (int y = dist * -1; y < dist + 1; y++)
                {
                    var tmp = new Vector2(v.X + x, v.Y + y);
                    if (!tmp.Equals(v))
                    {
                        n.Add(tmp);
                    }
                }
            }
            return n.ToArray();
        }
    }
}
