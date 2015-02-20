using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ParadoxCraft.Blocks
{
    /// <summary>
    /// List of possible material types
    /// </summary>
    /// <remarks>
    /// When adding terrain materials, also update VertexTextureTerrain
    /// </remarks>
    public enum MaterialType : ushort
    {
        Stone = 0,
        Cobblestone = 1,
        Dirt = 2,
        Grass = 3,
        Sand = 4
    }
}
