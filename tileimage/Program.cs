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

            [Option("zscale", Required = false, HelpText = "Scale height on Z axis.", Default = 100)]
            public int ZScale { get; set; }

        }

        static void Main(string[] args)
        {
            try
            {
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
            }
            catch (Exception ex)
            {

                Console.WriteLine(ex.Message);
            }

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
            // calc resolution of tile
            int tileSizeResolution = o.Components * o.Sections * o.Quads + 1;
            FIBITMAP dib = FreeImage.LoadEx(o.Filename);

            // craterize
            if (o.Craters > 0)
            {
                CraterEngine ce = new CraterEngine() { dib = dib, minRadius = o.CraterMin, maxRadius = o.CraterMax, numCraters = o.Craters, seed = o.Seed };
                ce.BuildCraters(Path.Combine(Path.GetDirectoryName(o.Filename), "crater-debug.bmp"));

                string fn = Path.Combine(Path.GetDirectoryName(o.Filename), "crater.png");
                FreeImage.Save(FREE_IMAGE_FORMAT.FIF_PNG, dib, fn, FREE_IMAGE_SAVE_FLAGS.DEFAULT);
            }

            // scale heights
            for (uint i = 0; i < FreeImage.GetHeight(dib); i++)
            {
                Scanline<ushort> line = new Scanline<ushort>(dib, (int)i);
                ushort[] data = line.Data;
                for (int k = 0; k < data.Length; k++)
                {
                    ushort v = data[k];
                    ushort adj = (ushort)(v * (o.ZScale / 100.0f));
                    data[k] = adj;
                }
                line.Data = data;
            }

            // tile
            TileEngine te = new TileEngine(dib, Path.GetDirectoryName(o.Filename));
            te.Tile(tileSizeResolution);

        }

    }
}
