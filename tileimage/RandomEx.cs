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
        public float NextFloat()
        {
            var byteArray = new byte[4];
            provider.GetBytes(byteArray);
            UInt32 rand = BitConverter.ToUInt32(byteArray, 0);
            return (float)( rand / (1.0 + UInt32.MaxValue));
        }

        public float NextFloat(float min, float max)
        {
            return NextFloat() * (max - min) + min;
        }
    }
}
