using FreeImageAPI;
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
    class Crater
    {
        public Vector2 origin;
        public float radius;
        public List<Vector2> craterPoints = new List<Vector2>();
        public List<Vector2> craterRidgePoints = new List<Vector2>();
        public Bitmap bmp;

        public Crater(float x, float y, float radius)
        {
            origin = new Vector2(x, y);
            this.radius = radius;
        }

        public void GeneratePixels2(int width, int height)
        {
            RandomEx rng = new RandomEx();
            // debug
            if (bmp != null)
            {
                lock (bmp)
                {
                    bmp.SetPixel((int)origin.X, (int)origin.Y, Color.Black);
                }

            }

            //
            // derived from https://stackoverflow.com/questions/54828017/c-create-random-shaped-blob-objects/54829058?newreg=4643a6f0bd0f44c9b2b24f3e6fb4b4cb
            // thx 'The Vee'
            //
            int numWaves = 32;
            float[] amps = new float[numWaves];
            float[] phases = new float[numWaves];
            for (int w = 0; w < numWaves; w++)
            {
                // float max =  w > 0 ? 1.0f / (2 * w) : 0;
                float max = (float)(1.0f / Math.Pow(w + 1, 1.5));
                amps[w] = rng.NextFloat(0, max);
                phases[w] = rng.NextFloat(0, (float)(2 * Math.PI));
            }

            for (int a = 0; a < 360; a++)
            {
                float periodic_fxn_sum = 0;
                float radians = (float)(a * Math.PI / 180);
                for (int i = 0; i < numWaves; i++)
                {
                    float periodic_fxn = (float)(amps[i] * Math.Cos((i + 1) * radians + phases[i]));
                    periodic_fxn_sum += periodic_fxn;
                }
                float adjustedRadius = 1 + periodic_fxn_sum;
                adjustedRadius *= radius;

                // adjust radius to keep area around origin free
                if (adjustedRadius < 3)
                {
                    adjustedRadius = 3;
                }
                int nx = (int)(origin.X + Math.Cos(radians) * adjustedRadius);
                int ny = (int)(origin.Y + Math.Sin(radians) * adjustedRadius);
                var p = new Vector2((int)nx, (int)ny);

                // add
                if (!craterRidgePoints.Contains(p) && p.ValidForImage(width, height))
                {
                    craterRidgePoints.Add(p);
                }
            }

            // fill gaps between ridge points
            List<Vector2> missing = new List<Vector2>();
            for (int i = 0; i < craterRidgePoints.Count; i++)
            {

                int start = i;
                int end;
                if (i == 0)
                {
                    end = craterRidgePoints.Count - 1;
                }
                else
                {
                    end = i - 1;
                }
                var points = MathEx.GetPointsOnLine(craterRidgePoints[start], craterRidgePoints[end]).ToList();
                foreach (var item in points)
                {
                    missing.Add(item);
                }

            }
            // dedupe and consolidate
            craterRidgePoints = craterRidgePoints.Union(missing).Where(p => p.ValidForImage(width, height)).Distinct().ToList();

            // thicken the ridge border
            missing.Clear();
            foreach (var item in craterRidgePoints)
            {
                missing.AddRange(item.Neighbors(1));
            }
            // dedupe and consolidate
            craterRidgePoints = craterRidgePoints.Union(missing).Where(p => p.ValidForImage(width, height)).Distinct().ToList();

            // remove the points near the origin
            craterRidgePoints = craterRidgePoints.Where(p => DistanceFromCenter(p) > 1).ToList();

            // debug
            if (bmp != null)
            {
                foreach (var item in craterRidgePoints)
                {
                    int x = (int)(item.X);
                    int y = (int)(item.Y);

                    // thread safety
                    lock (bmp)
                    {
                        bmp.SetPixel(x, y, Color.Red);
                    }

                }

            }

            // find pixels inside crater
            Floodfill(origin, width, height);
        }




        public void Floodfill(Vector2 p, int width, int height)
        {
            Stack<Vector2> pixels = new Stack<Vector2>();
            pixels.Push(p);

            while (pixels.Count > 0)
            {
                Vector2 a = pixels.Pop();
                if (a.ValidForImage(width, height))//make sure we stay within bounds
                {

                    if (craterRidgePoints.Contains(a))
                    {
                        // do nothing
                    }
                    else
                    {
                        if (!craterPoints.Contains(a))
                        {
                            craterPoints.Add(a);

                            // check neighbors
                            foreach (var n in a.Neighbors(1))
                            {
                                if (!craterRidgePoints.Contains(n) && !craterPoints.Contains(n) && !pixels.Contains(n) && n.ValidForImage(width, height))
                                {
                                    pixels.Push(n);
                                }
                            }
                        }
                    }
                }
            }
        }

        private bool IsNearRidge(Vector2 p, int checkdist)
        {
            foreach (var item in p.Neighbors(checkdist))
            {
                bool isridge = craterRidgePoints.Contains(p);
                if (isridge)
                {
                    return true;
                }
            }

            return false;
        }

        public bool IsRidge(Vector2 p)
        {
            float d = DistanceFromCenter(p);
            return d < radius && d.NearlyEqual(radius, 3);
        }

        public bool IsInside(Vector2 p)
        {
            return DistanceFromCenter(p) < radius;
        }

        public float DistanceFromCenter(Vector2 p)  // compute the distance of point p to the current point
        {
            return dist(origin, p);
        }

        public float dist(Vector2 a, Vector2 b)  // compute the distance of point p to the current point
        {
            float distance;

            distance = (float)(Math.Sqrt((a.X - b.X) * (a.X - b.X) + (a.Y - b.Y) * (a.Y - b.Y)));
            return distance;
        }

        public Vector2 FindRidge(Vector2 item, int imageWidth, int imageHeight, bool debug)
        {

            if (item.Equals(origin))
            {
                return craterRidgePoints[0];
            }

            // extrapolate ray from origin to this point until we encounter the ridge
            Vector2 lastValid = item;
            Vector2 direction = Vector2.Subtract(item, origin);
            direction = Vector2.Normalize(direction);

            float distance = 1;
            while (true)
            {
                Vector2 current = Vector2.Add(origin, Vector2.Multiply(distance, direction));
                current.X = (int)current.X;
                current.Y = (int)current.Y;

                // if it matches the origin, just return the first ridge point
                if (distance > radius * 3)
                {
                    return craterRidgePoints[0];
                }

                // if we found the ridge
                if (craterRidgePoints.Contains(current))
                {
                    return current;
                }

                // if we found the edge of the image return last valid
                if (!current.ValidForImage(imageWidth, imageHeight))
                {
                    return lastValid;
                }
                else
                {
                    lastValid = current;
                }

                distance++;
            }
        }
    }

}