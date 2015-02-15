using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ParadoxCraft.Blocks
{
    public struct DataBlock
    {
        //sadly x * y * z > byte.max, thus we need 16bits (12bits)
        public ushort Position { get; set; }
    }
}
