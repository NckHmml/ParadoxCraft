﻿using SiliconStudio.Core.Mathematics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ParadoxCraft.Blocks
{
    /// <summary>
    /// A block that contains all information to be drawn
    /// </summary>
    public class GraphicalBlock
    {
        /// <summary>
        /// Sides of the current block
        /// </summary>
        public BlockSides Sides { get; set; }

        /// <summary>
        /// Position of te current block
        /// </summary>
        public Vector3 Position { get; private set; }

        public ushort Material { get; set; }

        /// <summary>
        /// Create a new renderable block instance
        /// </summary>
        /// <param name="position">Draw position</param>
        /// <param name="sides">Sides to draw</param>
        public GraphicalBlock(Vector3 position, BlockSides sides, MaterialType material)
        {
            Sides = sides;
            Position = position;
            Material = (ushort)material;
        }

        /// <summary>
        /// Checks if the current block has a certain side
        /// </summary>
        /// <param name="side">Side to check</param>
        /// <returns>True if the block has <paramref name="side"/> as side</returns>
        public bool HasSide(BlockSides side)
        {
            return (Sides & side) != 0;
        }

        public uint GetMaterialCode(BlockSides side)
        {
            switch (Material)
            {
                case 3: // Grass, TODO: Material type enum
                    switch (side)
                    {
                        case BlockSides.Back:
                        case BlockSides.Front:
                        case BlockSides.Left:
                        case BlockSides.Right:
                            return Material + 0x100u;
                        case BlockSides.Top:
                            return Material + 0x200u;
                        default:
                            return Material;
                    }
                default:
                    return Material;
            }
        }
    }
}
