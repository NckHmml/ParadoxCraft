using ParadoxCraft.Helpers;
using ParadoxCraft.Terrain;
using SiliconStudio.Core.Mathematics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ParadoxCraft.Blocks.Chunks
{
    public class ChunkFactory
    {
        /// <summary>
        /// Terrain entity
        /// </summary>
        public GraphicalTerrain Terrain { get; set; }

        /// <summary>
        /// Chunk buffer
        /// </summary>
        private List<Point3> Chunks { get; set; }

        /// <summary>
        /// Requests a chunk at the chunkloader
        /// </summary>
        public Action<Point3> RequestChunk;

        /// <summary>
        /// Creates a new instance of <see cref="ChunkFactory"/>
        /// </summary>
        public ChunkFactory()
        {
            Chunks = new List<Point3>();
        }

        /// <summary>
        /// Checks if a chunk needs to be loaded
        /// </summary>
        /// <param name="x">X to check</param>
        /// <param name="y">Y to check</param>
        /// <param name="z">Z to check</param>
        public void CheckLoad(double x, double y, double z)
        {
            Point3 position = new Point3()
            {
                X = (int)Math.Round(x),
                Y = (short)Math.Round(y),
                Z = (int)Math.Round(z),
            };
            if (Chunks.Contains(position)) return;
            Chunks.Add(position);
            RequestChunk(position);
        }

        /// <summary>
        /// Processes chunk data to create graphical data
        /// </summary>
        /// <param name="position">Position of chunk</param>
        /// <param name="chunk">Chunk to process</param>
        public void ProcessChunk(Point3 position, DataChunk chunk)
        {
            Vector3 blockPos;
            DataBlock block;
            for (int i = 0; i < chunk.Blocks.Length; i++)
            {
                block = chunk.Blocks[i];

                byte
                    x = (byte)((block.Position & 0x00F)),
                    z = (byte)((block.Position & 0x0F0) / 0x010),
                    y = (byte)((block.Position & 0xF00) / 0x100);

                blockPos = new Vector3(position.X * Constants.ChunkSize + x, position.Y * Constants.ChunkSize + y, position.Z * Constants.ChunkSize + z);
                Terrain.AddBlock(new GraphicalBlock(blockPos, BlockSides.All));
            }
            Terrain.Build();
        }
    }
}
