using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
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
                craterize(dib, o);
            }


            string fn = Path.Combine(Path.GetDirectoryName(o.Filename), "crater.png");
            FreeImage.Save(FREE_IMAGE_FORMAT.FIF_PNG, dib, fn, FREE_IMAGE_SAVE_FLAGS.DEFAULT);


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

        private static void craterize(FIBITMAP dib, Options o)
        {
            Random rng = new Random(o.Seed);
            List<Crater> craters = new List<Crater>();

            int Height = (int)FreeImage.GetHeight(dib);
            int Width = (int)FreeImage.GetWidth(dib);

            for (int c = 0; c < o.Craters; c++)
            {
                int x = rng.Next(0, Width);
                int y = rng.Next(0, Height);
                double alpha = rng.NextDouble();
                int radius = (int)MathEx.Lerp(o.CraterMin, o.CraterMax, MathEx.EaseIn(alpha, 10));
                Crater crater = new Crater(x, y, radius);
                craters.Add(crater);

            }

            // generate craters
            if (false)
            {
                foreach (var item in craters)
                {
                    item.GeneratePixels2(Width, Height);
                }
            }
            else
            {
                Parallel.ForEach(craters, (c) =>
                {
                    c.GeneratePixels2(Width, Height);
                });
            }


            // draw craters
            int i = 0;
            foreach (var item in craters)
            {
                drawCrater(dib, item, Width, Height, o);
                Console.WriteLine($"{i + 1}. {item.origin.x},{item.origin.y} radius: {item.radius}");
                i++;
            }
        }

        private static void drawCrater(FIBITMAP dib, Crater crater, int width, int height, Options opt)
        {
            double radius = crater.radius;

            // scale x/y to z so we can scale crater depth
            double scale = ushort.MaxValue / Math.Min(width, height);

            // generate crater border

            // get highest/lowest point in the crater
            double scalealpha = radius / opt.CraterMax;
            PointXY low = crater.craterPoints.OrderBy(o => dib.GetHeight(o)).First();
            ushort lowHeight = dib.GetHeight(low);
            PointXY high = crater.craterPoints.OrderByDescending(o => dib.GetHeight(o)).First();
            ushort highHeight = dib.GetHeight(high);
            int lowPoint = Math.Max(0, lowHeight - (int)(radius * scale));

            // draw inside
            // set crater pixel heights
            foreach (var item in crater.craterPoints)
            {
                //get nearest ridge point height
                PointXY nearRidge = crater.craterRidgePoints.OrderBy(o => crater.dist(item, o)).First();
                //double k = crater.dist(item, nearRidge);
                int ridgeHeight = dib.GetHeight(nearRidge);
                ridgeHeight += (int)(Math.Max(1, (radius / 3)) * scale);
                //int h = dib.GetHeight(item);
                double d = crater.DistanceFromCenter(item);
                double alpha = d / radius;
                double newHeight = MathEx.Lerp(lowPoint, ridgeHeight, MathEx.EaseIn(alpha, 5));
                dib.SetHeight(item, (ushort)(newHeight));
            }


            // draw ridge
            foreach (var item in crater.craterRidgePoints)
            {
                int h = dib.GetHeight(item);
                dib.SetHeight(item, Math.Min(ushort.MaxValue, (ushort)(h + (int)(Math.Max(1, (radius / 3)) * scale))));
                dib.SetHeight(item, ushort.MaxValue);
            }

            // smooth
            if (true)
            {
                for (int cx = (int)(crater.origin.x - radius); cx < (int)(crater.origin.x + radius); cx++)
                {
                    for (int cy = (int)(crater.origin.y - radius); cy < (int)(crater.origin.y + radius); cy++)
                    {
                        List<int> k = new List<int>();
                        k.Add(GetPointHeight(dib, cx + 1, cy, width, height));
                        k.Add(GetPointHeight(dib, cx - 1, cy, width, height));
                        k.Add(GetPointHeight(dib, cx, cy + 1, width, height));
                        k.Add(GetPointHeight(dib, cx, cy - 1, width, height));
                        int valid = k.Count(o => o > 0);
                        int total = k.Where(o => o > 0).Sum();
                        PointXY p = new PointXY(cx, cy);
                        if (valid > 0)
                        {
                            dib.SetHeight(p, (ushort)(total / valid));
                        }
                    }
                }
            }

        }

        private static int GetPointHeight(FIBITMAP dib, double x, double y, int width, int height)
        {
            if (x >= 0 && y >= 0 && x < width && y < height)
            {
                PointXY p = new PointXY(x, y);
                return dib.GetHeight(p);
            }
            else
            {
                return -1;
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
