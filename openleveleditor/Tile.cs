using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace openleveleditor
{
    public enum TileType
    {
        Empty,
        Internal,
        Perimeter
    }

    public class Vector3
    {
        public float X { get; set; }
        public float Y { get; set; }
        public float Z { get; set; }
    }

    public class Level
    {
        public string Name { get; set; }
        public List<Tile> Tiles { get; set; }
    }

    public class Tile
    {
        [Category("Core Structure")]
        [Description("Logical position in the level")]
        [ReadOnly(true)]
        public Vector3 Location { get; set; }

        [Category("Core Structure")]
        [Description("The floor of the tile")]
        public string Floor { get; set; }

        [Category("Core Structure")]
        [Description("The ceiling of the tile")]
        public string Ceiling { get; set; }

        [Category("Core Structure")]
        [Description("Thing to spawn in this tile")]
        public string Thing { get; set; }

        //[Category("Core Structure")]
        //[Description("Custom tags")]
        //[Editor(@"System.Windows.Forms.Design.StringCollectionEditor," +
        //"System.Design, Version=2.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a",
        //typeof(System.Drawing.Design.UITypeEditor))]
        //[TypeConverter(typeof(CsvConverter))]
        //public List<string> Tags { get; set; }

        [Category("Walls")]
        public string WallNorth { get; set; }

        [Category("Walls")]
        public string WallSouth { get; set; }

        [Category("Walls")]
        public string WallEast { get; set; }

        [Category("Walls")]
        public string WallWest { get; set; }

        [Category("Doors")]
        public string DoorNorth { get; set; }

        [Category("Doors")]
        public string DoorSouth { get; set; }

        [Category("Doors")]
        public string DoorEast { get; set; }

        [Category("Doors")]
        public string DoorWest { get; set; }



        [Browsable(false)]
        [JsonIgnore]
        public bool Selected { get; set; }

        [Browsable(false)]
        [JsonIgnore]
        public bool Highlight { get; set; }

        public Tile()
        {
           
        }

        public Point Position(int tileSize, int margin)
        {
            int cx = (int)( tileSize * Location.X + margin * Location.X);
            int cy = (int)(tileSize * Location.Y + margin * Location.Y);
            return new Point(cx, cy);
        }

        public void Clear()
        {
            this.Thing = "";
            this.WallEast = "";
            this.WallWest = "";
            this.WallNorth = "";
            this.WallSouth = "";
            this.DoorEast = "";
            this.DoorWest = "";
            this.DoorNorth = "";
            this.DoorSouth = "";
            this.Floor = "";
            this.Ceiling = "";
        }
    }


    public class CsvConverter : TypeConverter
    {
        // Overrides the ConvertTo method of TypeConverter.
        public override object ConvertTo(ITypeDescriptorContext context,
           CultureInfo culture, object value, Type destinationType)
        {
            if (value == null)
            {
                return null;
            }
            List<String> v = value as List<String>;
            if (destinationType == typeof(string))
            {
                return String.Join(",", v.ToArray());
            }
            return base.ConvertTo(context, culture, value, destinationType);
        }
    }
}
