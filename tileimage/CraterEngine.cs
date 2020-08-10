
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
                int radius = (int)MathEx.Lerp(minRadius, maxRadius, MathEx.EaseIn(alpha, 6));
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
            if (!string.IsNullOrEmpty(debugFilename))
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
                
                // debug
                //dib.SetHeight(item, ushort.MaxValue);
            }

            // smooth
            if (true)
            {
                Smooth(crater);
            }

        }

        private void Smooth(Crater crater)
        {
            float xmin = crater.craterRidgePoints.Select(c => c.X).Min();
            float xmax = crater.craterRidgePoints.Select(c => c.X).Max();
            float ymin = crater.craterRidgePoints.Select(c => c.Y).Min();
            float ymax = crater.craterRidgePoints.Select(c => c.Y).Max();

            // inflate by a few pixels
            float offset = 5;
            xmin = Math.Max(0, xmin - offset);
            xmax = Math.Min(ImageWidth, xmax + offset);
            ymin = Math.Max(0, ymin - offset);
            ymax = Math.Min(ImageWidth, ymax + offset);

            for (int x = (int)xmin; x < (int)xmax; x++)
            {
                for (int y = (int)ymin; y < (int)ymax; y++)
                {
                    Vector2 v = new Vector2(x, y);
                    
                    List<int> k = new List<int>();
                    k.Add(GetPointHeight(v));
                    foreach (var n in v.AllNeighbors(1))
                    {
                        k.Add(GetPointHeight(n));
                    }

                    int valid = k.Count(o => o > 0);
                    int total = k.Where(o => o > 0).Sum();
                    
                    if (valid > 0)
                    {
                        dib.SetHeight(v, (ushort)(total / valid));
                    }
                }
            }


            //string fn = @"c:\temp\ceres\section-blur.png";
            //FreeImage.Save(FREE_IMAGE_FORMAT.FIF_PNG, dib, fn, FREE_IMAGE_SAVE_FLAGS.DEFAULT);

            int f = 0;
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
