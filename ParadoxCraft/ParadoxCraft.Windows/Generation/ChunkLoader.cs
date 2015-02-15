using ParadoxCraft.Blocks;
using ParadoxCraft.Blocks.Chunks;
using ParadoxCraft.Helpers;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Web.Script.Serialization;

namespace ParadoxCraft.Generation
{
    /// <summary>
    /// Helper class for either loading or creating chunks 
    /// </summary>
    public static class ChunkLoader
    {
        /// <summary>
        /// Json serializer
        /// </summary>
        private static JavaScriptSerializer serializer = new JavaScriptSerializer();

        /// <summary>
        /// Process callback to the game
        /// </summary>
        public static Action<Point3, DataChunk> ProcessChunk;

        /// <summary>
        /// Initializes the <see cref="ChunkLoader"/> instance
        /// </summary>
        public static void Initialize()
        {
            Directory.CreateDirectory("data/world/");
        }

        /// <summary>
        /// Request a chunk to be queued for loading
        /// </summary>
        /// <param name="position">Position of requested chunk</param>
        public static void RequestChunk(Point3 position)
        {
            ThreadPool.QueueUserWorkItem(new WaitCallback(LoadChunk), position);
        }

        /// <summary>
        /// Loads a chunk from the file system
        /// </summary>
        /// <remarks>
        /// Creates the chunk if not found on the file system
        /// </remarks>
        /// <param name="state">Position of requested chunk</param>
        private static void LoadChunk(object state)
        {
            var position = (state as Point3?).Value;
            string jsonPath = String.Format("data/world/{0:X8}_{1:X8}_{2:X4}.json.gz", position.X, position.Z, position.Y);
            if (File.Exists(jsonPath))
            {
                // Chunk exists, load it
                string jsonData;
                using (FileStream stream = File.OpenRead(jsonPath))
                using (GZipStream compressor = new GZipStream(stream, CompressionMode.Decompress))
                using (MemoryStream decompressed = new MemoryStream())
                {
                    compressor.CopyTo(decompressed);
                    jsonData = Encoding.UTF8.GetString(decompressed.ToArray());
                }
                DataChunk chunk = serializer.Deserialize<DataChunk>(jsonData);
                ProcessChunk(position, chunk);
            }
            else
            {
                // Chunk does not exist, create it
                DataChunk chunk = new DataChunk();
                for (ushort i = 0xEF; i > 0; i--)
                {
                    chunk.Blocks[i] = new DataBlock()
                    {
                        Position = i
                    };
                }
                ProcessChunk(position, chunk);
            }
        }
    }
}
