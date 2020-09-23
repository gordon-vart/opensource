using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace openleveleditor
{
    public static class Extensions
    {
        public static bool IsValid(this string data)
        {
            return !string.IsNullOrEmpty(data);
        }
    }
}
