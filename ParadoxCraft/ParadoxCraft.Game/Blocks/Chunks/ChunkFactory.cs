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
        private Dictionary<Point3, DataChunk> Chunks { get; set; }

        /// <summary>
        /// Requests a chunk at the chunkloader
        /// </summary>
        public Action<Point3> RequestChunk;

        /// <summary>
        /// General locker
        /// </summary>
        public object Locker = new object();

        /// <summary>
        /// Creates a new instance of <see cref="ChunkFactory"/>
        /// </summary>
        public ChunkFactory()
        {
            Chunks = new Dictionary<Point3, DataChunk>();
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
            if (Chunks.ContainsKey(position)) return;
            lock (Locker)
                Chunks.Add(position, null);
            RequestChunk(position);
        }

        /// <summary>
        /// Purges chunks beyond a certain distance
        /// </summary>
        public void PurgeDistancedChunks(double x, double y, double z, int radius)
        {
            lock (Locker)
            {
                List<Point3> toRemove = new List<Point3>();
                // TODO: check y
                foreach (var chunk in Chunks.Where(pair =>
                    {
                        var height = x - pair.Key.X;
                        var width = z - pair.Key.Z;
                        var distance = Math.Sqrt((height * height) + (width * width));
                        return distance > radius;
                    }))
                    toRemove.Add(chunk.Key);

                Terrain.PurgeChunks(toRemove);

                foreach (var chunk in toRemove)
                    Chunks.Remove(chunk);
            }
        }

        /// <summary>
        /// Processes chunk data to create graphical data
        /// </summary>
        /// <param name="position">Position of chunk</param>
        /// <param name="chunk">Chunk to process</param>
        public void ProcessChunk(Point3 position, DataChunk chunk)
        {
            lock (Locker)
            {
                Chunks[position] = chunk;
                Vector3 blockPos;
                DataBlock block;
                BlockSides sides;
                for (int i = 0; i < chunk.Blocks.Length; i++)
                {
                    block = chunk.Blocks[i];
                    if (block == null)
                        continue;

                    byte
                        x = (byte)((block.Position & 0x00F)),
                        z = (byte)((block.Position & 0x0F0) / 0x010),
                        y = (byte)((block.Position & 0xF00) / 0x100);

                    blockPos = new Vector3(position.X * Constants.ChunkSize + x, position.Y * Constants.ChunkSize + y, position.Z * Constants.ChunkSize + z);
                    sides = CalculateBlockSides(chunk, block.Position);

                    Terrain.AddBlock(position, new GraphicalBlock(blockPos, sides));
                }

                CheckLeftChunk(position, chunk);
                CheckRightChunk(position, chunk);
            }

            Terrain.Build();
        }

        #region BlockSide Checks
        /// <summary>
        /// Calculates the visable sides for blocks within the <paramref name="chunk"/>
        /// </summary>
        /// <param name="chunk">Chunk of the current block</param>
        /// <param name="blockPosition">Relative position of the block</param>
        private BlockSides CalculateBlockSides(DataChunk chunk, ushort blockPosition)
        {
            var sides = BlockSides.None;
            byte
                x = (byte)((blockPosition & 0x00F)),
                z = (byte)((blockPosition & 0x0F0) / 0x010),
                y = (byte)((blockPosition & 0xF00) / 0x100);

            if (!chunk.HasBlock(blockPosition + 0x001) && x != 0xF)
                sides |= BlockSides.Left;
            if (!chunk.HasBlock(blockPosition - 0x001) && x != 0x0)
                sides |= BlockSides.Right;

            if (!chunk.HasBlock(blockPosition + 0x010) && z != 0xF)
                sides |= BlockSides.Front;
            if (!chunk.HasBlock(blockPosition - 0x010) && z != 0x0)
                sides |= BlockSides.Back;

            if (!chunk.HasBlock(blockPosition + 0x100) && y != 0xF)
                sides |= BlockSides.Top;
            if (!chunk.HasBlock(blockPosition - 0x100) && y != 0x0)
                sides |= BlockSides.Bottom;

            return sides;
        }

        /// <summary>
        /// Checks edges in the relative left chunk
        /// </summary>
        private void CheckLeftChunk(Point3 chunkPos, DataChunk chunk)
        {
            Point3 leftPos = chunkPos;
            leftPos.X -= 1;
            if (!Chunks.ContainsKey(leftPos) || Chunks[leftPos] == null) return;
            DataChunk leftChunk = Chunks[leftPos];

            for (int i = 0; i <= 0xFF; i++)
            {
                int 
                    right = (i * 0x10) + 0xF,
                    left = i * 0x10;
                byte 
                    z = (byte)((i & 0x00F)),
                    y = (byte)((i & 0x0F0) / 0x010);

                if (chunk.HasBlock(right) && !leftChunk.HasBlock(left))
                {
                    var blockPos = new Vector3(chunkPos.X * Constants.ChunkSize + 0xF, chunkPos.Y * Constants.ChunkSize + y, chunkPos.Z * Constants.ChunkSize + z);
                    Terrain.AddBlock(chunkPos, new GraphicalBlock(blockPos, BlockSides.Left));
                }
                else if (!chunk.HasBlock(right) && leftChunk.HasBlock(left))
                {
                    var blockPos = new Vector3(leftPos.X * Constants.ChunkSize + 0x0, leftPos.Y * Constants.ChunkSize + y, leftPos.Z * Constants.ChunkSize + z);
                    Terrain.AddBlock(leftPos, new GraphicalBlock(blockPos, BlockSides.Right));
                }
            }
        }

        /// <summary>
        /// Checks edges in the relative right chunk
        /// </summary>
        private void CheckRightChunk(Point3 chunkPos, DataChunk chunk)
        {
            Point3 rightPos = chunkPos;
            rightPos.X += 1;
            if (!Chunks.ContainsKey(rightPos) || Chunks[rightPos] == null) return;
            DataChunk rightChunk = Chunks[rightPos];

            for (int i = 0; i <= 0xFF; i++)
            {
                int
                    right = (i * 0x10) + 0xF,
                    left = i * 0x10;
                byte
                    z = (byte)((i & 0x00F)),
                    y = (byte)((i & 0x0F0) / 0x010);

                if (chunk.HasBlock(left) && !rightChunk.HasBlock(right))
                {
                    var blockPos = new Vector3(chunkPos.X * Constants.ChunkSize + 0x0, chunkPos.Y * Constants.ChunkSize + y, chunkPos.Z * Constants.ChunkSize + z);
                    Terrain.AddBlock(chunkPos, new GraphicalBlock(blockPos, BlockSides.Right));
                }
                else if (!chunk.HasBlock(left) && rightChunk.HasBlock(right))
                {
                    var blockPos = new Vector3(rightPos.X * Constants.ChunkSize + 0xF, rightPos.Y * Constants.ChunkSize + y, rightPos.Z * Constants.ChunkSize + z);
                    Terrain.AddBlock(rightPos, new GraphicalBlock(blockPos, BlockSides.Left));
                }
            }
        }
        #endregion
    }
}
