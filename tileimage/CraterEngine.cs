using FreeImageAPI;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace tileimage
{
    class CraterEngine
    {
        public Bitmap debugbmp { get; set; }
        public FIBITMAP dib { get; set; }
        public int numCraters { get; set; }
        public int minRadius { get; set; }
        public int maxRadius { get; set; }
        public int seed { get; set; }
        public List<Crater> craters = new List<Crater>();
        int ImageHeight;
        int ImageWidth;

        public CraterEngine()
        {

        }

        public void BuildCraters(string debugFilename)
        {
            Random rng = new Random(seed);

            ImageHeight = (int)FreeImage.GetHeight(dib);
            ImageWidth = (int)FreeImage.GetWidth(dib);

            // debug
            debugbmp = new Bitmap(ImageWidth, ImageHeight);

            // generate craters
            for (int c = 0; c < numCraters; c++)
            {
                int x = rng.Next(0, ImageWidth);
                int y = rng.Next(0, ImageHeight);
                double alpha = rng.NextDouble();
                int radius = (int)MathEx.Lerp(minRadius, maxRadius, MathEx.EaseIn(alpha, 10));
                Crater crater = new Crater(x, y, radius) { bmp = debugbmp };
                craters.Add(crater);

            }

            // really generate craters
            if (false)
            {
                // for debug
                foreach (var item in craters)
                {
                    item.GeneratePixels2(ImageWidth, ImageHeight);
                }
            }
            else
            {
                // generate on all cores
                Parallel.ForEach(craters, (c) =>
                {
                    c.GeneratePixels2(ImageWidth, ImageHeight);
                });
            }


            // draw craters
            int i = 0;
            foreach (var item in craters)
            {
                drawCrater(item);
                Console.WriteLine($"{i + 1}. {item.origin.X},{item.origin.Y} radius: {item.radius}");
                i++;
            }

            // debug
            if(!string.IsNullOrEmpty(debugFilename))
            {
                debugbmp.Save(debugFilename);
            }
        }

        private void drawCrater(Crater crater)
        {
            double radius = crater.radius;

            // scale x/y to z so we can scale crater depth
            double scale = ushort.MaxValue / Math.Min(ImageWidth, ImageHeight);

            // generate crater border

            // get highest/lowest point in the crater
            double scalealpha = radius / maxRadius;
            Vector2 low = crater.craterPoints.OrderBy(o => dib.GetHeight(o)).First();
            ushort lowHeight = dib.GetHeight(low);
            Vector2 high = crater.craterPoints.OrderByDescending(o => dib.GetHeight(o)).First();
            ushort highHeight = dib.GetHeight(high);
            int lowPoint = Math.Max(0, lowHeight - (int)(radius * scale));

            // set crater pixel heights inside
            int index = 0;
            foreach (var item in crater.craterPoints)
            {
                // extrapolate ray from origin to this point until we encounter the ridge
                Vector2 ridge = crater.FindRidge(item, ImageWidth, ImageHeight, index % 100 == 0);
                double ridgeDist = crater.DistanceFromCenter(ridge);

                int ridgeHeight = dib.GetHeight(ridge);
                ridgeHeight += (int)(Math.Max(1, (radius / 3)) * scale);

                double itemDist = crater.DistanceFromCenter(item);

                double alpha = itemDist / ridgeDist;
                double newHeight = MathEx.Lerp(lowPoint, ridgeHeight, MathEx.EaseIn(alpha, 5));
                dib.SetHeight(item, (ushort)(newHeight));
                index++;
            }

            // draw ridge
            foreach (var item in crater.craterRidgePoints)
            {
                int h = dib.GetHeight(item);
                dib.SetHeight(item, Math.Min(ushort.MaxValue, (ushort)(h + (int)(Math.Max(1, (radius / 3)) * scale))));
            }

            // smooth
            if (true)
            {
                Smooth(crater);
            }

        }

        private void Smooth(Crater crater)
        {
            foreach (var i in crater.craterPoints.Union(crater.craterRidgePoints))
            {
                List<int> k = new List<int>();
                foreach (var n in i.Neighbors(1))
                {
                    k.Add(GetPointHeight(n));
                }

                int valid = k.Count(o => o > 0);
                int total = k.Where(o => o > 0).Sum();
                Vector2 p = new Vector2(i.X, i.Y);
                if (valid > 0)
                {
                    dib.SetHeight(p, (ushort)(total / valid));
                }
            }
        }

        private int GetPointHeight(Vector2 v)
        {
            if (v.ValidForImage(ImageWidth, ImageHeight))
            {
                return dib.GetHeight(v);
            }
            else
            {
                return -1;
            }
        }
    }
}
