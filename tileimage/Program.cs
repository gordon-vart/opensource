using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Threading.Tasks;
using System.Windows.Forms;
using CommandLine;
using FreeImageAPI;

namespace tileimage
{
    class Program
    {
        public class Options
        {
            [Option('f', "filename", Required = true, HelpText = "Filename of image to tile.")]
            public string Filename { get; set; }

            [Option('t', "tile", Required = false, HelpText = "The image should be tiled")]
            public bool Tile { get; set; }

            [Option('c', "components", Required = false, HelpText = "The number of components. i.e. 2x2", Default = 2)]
            public int Components { get; set; }

            [Option('s', "sections", Required = false, HelpText = "The number of sections per component. i.e. 1x1", Default = 1)]
            public int Sections { get; set; }

            [Option('q', "quads", Required = false, HelpText = "The number of quads per section. i.e. 7, 15, 31, 63, 127, 255", Default = 63)]
            public int Quads { get; set; }

            [Option('i', "info", Required = false, HelpText = "Display file info.")]
            public bool ShowInfo { get; set; }

            [Option('z', "craters", Required = false, HelpText = "Numbers of craters.", Default = 0)]
            public int Craters { get; set; }

            [Option('d', "seed", Required = false, HelpText = "Random seed.", Default = 1)]
            public int Seed { get; set; }

            [Option('m', "minradius", Required = false, HelpText = "Min crater radius.", Default = 5)]
            public int CraterMin { get; set; }

            [Option('x', "maxradius", Required = false, HelpText = "Max crater radius.", Default = 30)]
            public int CraterMax { get; set; }

        }

        static void Main(string[] args)
        {
            //try
            //{
            Parser.Default.ParseArguments<Options>(args)
                 .WithParsed<Options>(o =>
                 {
                     if (o.ShowInfo)
                     {
                         // display info
                         image_info(o);
                     }

                     if (o.Tile)
                     {
                         // tile
                         tile_image(o);
                     }

                 })
                 .WithNotParsed(e =>
                 {
                     foreach (var item in e)
                     {
                         Console.WriteLine(item.ToString());
                     }
                 });
            //}
            //catch (Exception ex)
            //{

            //    Console.WriteLine(ex.Message);
            //}

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

        private static void image_info(Options o)
        {

            FIBITMAP dib = FreeImage.LoadEx(o.Filename);

            int Height = (int)FreeImage.GetHeight(dib);
            int Width = (int)FreeImage.GetWidth(dib);
            Console.WriteLine($"Height: {Height} Width: {Width}");
            int resolution = o.Components * o.Sections * o.Quads + 1;
            Console.WriteLine($"Components: {o.Components} x Sections: {o.Sections} x Quads: {o.Quads} = Resolution: {resolution}");
            int tw = Width / resolution;
            int th = Height / resolution;
            int mtc = Math.Min(tw, th);

            Console.WriteLine($"Tiles: {mtc} x {mtc} = {mtc * mtc} total");

            FreeImage.UnloadEx(ref dib);
        }

        private static void tile_image(Options o)
        {
            int xindex = 0;
            int yindex = 0;
            List<Tile> tiles = new List<Tile>();
            FIBITMAP dib = FreeImage.LoadEx(o.Filename);

            // calc resolution of tile
            int tileSizeResolution = o.Components * o.Sections * o.Quads + 1;

            int Height = (int)FreeImage.GetHeight(dib);
            int Width = (int)FreeImage.GetWidth(dib);

            // craterize
            if (o.Craters > 0)
            {
                CraterEngine ce = new CraterEngine() { dib = dib, minRadius = o.CraterMin, maxRadius = o.CraterMax, numCraters = o.Craters, seed = o.Seed };
                ce.BuildCraters(Path.Combine(Path.GetDirectoryName(o.Filename), "crater-debug.bmp"));

                string fn = Path.Combine(Path.GetDirectoryName(o.Filename), "crater.png");
                FreeImage.Save(FREE_IMAGE_FORMAT.FIF_PNG, dib, fn, FREE_IMAGE_SAVE_FLAGS.DEFAULT);
            }




            // calc max square size
            int max_x = Width / tileSizeResolution;
            int max_y = Height / tileSizeResolution;
            int tilecount = Math.Min(max_x, max_y);
            int cx, cy;
            cx = cy = 0;

            for (int y = 0; y < tilecount; y++)
            {
                for (int x = 0; x < tilecount; x++)
                {
                    Tile t = new Tile() { x = xindex, y = yindex };
                    FIBITMAP section = FreeImage.Copy(dib, cx, cy, cx + tileSizeResolution, cy + tileSizeResolution);
                    t.pixels = LoadPixels(section);

                    FreeImage.UnloadEx(ref section);
                    xindex++;
                    cx += tileSizeResolution;

                    tiles.Add(t);
                }
                xindex = 0;
                cx = 0;
                yindex++;
                cy += tileSizeResolution;
            }
            FreeImage.UnloadEx(ref dib);

            // stitch edges
            StichEdges(tiles);

            // write files
            string path = Path.GetDirectoryName(o.Filename);
            path = Path.Combine(path, "tiles");
            Directory.CreateDirectory(path);
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

            // clean up old file
            if (File.Exists(target))
            {
                File.Delete(target);
            }

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
