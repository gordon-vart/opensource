using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace tileimage
{
    public class RandomEx
    {
        RNGCryptoServiceProvider provider = new RNGCryptoServiceProvider();
        public double NextDouble()
        {
            var byteArray = new byte[4];
            provider.GetBytes(byteArray);
            UInt32 rand = BitConverter.ToUInt32(byteArray, 0);
            return rand / (1.0 + UInt32.MaxValue);
        }

        public double NextDouble(double min, double max)
        {
            return NextDouble() * (max - min) + min;
        }
    }
}
