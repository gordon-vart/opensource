using Fernandezja.ColorHashSharp;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace openleveleditor
{
    public partial class frmMain : Form
    {
        Tile currentTileHighlight;
        Tile copyTile;
        List<Tile> previousTiles = new List<Tile>();
        Point clickStart;
        int tileCount;
        int margin;
        Level level;
        int tileSize;

        public frmMain()
        {
            InitializeComponent();
            Reset();
        }

        private void Reset()
        {
            tileCount = 0;
            margin = 1;
            currentTileHighlight = null;
            previousTiles = new List<Tile>();
        }

        private void toolStripButton1_Click(object sender, EventArgs e)
        {

        }

        private void PopulateTiles()
        {
            level = new Level() { Tiles = new List<Tile>() };
            for (int x = 0; x < tileCount; x++)
            {
                for (int y = 0; y < tileCount; y++)
                {
                    Tile t = new Tile() { Location = new Vector3() { X = x, Y = y, Z = 0 } };
                    level.Tiles.Add(t);
                }
            }
        }

        private int CalcTileSize()
        {
            if (tileCount > 0)
            {
                int min = Math.Min(mainPanel.Width, mainPanel.Height);
                min = (int)(min - (tileCount * margin));
                int size = (int)Math.Floor((double)(min / tileCount));
                return size;
            }
            return 0;
        }





        private void P_MouseLeave(object sender, EventArgs e)
        {
            Panel p = (Panel)sender;
            ((Tile)p.Tag).Highlight = false;
            p.Invalidate();
            //p.BorderStyle = BorderStyle.None;
        }

        private void P_MouseEnter(object sender, EventArgs e)
        {
            Panel p = (Panel)sender;
            ((Tile)p.Tag).Highlight = true;
            p.Invalidate();
            // p.BorderStyle = BorderStyle.FixedSingle;

        }

        private void newToolStripMenuItem_Click(object sender, EventArgs e)
        {
            string promptValue = Prompt.ShowDialog("Enter number of tiles", "New Level", "30");

            if (int.TryParse(promptValue, out tileCount))
            {
                mainPanel.Width = tileCount * trackBar1.Value;
                mainPanel.Height = tileCount * trackBar1.Value;
                tileSize = CalcTileSize(); PopulateTiles();
                tileSize = CalcTileSize();
                mainPanel.Refresh();

            }
        }

        private void frmMain_ResizeEnd(object sender, EventArgs e)
        {
            tileSize = CalcTileSize();
        }

        private void frmMain_SizeChanged(object sender, EventArgs e)
        {
            tileSize = CalcTileSize();

        }

        private void saveToolStripMenuItem_Click(object sender, EventArgs e)
        {


            // save
            SaveFileDialog sfd = new SaveFileDialog();
            sfd.OverwritePrompt = true;
            sfd.AddExtension = true;
            sfd.DefaultExt = "lvl";
            sfd.Filter = "Level files (*.lvl)|*.lvl|All files (*.*)|*.*";
            if (sfd.ShowDialog() == DialogResult.OK)
            {
                string json = JsonConvert.SerializeObject(level, Formatting.Indented);
                using (FileStream fs = File.Create(sfd.FileName))
                {
                    using (StreamWriter sw = new StreamWriter(fs))
                    {
                        sw.Write(json);
                        sw.Flush();
                    }
                    fs.Close();
                }
            }
        }

        private void openToolStripMenuItem_Click(object sender, EventArgs e)
        {
            OpenFileDialog ofd = new OpenFileDialog();
            ofd.Filter = "Level files (*.lvl)|*.lvl|All files (*.*)|*.*";
            if (ofd.ShowDialog() == DialogResult.OK)
            {
                // reset
                Reset();

                // load
                string json = File.ReadAllText(ofd.FileName);
                level = JsonConvert.DeserializeObject<Level>(json);

                // deselect
                foreach (var t in level.Tiles)
                {
                    t.Highlight = false;
                    t.Selected = false;
                }

                // set tile count
                tileCount = (int)Math.Sqrt(level.Tiles.Count);
                mainPanel.Width = tileCount * trackBar1.Value;
                mainPanel.Height = tileCount * trackBar1.Value;
                tileSize = CalcTileSize();

                mainPanel.Refresh();
            }
        }

        private void mainPanel_Paint(object sender, PaintEventArgs e)
        {
            if (level != null)
            {
                Color c;
                var p = sender as Panel;
                var g = e.Graphics;

                foreach (var t in level.Tiles)
                {
                    // tile rectangle
                    Point position = t.Position(tileSize, margin);
                    Rectangle r = new Rectangle(position.X, position.Y, tileSize, tileSize);
                    if (e.ClipRectangle.IntersectsWith(r))
                    {
                        DrawBorder(g, t, r);
                        DrawBackcolor(g, t, r);
                        DrawWalls(g, t, r);
                        DrawDoors(g, t, r);
                        DrawThing(g, t, r);
                    }
                }
            }
        }

        private void DrawThing(Graphics g, Tile t, Rectangle r)
        {
            if (t.Thing.IsValid())
            {
                ColorHash ch = new ColorHash();
                Brush b = Brushes.White;

                b = new SolidBrush(ch.BuildToColor(t.Thing));
                Rectangle tmp = r;
                int size = (int)(r.Width * 0.2f) * -1;
                tmp.Inflate(size, size);
                g.FillEllipse(b, tmp);
                g.DrawEllipse(Pens.Black, tmp);
            }

        }

        private void DrawDoors(Graphics g, Tile t, Rectangle r)
        {
            Rectangle tmp = r;
            tmp.Inflate(-8, -8);
            Pen p = new Pen(Color.Fuchsia, 2);
            if (t.DoorNorth.IsValid())
            {
                g.DrawLine(p, tmp.Left, tmp.Top, tmp.Right, tmp.Top);
            }
            if (t.DoorSouth.IsValid())
            {
                g.DrawLine(p, tmp.Left, tmp.Bottom, tmp.Right, tmp.Bottom);
            }
            if (t.DoorWest.IsValid())
            {
                g.DrawLine(p, tmp.Left, tmp.Top, tmp.Left, tmp.Bottom);
            }
            if (t.DoorEast.IsValid())
            {
                g.DrawLine(p, tmp.Right, tmp.Top, tmp.Right, tmp.Bottom);
            }

        }

        private void DrawWalls(Graphics g, Tile t, Rectangle r)
        {
            Rectangle tmp = r;
            tmp.Inflate(-4, -4);
            Pen p = new Pen(Color.Aqua, 2);
            if (t.WallNorth.IsValid())
            {
                g.DrawLine(p, tmp.Left, tmp.Top, tmp.Right, tmp.Top);
            }
            if (t.WallSouth.IsValid())
            {
                g.DrawLine(p, tmp.Left, tmp.Bottom, tmp.Right, tmp.Bottom);
            }
            if (t.WallWest.IsValid())
            {
                g.DrawLine(p, tmp.Left, tmp.Top, tmp.Left, tmp.Bottom);
            }
            if (t.WallEast.IsValid())
            {
                g.DrawLine(p, tmp.Right, tmp.Top, tmp.Right, tmp.Bottom);
            }

        }

        private static void DrawBackcolor(Graphics g, Tile t, Rectangle r)
        {
            // backcolor
            Rectangle tmp = r;
            tmp.Inflate(-2, -2);
            string key = $"{t.Floor}.{t.Ceiling}";
            ColorHash ch = new ColorHash();
            Brush b = Brushes.White;
            if (string.Compare(key, ".") != 0)
            {
                b = new SolidBrush(ch.BuildToColor(key));
            }
            g.FillRectangle(b, tmp);

        }

        private static void DrawBorder(Graphics g, Tile t, Rectangle r)
        {
            Color c;
            // border
            if (t.Highlight)
            {
                c = Color.Yellow;
            }
            else if (t.Selected)
            {
                c = Color.Red;
            }
            else
            {
                c = Color.Black;
            }
            Brush b = new SolidBrush(c);
            g.FillRectangle(b, r);

        }

        private float GetBorderSize()
        {
            float r = Math.Max(1f, tileSize * 0.3f);
            return r;
        }

        private void mainPanel_MouseMove(object sender, MouseEventArgs e)
        {
            if (level != null)
            {
                // clear
                if (currentTileHighlight != null)
                {
                    currentTileHighlight.Highlight = false;
                    // paint
                    InvalidateTile(currentTileHighlight);

                }

                // set
                var tile = level.Tiles.Find(t =>
                {
                    Point position = t.Position(tileSize, margin);
                    Rectangle r = new Rectangle(position.X, position.Y, tileSize, tileSize);
                    return r.Contains(e.Location);
                });
                if (tile != null)
                {
                    // paint
                    InvalidateTile(tile);

                    tile.Highlight = true;
                    currentTileHighlight = tile;
                }
                mainPanel.Update();
            }
        }

        public void InvalidateTile(Tile tile)
        {
            Point position = tile.Position(tileSize, margin);
            mainPanel.Invalidate(new Rectangle(position, new Size(tileSize, tileSize)));
        }



        private void mainPanel_MouseDown(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                clickStart = e.Location;
            }

        }

        private void mainPanel_MouseUp(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                Point start = clickStart;
                Point end = e.Location;
                if (level != null & start != Point.Empty)
                {
                    // clear
                    foreach (var tile in previousTiles)
                    {
                        tile.Selected = false;
                        // paint
                        InvalidateTile(tile);

                    }
                    previousTiles.Clear();

                    // get selected region
                    var region = new Rectangle(Math.Min(start.X, end.X),
                        Math.Min(start.Y, end.Y),
                        Math.Abs(start.X - end.X),
                        Math.Abs(start.Y - end.Y));
                    mainPanel.CreateGraphics().DrawRectangle(Pens.Aqua, region);

                    // find selected
                    var tiles = level.Tiles.Where(t =>
                    {
                        Point position = t.Position(tileSize, margin);
                        Rectangle r = new Rectangle(position.X, position.Y, tileSize, tileSize);
                        return region.IntersectsWith(r);
                    });

                    // select
                    foreach (var tile in tiles)
                    {
                        tile.Selected = true;
                        // paint
                        InvalidateTile(tile);

                    }

                    // prop editor
                    propertyGrid1.SelectedObjects = tiles.ToArray();

                    // store selected
                    previousTiles.AddRange(tiles);

                    // paint
                    mainPanel.Update();
                }
            }
        }

        private void trackBar1_ValueChanged(object sender, EventArgs e)
        {
            mainPanel.Width = tileCount * trackBar1.Value;
            mainPanel.Height = tileCount * trackBar1.Value;
            tileSize = CalcTileSize();
            mainPanel.Refresh();
        }

        private void propertyGrid1_PropertyValueChanged(object s, PropertyValueChangedEventArgs e)
        {
            foreach (var tile in previousTiles)
            {
                // paint
                InvalidateTile(tile);
            }


            // paint
            mainPanel.Update();
        }

        private void copyToolStripMenuItem_Click(object sender, EventArgs e)
        {
            copyTile = previousTiles.FirstOrDefault();
        }

        private void allToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Copy("Floor", "Ceiling", "Thing", "WallNorth", "WallSouth", "WallEast", "WallWest", "DoorNorth", "DoorSouth", "DoorEast", "DoorWest");
        }

        private void Copy(params string[] attributes)
        {
            if (copyTile != null)
            {
                PropertyInfo[] properties = typeof(Tile).GetProperties();

                foreach (var t in previousTiles)
                {
                    // paint
                    InvalidateTile(t);

                    foreach (PropertyInfo property in properties)
                    {
                        if (attributes.Contains(property.Name))
                        {
                            property.SetValue(t, property.GetValue(copyTile));
                        }

                    }

                }

                // paint
                mainPanel.Update();
            }
        }

        private void floorCeilingToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Copy("Floor", "Ceiling");

        }

        private void thingToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Copy("Thing");

        }

        private void doorsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Copy("DoorNorth", "DoorSouth", "DoorEast", "DoorWest");

        }

        private void wallsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Copy("WallNorth", "WallSouth", "WallEast", "WallWest");
        }

        private void clearToolStripMenuItem_Click(object sender, EventArgs e)
        {
            foreach (var t in previousTiles)
            {
                // paint
                InvalidateTile(t);

                t.Clear();
            }

            // paint
            mainPanel.Update();
        }
    }
}