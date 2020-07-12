using System;
using System.Collections.Generic;
using System.IO;
using FreeImageAPI;

namespace tileimage
{
    class Program
    {
        static void Main(string[] args)
        {
            int tile_width = 1072;

            string file = args[0];

            tile_image(file, tile_width);
           
        }

        private static List<List<int>> LoadPixels(FIBITMAP dib)
        {

            int Height = (int)FreeImage.GetHeight(dib);
            int Width = (int)FreeImage.GetWidth(dib);

            List<List<int>> ImageData = new List<List<int>> { };
            for (int y = 0; y < Height; y++)
            {
                List<int> row = new List<int> { };
                // Note: dib is stored upside down
                Scanline<ushort> line = new Scanline<ushort>(dib, Height - 1 - y);
                foreach (ushort pixel in line)
                {
                    row.Add(pixel);
                }
                ImageData.Add(row);
            }

            return ImageData;

        }

        private static void tile_image(string filename, int tilesize)
        {
            int xindex = 0;
            int yindex = 0;
            List<Tile> tiles = new List<Tile>();
            FIBITMAP dib = FreeImage.LoadEx(filename);

            int Height = (int)FreeImage.GetHeight(dib);
            int Width = (int)FreeImage.GetWidth(dib);

            for (int y = 0; y + tilesize <= Height; y += tilesize)
            {
                for (int x = 0; x + tilesize <= Width; x += tilesize)
                {
                    Tile t = new Tile() { x = xindex, y = yindex };
                    FIBITMAP section = FreeImage.Copy(dib, x, y, x + tilesize, y + tilesize);
                    t.pixels = LoadPixels(section);

                    FreeImage.UnloadEx(ref section);
                    xindex++;

                    tiles.Add(t);
                }
                xindex = 0;
                yindex++;
            }
            FreeImage.UnloadEx(ref dib);

            // stitch edges
            StichEdges(tiles);

            string path = Path.GetDirectoryName(filename);
            foreach (var tile in tiles)
            {
                string tilename = string.Format(@"tile_x{0}_y{1}.bmp", tile.x, tile.y);
                tilename = Path.Combine(path, tilename);
                writesection(tilename, tile.pixels);
            }
        }

        private static void StichEdges(List<Tile> tiles)
        {
            foreach (var tile in tiles)
            {
                tile.StitchEdges(tiles);
            }
        }

        private static void writesection(string filename, List<List<int>> pixels)
        {
            string target = Path.GetFileNameWithoutExtension(filename) + ".raw";
            target = Path.Combine(Path.GetDirectoryName(filename), target);

            using (Stream fs = File.OpenWrite(target))
            {
                using (BinaryWriter bw = new BinaryWriter(fs))
                {
                    for (int y = 0; y < pixels.Count; y++)
                    {
                        for (int x = 0; x < pixels[y].Count; x++)
                        {
                            int gray = pixels[y][x];
                            UInt16 grayscale = Convert.ToUInt16(gray);
                            bw.Write(grayscale);
                        }
                    }
                    bw.Flush();
                }
            }

        } 
    }
}
