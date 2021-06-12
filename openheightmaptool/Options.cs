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
        [Description("The filename of the heightmap image.")]
        public string Filename { get; set; }

        [Category("Image")]
        [Description("Skip the preview of the image.")]
        public bool SkipPreview { get; set; }


        [Category("Tiling")]
        [Description("The number of vertices per tile (must be a power of 2). i.e. . 8, 16, 32, 64, 128, 256, etc...")]
        public int VerticesPerTile { get; set; }

        [Category("Crater")]
        [Description("The number of craters to generate")]
        public int Craters { get; set; }

        [Category("Crater")]
        [Description("Controls randomness of craters")]
        public int Seed { get; set; }

        [Category("Crater")]
        [Description("The minumum radius in pixels")]
        public int CraterMinRadius { get; set; }

        [Category("Crater")]
        [Description("The mamiumum radius in pixels")]
        public int CraterMaxRadius { get; set; }

        [Category("Scaling")]
        [Description("The amount to scale heightmap values by as a percent. i.e. 50 = 50% - use to reduce overall height")]
        public int ZScale { get; set; }

    }
}
