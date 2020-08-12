using System;
using System.ComponentModel;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace tileimage
{
    public class Options
    {

        [Category("Image")]
        [EditorAttribute(typeof(System.Windows.Forms.Design.FileNameEditor), typeof(System.Drawing.Design.UITypeEditor))]
        public string Filename { get; set; }

        [Category("Tiling")]
        [Description("The number of vertices per tile (must be a power of 2). i.e. . 8, 16, 32, 64, 128, 256, etc...")]
        public int VerticesPerTile { get; set; }

        [Category("Crater")]
        public int Craters { get; set; }

        [Category("Randomness")]
        public int Seed { get; set; }

        [Category("Crater")]
        public int CraterMinRadius { get; set; }

        [Category("Crater")]
        public int CraterMaxRadius { get; set; }

        [Category("Scaling")]
        public int ZScale { get; set; }

    }
}
