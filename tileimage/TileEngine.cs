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

            string fpath = Path.Combine(path, "tiles");
            Directory.CreateDirectory(fpath);


            for (int y = 0; y < tilecount; y++)
            {
                for (int x = 0; x < tilecount; x++)
                {
                    FIBITMAP section = FreeImage.Copy(dib, cx, cy, cx + tileSizeResolution, cy + tileSizeResolution);
                    
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
                    tilename = Path.Combine(fpath, tilename);

                    // clean up old file
                    if (File.Exists(tilename))
                    {
                        File.Delete(tilename);
                    }

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
    }
}
