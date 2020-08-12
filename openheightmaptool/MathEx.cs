using System;
using System.Collections.Generic;
using System.Drawing;
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

        public static List<Rectangle> GetTiles(int width, int height, int tilesize)
        {
            List<Rectangle> tiles = new List<Rectangle>();
            int xindex = 0;
            int yindex = 0;
            // calc max square size
            int max_x = width / tilesize;
            int max_y = height / tilesize;
            int tilecount = Math.Min(max_x, max_y);
            int cx, cy;
            cx = cy = 0;
            for (int y = 0; y < tilecount; y++)
            {
                for (int x = 0; x < tilecount; x++)
                {
                    tiles.Add(new Rectangle(cx, cy, tilesize, tilesize));
                    xindex++;
                    cx += tilesize - 1;
                }
                xindex = 0;
                cx = 0;
                yindex++;
                cy += tilesize - 1;
            }
            return tiles;
        }
    }
}
