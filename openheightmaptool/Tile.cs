using System;
using System.Collections.Generic;

namespace tileimage
{

    public class Tile
    {
        public int x { get; set; } 
        public int y { get; set; }
        public List<List<int>> pixels { get; set; }

        public void StitchEdges(List<Tile> tiles)
        {
            var top = tiles.Find(o => o.x == x && o.y == y - 1);
            var left = tiles.Find(o => o.x == x - 1 && o.y == y);

            if (top != null)
            {
                pixels[0] = top.GetBottomPixels();
            }
            if (left != null)
            {
                var p = left.GetRightPixels();
                for (int i = 0; i < pixels.Count; i++)
                {
                    pixels[i][0] = p[i];
                }
            }
        }

        public List<int> GetTopPixels()
        {
            List<int> results = new List<int>();
            results.AddRange(pixels[0]);
            return results;
        }
        public List<int> GetLeftPixels()
        {
            List<int> results = new List<int>();
            foreach (var set in pixels)
            {
                results.Add(set[0]);
            }
            return results;
        }
        public List<int> GetRightPixels()
        {
            List<int> results = new List<int>();
            foreach (var set in pixels)
            {
                results.Add(set[set.Count - 1]);
            }
            return results;
        }
        public List<int> GetBottomPixels()
        {
            List<int> results = new List<int>();
            results.AddRange(pixels[pixels.Count - 1]);
            return results;
        }
    }
}
