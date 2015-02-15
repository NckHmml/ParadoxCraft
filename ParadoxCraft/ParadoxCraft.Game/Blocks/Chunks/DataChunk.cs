using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ParadoxCraft.Blocks.Chunks
{
    public class DataChunk
    {
        public DataBlock[] Blocks = new DataBlock[0x1000];

        public bool HasBlock(int position)
        {
            if (position < 0 || position > Blocks.Length - 1)
                return true; 
            return Blocks[position] != null;
        }
    }
}
