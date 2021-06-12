using FreeImageAPI;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace tileimage
{
    public class TileEngine
    {
        FIBITMAP dib;
        string path;
        List<Tile> tiles = new List<Tile>();
        int ImageHeight;
        int ImageWidth;
        public event statusChangedDelegate StatusEvent;

        public TileEngine(FIBITMAP image, string dir)
        {
            dib = image;
            path = dir;
            ImageHeight = (int)FreeImage.GetHeight(dib);
            ImageWidth = (int)FreeImage.GetWidth(dib);
        }

        public void Tile(int tileSizeResolution)
        {
            int xindex = 0;
            int yindex = 0;
            // calc max square size
            int max_x = ImageWidth / tileSizeResolution;
            int max_y = ImageHeight / tileSizeResolution;
            int tilecount = Math.Min(max_x, max_y);
            int cx, cy;
            cx = cy = 0;

            string tilePath = Path.Combine(path, "tiles");
            Directory.CreateDirectory(tilePath);
            string pngPath = Path.Combine(tilePath, "png");
            Directory.CreateDirectory(pngPath);


            for (int y = 0; y < tilecount; y++)
            {
                for (int x = 0; x < tilecount; x++)
                {
                    // write png
                    string pngfile = string.Format(@"tile_x{0}_y{1}.png", xindex, yindex);
                    pngfile = Path.Combine(pngPath, pngfile);
                    FIBITMAP section = FreeImage.Copy(dib, cx, cy, cx + tileSizeResolution, cy + tileSizeResolution);
                    FreeImage.Save(FREE_IMAGE_FORMAT.FIF_PNG, section, pngfile, FREE_IMAGE_SAVE_FLAGS.PNG_Z_DEFAULT_COMPRESSION);

                    int count = tileSizeResolution * tileSizeResolution;
                    ushort[] pixeldata = new ushort[count];
                    int idx = 0;

                    // Note: dib is stored upside down
                    for (int k = (int)FreeImage.GetHeight(section) - 1; k >= 0; k--)
                    {
                        List<int> row = new List<int> { };

                        Scanline<ushort> line = new Scanline<ushort>(section, k);
                        foreach (ushort pixel in line)
                        {
                            pixeldata[idx] = pixel;
                            idx++;
                        }

                    }
                    string tilename = string.Format(@"tile_x{0}_y{1}.raw", xindex, yindex);
                    tilename = Path.Combine(tilePath, tilename);

                    // status
                    if (StatusEvent != null)
                    {
                        StatusEvent(this, $"Writing {tilename}");
                    }

                    // clean up old file
                    if (File.Exists(tilename))
                    {
                        File.Delete(tilename);
                    }

                    //unsafe
                    //{


                    //    string bmptilename = string.Format(@"tile_x{0}_y{1}.png", xindex, yindex);
                    //    bmptilename = Path.Combine(fpath, bmptilename);
                    //    Bitmap b = new Bitmap(tileSizeResolution + 1, tileSizeResolution + 1, PixelFormat.Format48bppRgb);
                    //    var bmd = b.LockBits(new Rectangle(0, 0, tileSizeResolution, tileSizeResolution), ImageLockMode.ReadWrite, PixelFormat.Format48bppRgb);

                    //    byte bitsPerPixel = 48;
                    //    ushort* scan0 = (ushort*)bmd.Scan0.ToPointer();

                    //    for (int i = 0; i < pixeldata.Length; i++)
                    //    {
                    //        ushort* data = scan0 + i * (bitsPerPixel / 16);
                    //        //*data = pixeldata[offset];

                    //        //data is a pointer to the first 16 bits of the 48-bit color data
                    //        data[0] = pixeldata[i];
                    //        data[1] = pixeldata[i];
                    //        data[2] = pixeldata[i];
                    //    }
                    //    b.UnlockBits(bmd);
                    //    //Image ib = Image.FromHbitmap(b.GetHbitmap());
                    //    b.Save(bmptilename, ImageFormat.Png);
                    //}
                    using (Stream fs = File.OpenWrite(tilename))
                    {
                        using (BinaryWriter bw = new BinaryWriter(fs))
                        {
                            foreach (var pixel in pixeldata)
                            {
                                bw.Write(pixel);
                            }
                            bw.Flush();
                        }
                        fs.Close();
                    }
                    xindex++;
                    cx += tileSizeResolution - 1;

                    FreeImage.UnloadEx(ref section);
                }
                xindex = 0;
                cx = 0;
                yindex++;
                cy += tileSizeResolution - 1;
            }
        }

        public void ScaleZ(float scale)
        {
            // scale heights
            for (uint i = 0; i < FreeImage.GetHeight(dib); i++)
            {
                Scanline<ushort> line = new Scanline<ushort>(dib, (int)i);
                ushort[] data = line.Data;
                for (int k = 0; k < data.Length; k++)
                {
                    ushort v = data[k];
                    ushort adj = (ushort)(v * scale);
                    data[k] = adj;
                }
                line.Data = data;
            }
        }
    }
}
