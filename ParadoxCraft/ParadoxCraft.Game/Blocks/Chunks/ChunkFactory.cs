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
                var position = new Point3<double>()
                {
                    X = x,
                    Y = y,
                    Z = z
                };

                foreach (Point3 pos in Terrain.PurgeChunks(position, radius).Distinct())
                    Chunks.Remove(pos);
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
                Chunks[position] = chunk;
            Vector3 blockPos;
            DataBlock block;
            BlockSides sides;
            List<GraphicalBlock> toAdd = new List<GraphicalBlock>();
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

                toAdd.Add(new GraphicalBlock(blockPos, sides, block.Material));
            }

            CheckLeftChunk(position, chunk, toAdd);
            CheckRightChunk(position, chunk, toAdd);
            CheckFrontChunk(position, chunk, toAdd);
            CheckBackChunk(position, chunk, toAdd);
            CheckTopChunk(position, chunk, toAdd);
            CheckBottomChunk(position, chunk, toAdd);

            Terrain.AddBlocks(position, toAdd);
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
                sides |= BlockSides.Right;
            if (!chunk.HasBlock(blockPosition - 0x001) && x != 0x0)
                sides |= BlockSides.Left;

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
        private void CheckLeftChunk(Point3 chunkPos, DataChunk chunk, List<GraphicalBlock> toAdd)
        {
            Point3 leftPos = chunkPos;
            leftPos.X -= 1;
            DataChunk leftChunk;
            if (!Chunks.TryGetValue(leftPos, out leftChunk) || leftChunk == null) return;

            for (int i = 0; i <= 0xFF; i++)
            {
                int 
                    right = (i * 0x10) + 0xF,
                    left = i * 0x10;
                byte 
                    z = (byte)((i & 0x00F)),
                    y = (byte)((i & 0x0F0) / 0x010);

                if (chunk.HasBlock(left) && !leftChunk.HasBlock(right))
                {
                    var blockPos = new Vector3(chunkPos.X * Constants.ChunkSize + 0x0, chunkPos.Y * Constants.ChunkSize + y, chunkPos.Z * Constants.ChunkSize + z);
                    toAdd.Add(new GraphicalBlock(blockPos, BlockSides.Left, chunk.Blocks[left].Material));
                }
                else if (!chunk.HasBlock(left) && leftChunk.HasBlock(right))
                {
                    var blockPos = new Vector3(leftPos.X * Constants.ChunkSize + 0xF, leftPos.Y * Constants.ChunkSize + y, leftPos.Z * Constants.ChunkSize + z);
                    toAdd.Add(new GraphicalBlock(blockPos, BlockSides.Right, leftChunk.Blocks[right].Material));
                }
            }
        }

        /// <summary>
        /// Checks edges in the relative right chunk
        /// </summary>
        private void CheckRightChunk(Point3 chunkPos, DataChunk chunk, List<GraphicalBlock> toAdd)
        {
            Point3 rightPos = chunkPos;
            rightPos.X += 1;
            DataChunk rightChunk;
            if (!Chunks.TryGetValue(rightPos, out rightChunk) || rightChunk == null) return;

            for (int i = 0; i <= 0xFF; i++)
            {
                int
                    right = (i * 0x10) + 0xF,
                    left = i * 0x10;
                byte
                    z = (byte)((i & 0x00F)),
                    y = (byte)((i & 0x0F0) / 0x010);

                if (chunk.HasBlock(right) && !rightChunk.HasBlock(left))
                {
                    var blockPos = new Vector3(chunkPos.X * Constants.ChunkSize + 0xF, chunkPos.Y * Constants.ChunkSize + y, chunkPos.Z * Constants.ChunkSize + z);
                    toAdd.Add(new GraphicalBlock(blockPos, BlockSides.Right, chunk.Blocks[right].Material));
                }
                else if (!chunk.HasBlock(right) && rightChunk.HasBlock(left))
                {
                    var blockPos = new Vector3(rightPos.X * Constants.ChunkSize + 0x0, rightPos.Y * Constants.ChunkSize + y, rightPos.Z * Constants.ChunkSize + z);
                    toAdd.Add(new GraphicalBlock(blockPos, BlockSides.Left, rightChunk.Blocks[left].Material));
                }
            }
        }

        /// <summary>
        /// Checks edges in the relative front chunk
        /// </summary>
        private void CheckFrontChunk(Point3 chunkPos, DataChunk chunk, List<GraphicalBlock> toAdd)
        {
            Point3 frontPos = chunkPos;
            frontPos.Z += 1;
            DataChunk frontChunk;
            if (!Chunks.TryGetValue(frontPos, out frontChunk) || frontChunk == null) return;

            for (int i = 0; i <= 0xFF; i++)
            {
                byte x = (byte)((i & 0x00F));
                byte y = (byte)((i & 0x0F0) / 0x010);

                int front = x + 0x0F0 + (y * 0x100);
                int back = x + (y * 0x100);

                if (chunk.HasBlock(front) && !frontChunk.HasBlock(back))
                {
                    var blockPos = new Vector3(chunkPos.X * Constants.ChunkSize + x, chunkPos.Y * Constants.ChunkSize + y, chunkPos.Z * Constants.ChunkSize + 0xF);
                    toAdd.Add(new GraphicalBlock(blockPos, BlockSides.Front, chunk.Blocks[front].Material));
                }
                else if (!chunk.HasBlock(front) && frontChunk.HasBlock(back))
                {
                    var blockPos = new Vector3(frontPos.X * Constants.ChunkSize + x, frontPos.Y * Constants.ChunkSize + y, frontPos.Z * Constants.ChunkSize + 0x0);
                    toAdd.Add(new GraphicalBlock(blockPos, BlockSides.Back, frontChunk.Blocks[back].Material));
                }
            }
        }

        /// <summary>
        /// Checks edges in the relative back chunk
        /// </summary>
        private void CheckBackChunk(Point3 chunkPos, DataChunk chunk, List<GraphicalBlock> toAdd)
        {
            Point3 backPos = chunkPos;
            backPos.Z -= 1;
            DataChunk backChunk;
            if (!Chunks.TryGetValue(backPos, out backChunk) || backChunk == null) return;

            for (int i = 0; i <= 0xFF; i++)
            {
                byte x = (byte)((i & 0x00F));
                byte y = (byte)((i & 0x0F0) / 0x010);

                int front = x + 0x0F0 + (y * 0x100);
                int back = x + (y * 0x100);

                if (chunk.HasBlock(back) && !backChunk.HasBlock(front)) 
                {
                    var blockPos = new Vector3(chunkPos.X * Constants.ChunkSize + x, chunkPos.Y * Constants.ChunkSize + y, chunkPos.Z * Constants.ChunkSize + 0x0);
                    toAdd.Add(new GraphicalBlock(blockPos, BlockSides.Back, chunk.Blocks[back].Material));
                }
                else if (!chunk.HasBlock(back) && backChunk.HasBlock(front))
                {
                    var blockPos = new Vector3(backPos.X * Constants.ChunkSize + x, backPos.Y * Constants.ChunkSize + y, backPos.Z * Constants.ChunkSize + 0xF);
                    toAdd.Add(new GraphicalBlock(blockPos, BlockSides.Front, backChunk.Blocks[front].Material));
                }
            }
        }
        
        /// <summary>
        /// Checks edges in the relative top chunk
        /// </summary>
        private void CheckTopChunk(Point3 chunkPos, DataChunk chunk, List<GraphicalBlock> toAdd)
        {
            Point3 topPos = chunkPos;
            topPos.Y += 1;
            DataChunk topChunk;
            if (!Chunks.TryGetValue(topPos, out topChunk) || topChunk == null) return;

            for (int i = 0; i <= 0xFF; i++)
            {
                byte x = (byte)((i & 0x00F));
                byte z = (byte)((i & 0x0F0) / 0x010);

                int top = x + z * 0x10 + 0xF00;
                int bottom = x + z * 0x10;

                if (chunk.HasBlock(top) && !topChunk.HasBlock(bottom))
                {
                    var blockPos = new Vector3(chunkPos.X * Constants.ChunkSize + x, chunkPos.Y * Constants.ChunkSize + 0xF, chunkPos.Z * Constants.ChunkSize + z);
                    toAdd.Add(new GraphicalBlock(blockPos, BlockSides.Top, chunk.Blocks[top].Material));
                }
                else if (!chunk.HasBlock(top) && topChunk.HasBlock(bottom))
                {
                    var blockPos = new Vector3(topPos.X * Constants.ChunkSize + x, topPos.Y * Constants.ChunkSize + 0x0, topPos.Z * Constants.ChunkSize + z);
                    toAdd.Add(new GraphicalBlock(blockPos, BlockSides.Bottom, topChunk.Blocks[bottom].Material));
                }
            }
        }

        /// <summary>
        /// Checks edges in the relative bottom chunk
        /// </summary>
        private void CheckBottomChunk(Point3 chunkPos, DataChunk chunk, List<GraphicalBlock> toAdd)
        {
            Point3 bottomPos = chunkPos;
            bottomPos.Y -= 1;
            DataChunk bottomChunk;
            if (!Chunks.TryGetValue(bottomPos, out bottomChunk) || bottomChunk == null) return;

            for (int i = 0; i <= 0xFF; i++)
            {
                byte x = (byte)((i & 0x00F));
                byte z = (byte)((i & 0x0F0) / 0x010);

                int top = x + z * 0x10 + 0xF00;
                int bottom = x + z * 0x10;

                if (chunk.HasBlock(bottom) && !bottomChunk.HasBlock(top))
                {
                    var blockPos = new Vector3(chunkPos.X * Constants.ChunkSize + x, chunkPos.Y * Constants.ChunkSize + 0x0, chunkPos.Z * Constants.ChunkSize + z);
                    toAdd.Add(new GraphicalBlock(blockPos, BlockSides.Bottom, chunk.Blocks[bottom].Material));
                }
                else if (!chunk.HasBlock(bottom) && bottomChunk.HasBlock(top))
                {
                    var blockPos = new Vector3(bottomPos.X * Constants.ChunkSize + x, bottomPos.Y * Constants.ChunkSize + 0xF, bottomPos.Z * Constants.ChunkSize + z);
                    toAdd.Add(new GraphicalBlock(blockPos, BlockSides.Top, bottomChunk.Blocks[top].Material));
                }
            }
        }
        #endregion
    }
}
