using FreeImageAPI;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace tileimage
{
    class Crater
    {
        public PointXY origin;
        public double radius;
        public List<PointXY> craterPoints = new List<PointXY>();
        public List<PointXY> craterRidgePoints = new List<PointXY>();


        public Crater(double x, double y, double radius)
        {

            origin = new PointXY(x, y);
            this.radius = radius;
        }
        public void GeneratePixels2(int width, int height)
        {
            RandomEx rng = new RandomEx();
            int numWaves = 32;
            double[] amps = new double[numWaves];
            double[] phases = new double[numWaves];
            for (int w = 0; w < numWaves; w++)
            {
                float max =  w > 0 ? 1.0f / (2 * w) : 0;
                amps[w] = rng.NextDouble(0, max);
                phases[w] = rng.NextDouble(0, 2 * Math.PI);
            }

            for (int a = 0; a < 360; a++)
            {
                double periodic_fxn_sum = 0;
                double radians = a * Math.PI / 180;
                for (int i = 0; i < numWaves; i++)
                {
                    double periodic_fxn = amps[i] * Math.Cos((i + 1) * radians + phases[i]);
                    periodic_fxn_sum += periodic_fxn;
                }
                double adjustedRadius = 1 + periodic_fxn_sum;
                adjustedRadius *= radius;
                int nx = (int)(origin.x + Math.Cos(radians) * adjustedRadius);
                int ny = (int)(origin.y + Math.Sin(radians) * adjustedRadius);
                int cx = (int)(origin.x + Math.Cos(radians) * radius);
                int cy = (int)(origin.y + Math.Sin(radians) * radius);

                // make sure new point doesn't overlap origin
                var p = new PointXY((int)cx, (int)cy);
                var n = new PointXY((int)nx, (int)ny);
                if (!n.Equals(origin))
                {
                    p = n;
                }

                // add
                if (!craterRidgePoints.Contains(p) && p.ValidForImage(width, height))
                {
                    craterRidgePoints.Add(p);
                }
            }
            Floodfill(origin, width, height);
        }
        public void GeneratePixels(int width, int height)
        {
            LibNoise.Primitive.BevinsValue pp = new LibNoise.Primitive.BevinsValue();
            float warpScale = 0.5f;
            float inc = 0;
            for (int i = 0; i < 360; i++)
            {

                inc += 0.0001f;
                // distort radius with noise
                float k = pp.ValueCoherentNoise2D((float)origin.x + inc, (float)origin.y, (long)(origin.x * origin.y), LibNoise.NoiseQuality.Best);
                double cx = origin.x + radius * Math.Cos(i * Math.PI / 180);
                double cy = origin.y + radius * Math.Sin(i * Math.PI / 180);
                double nx = cx + cx * warpScale * k;
                double ny = cy + cy * warpScale * k;

                // make sure new point doesn't overlap origin
                var p = new PointXY((int)cx, (int)cy);
                var n = new PointXY((int)nx, (int)ny);
                if (!n.Equals(origin))
                {
                    p = n;
                }

                // add
                if (!craterRidgePoints.Contains(p) && p.ValidForImage(width, height))
                {
                    craterRidgePoints.Add(p);
                }

            }
            Floodfill(origin, width, height);
        }

        public void Floodfill(PointXY p, int width, int height)
        {
            Stack<PointXY> pixels = new Stack<PointXY>();
            pixels.Push(p);

            while (pixels.Count > 0)
            {
                PointXY a = pixels.Pop();
                if (a.ValidForImage(width, height))//make sure we stay within bounds
                {

                    if (craterRidgePoints.Contains(a))
                    {
                        // do nothing
                    }
                    else if (DistanceFromCenter(a) >= radius)
                    {
                        craterRidgePoints.Add(a);
                    }
                    else
                    {
                        if (!craterPoints.Contains(a))
                        {
                            craterPoints.Add(a);
                            for (int i = -1; i < 2; i++)
                            {
                                var tmp = new PointXY(a.x + i, a.y);
                                if (!craterPoints.Contains(tmp) && !pixels.Contains(tmp) && tmp.ValidForImage(width, height))
                                {
                                    pixels.Push(tmp);
                                }
                            }
                            for (int i = -1; i < 2; i++)
                            {
                                var tmp = new PointXY(a.x, a.y + i);
                                if (!craterPoints.Contains(tmp) && !pixels.Contains(tmp) && tmp.ValidForImage(width, height))
                                {
                                    pixels.Push(tmp);
                                }
                            }

                        }

                    }
                }
            }
        }

        public bool IsRidge(PointXY p)
        {
            double d = DistanceFromCenter(p);
            return d < radius && d.NearlyEqual(radius, 3);
        }

        public bool IsInside(PointXY p)
        {
            return DistanceFromCenter(p) < radius;
        }

        public double DistanceFromCenter(PointXY p)  // compute the distance of point p to the current point
        {
            return dist(origin, p);
        }

        public double dist(PointXY a, PointXY b)  // compute the distance of point p to the current point
        {
            double distance;

            distance = Math.Sqrt((a.x - b.x) * (a.x - b.x) + (a.y - b.y) * (a.y - b.y));
            return distance;
        }
    }

    public class PointXY
    {
        public double x { get; set; }
        public double y { get; set; }

        public PointXY(double x, double y)
        {
            this.x = x;
            this.y = y;
        }

        public bool ValidForImage(int width, int height)
        {
            return x >= 0 && y >= 0 && x < width && y < height;
        }
        public override bool Equals(object obj)
        {
            PointXY o = (PointXY)obj;
            return o.x == x && o.y == y;
        }
    }
}