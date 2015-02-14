using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ParadoxCraft.Block
{
    /// <summary>
    /// All posible sides a block could have
    /// </summary>
    [Flags]
    public enum BlockSides : byte
    {
        Top = 0x01,
        Bottom = 0x02,

        Front = 0x04,
        Back = 0x08,

        Left = 0x10,
        Right = 0x20,

        None = 0x00,
        All = 0x3F
    }
}
