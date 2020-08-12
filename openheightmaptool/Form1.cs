using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Numerics;
using System.Windows.Forms;
using tileimage;
using System.IO;
using FreeImageAPI;
using System.Reflection;
using System.Diagnostics;

namespace openheightmaptool
{
    public partial class Form1 : Form
    {

        private CraterEngine ce = null;
        public Form1()
        {
            InitializeComponent();

            Version v = Assembly.GetExecutingAssembly().GetName().Version;

            this.Text = $"OpenTileVersion v{v.Major}.{v.Minor}";
            pgOptions.SelectedObject = new Options() { VerticesPerTile = 256, Seed = 1, ZScale = 100, Craters = 0, CraterMinRadius = 10, CraterMaxRadius = 35 };

            pgOptions.PropertyValueChanged += PgOptions_PropertyValueChanged;
        }

        private void PgOptions_PropertyValueChanged(object s, PropertyValueChangedEventArgs e)
        {
            RenderImage();
        }

        private void openToolStripMenuItem_Click(object sender, EventArgs e)
        {
            OpenFileDialog ofd = new OpenFileDialog();
            if (ofd.ShowDialog() == DialogResult.OK)
            {
                ((Options)pgOptions.SelectedObject).Filename = ofd.FileName;
                pgOptions.Refresh();
            }
        }

        private void RenderImage()
        {
            Options o = GetOptions();

            try
            {
                tslInfo.Text = "";

                // Validate number of vertices per tile
                int k = o.VerticesPerTile & (o.VerticesPerTile - 1);
                if (k != 0)
                {
                    throw new Exception($"VerticesPerTile value { o.VerticesPerTile} must be a power of 2.");
                }

                // Validate number of vertices per tile

                if (o.VerticesPerTile < 8)
                {
                    throw new Exception($"VerticesPerTile value { o.VerticesPerTile} must be greater than or equal to 8.");
                }

                // load
                FIBITMAP dib = FreeImage.LoadEx(o.Filename);

                // calc resolution of tile
                int tileSizeResolution = o.VerticesPerTile - 1;

                // show tiles
                FREE_IMAGE_TYPE it = FreeImage.GetImageType(dib);
                uint bpp = FreeImage.GetBPP(dib);

                // Warn if bits-per-pixel is less than 16
                if (bpp != 16)
                {
                    tslInfo.Text = $"Warning! The image provided it's not 16 bits per pixel. 16 bpp grayscale image needed.";
                }

                groupBox1.Text = $"{Path.GetFileName(o.Filename)}, {FreeImage.GetWidth(dib)}x{FreeImage.GetHeight(dib)}, {bpp} bpp";
                Image i = Image.FromFile(o.Filename);
                Bitmap bmp = new Bitmap(i);

                Graphics g = Graphics.FromImage(bmp);
                var tiles = MathEx.GetTiles(bmp.Width, bmp.Height, tileSizeResolution);
                foreach (var item in tiles)
                {
                    g.DrawRectangle(Pens.LightGreen, item);
                }

                // show craters
                // craterize
                if (o.Craters > 0)
                {
                    ce = new CraterEngine() { dib = dib, minRadius = o.CraterMinRadius, maxRadius = o.CraterMaxRadius, numCraters = o.Craters, seed = o.Seed };
                    ce.BuildCraters();

                    foreach (var crater in ce.craters)
                    {
                        foreach (var item in crater.craterRidgePoints)
                        {
                            bmp.SetPixel((int)item.X, (int)item.Y, Color.Red);
                        }
                    }
                }


                // show image
                pictureBox1.Image = bmp;

                // free
                FreeImage.Unload(dib);
            }
            catch (Exception ex)
            {
                tslInfo.Text = ex.Message;
            }
        }

        private Options GetOptions()
        {
            return (Options)pgOptions.SelectedObject;
        }

        private void toolStripButton1_Click(object sender, EventArgs e)
        {
            toolStripButton1.Enabled = false;
            pgOptions.Enabled = false;

            // Set cursor as hourglass
            Cursor.Current = Cursors.WaitCursor;

            //thread
            BackgroundWorker w = new BackgroundWorker();
            w.DoWork += TileImage;
            w.ProgressChanged += W_ProgressChanged;
            w.WorkerReportsProgress = true;
            w.RunWorkerCompleted += W_RunWorkerCompleted;
            w.RunWorkerAsync();
        }

        private void W_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            tslInfo.Text = e.Result.ToString();

            toolStripButton1.Enabled = true;
            pgOptions.Enabled = true;

            // Set cursor as default arrow
            Cursor.Current = Cursors.Default;
        }

        private void W_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            tslInfo.Text = e.UserState.ToString();
        }

        private void TileImage(object sender, DoWorkEventArgs e)
        {
            try
            {

                Options o = GetOptions();
                FIBITMAP dib = FreeImage.LoadEx(o.Filename);

                // calc resolution of tile
                int tileSizeResolution = o.VerticesPerTile - 1;

                // commit to dib
                if (o.Craters > 0)
                {
                    // status
                    ce.StatusEvent += (k, msg) => { ((BackgroundWorker)sender).ReportProgress(0, msg); };

                    // reset dib
                    ce.dib = dib;
                    ce.Commit();

                    // save crater heightmap
                    string fn = Path.Combine(Path.GetDirectoryName(o.Filename), $"{Path.GetFileNameWithoutExtension(o.Filename)}-crater.png");
                    FreeImage.Save(FREE_IMAGE_FORMAT.FIF_PNG, dib, fn, FREE_IMAGE_SAVE_FLAGS.DEFAULT);

                    // save crater debug
                    fn = Path.Combine(Path.GetDirectoryName(o.Filename), $"{Path.GetFileNameWithoutExtension(o.Filename)}-crater-debug.png");
                    ce.debugbmp.Save(fn);
                }

                // tile
                TileEngine te = new TileEngine(dib, Path.GetDirectoryName(o.Filename));

                // status
                te.StatusEvent += (k, msg) => { ((BackgroundWorker)sender).ReportProgress(0, msg); };

                te.ScaleZ(o.ZScale / 100.0f);
                te.Tile(tileSizeResolution);

                // free
                FreeImage.Unload(dib);

                e.Result = "Finished";

            }
            catch (Exception ex)
            {
                e.Result = ex.Message;
            }
        }

        private void exitToolStripMenuItem_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        private void aboutToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Process.Start("https://github.com/gordon-vart/opensource/wiki");
        }
    }
}
