using ParadoxCraft.Blocks;
using ParadoxCraft.Helpers;
using SiliconStudio.Core.Mathematics;
using SiliconStudio.Paradox.Effects;
using SiliconStudio.Paradox.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Buffer = SiliconStudio.Paradox.Graphics.Buffer;

namespace ParadoxCraft.Terrain
{
    public class TerrainMesh
    {
        /// <summary>
        /// Max number of blocks the mesh can contain
        /// </summary>
        public const int MaxBlockCount = 2 * 0x1000;

        /// <summary>
        /// Vertex buffer
        /// </summary>
        private Buffer TerrainVertexBuffer { get; set; }

        /// <summary>
        /// Index buffer
        /// </summary>
        private Buffer TerrainIndexBuffer { get; set; }

        /// <summary>
        /// GraphicsDevice of the current game
        /// </summary>
        private GraphicsDevice GraphicsDevice { get; set; }

        /// <summary>
        /// Block buffer
        /// </summary>
        private Dictionary<Point3, List<GraphicalBlock>> Blocks { get; set; }

        /// <summary>
        /// Numbers of blocks in the buffer
        /// </summary>
        public int BlockCount { get; private set; }

        /// <summary>
        /// Resulting Mesh for this instance
        /// </summary>
        private Mesh InnerMesh { get; set; }

        // State to check if there are any pending changes
        private bool PendingChanges = false;

        /// <summary>
        /// Locker for cross threaded block adding/removing
        /// </summary>
        private volatile object BlockLocker = new object();

        /// <summary>
        /// Creates a new instance of <see cref="TerrainMesh"/>
        /// </summary>
        public TerrainMesh(GraphicsDevice device)
        {
            GraphicsDevice = device;
            Blocks = new Dictionary<Point3, List<GraphicalBlock>>();

            int vertexBufferSize = VertexTerrain.Layout.VertexStride * 4 * MaxBlockCount * 6; //A side has 4 vertice, a block has 6 sides
            TerrainVertexBuffer = Buffer.Vertex.New(GraphicsDevice, vertexBufferSize, GraphicsResourceUsage.Dynamic);

            int indexBufferSize = sizeof(int) * 6 * MaxBlockCount * 6; //A side has 6 vertice, a block has 6 sides
            TerrainIndexBuffer = Buffer.Index.New(GraphicsDevice, indexBufferSize, GraphicsResourceUsage.Dynamic);

            InnerMesh = new Mesh { 
                Draw = new MeshDraw
                {
                    PrimitiveType = PrimitiveType.TriangleList,
                    VertexBuffers = new[] { new VertexBufferBinding(TerrainVertexBuffer, VertexTerrain.Layout, TerrainVertexBuffer.ElementCount) },
                    IndexBuffer = new IndexBufferBinding(TerrainIndexBuffer, true, TerrainIndexBuffer.ElementCount)
                }
            };
        }

        /// <summary>
        /// Adds a block to the buffer
        /// </summary>
        public void AddBlocks(Point3 chunkPos, List<GraphicalBlock> toAdd)
        {
            if (TerrainMesh.MaxBlockCount <= Blocks.Count)
                return; //Emergency overflow check, else we might end up writing corrupt memory

            lock (BlockLocker)
            {
                List<GraphicalBlock> blocks;
                if (!Blocks.TryGetValue(chunkPos, out blocks))
                    Blocks.Add(chunkPos, toAdd);
                else
                    blocks.AddRange(toAdd);
            }
            BlockCount = Blocks.Count;
            PendingChanges = true;
        }

        /// <summary>
        /// Purges a chunk when inside the current buffer
        /// </summary>
        public IEnumerable<Point3> TryPurgeChunks(Point3<double> position, int radius)
        {
            List<Point3> AllChunks;
            lock (BlockLocker)
                AllChunks = Blocks.Keys.ToList();

            // TODO: check y
            foreach (var chunk in AllChunks.Where(pair =>
            {
                var height = position.X - pair.X;
                var width = position.Z - pair.Z;
                var distance = Math.Sqrt((height * height) + (width * width));
                return distance > radius;
            }))
            {
                Blocks.Remove(chunk);
                yield return chunk;
            }

            if (AllChunks.Count > 0)
            {
                PendingChanges = true;
                BlockCount = Blocks.Count;
            }
            AllChunks.Clear();
        }

        /// <summary>
        /// Creates the indices and vertices
        /// </summary>
        public void Build()
        {
            if (!PendingChanges) return;
            int verticeCount;
            lock (BlockLocker)
                verticeCount = GenerateVertices();
            InnerMesh.Draw.DrawCount = GenerateIndices(verticeCount);

            PendingChanges = false;
        }

        /// <summary>
        /// Creates the indices
        /// </summary>
        /// <returns>Amount of indices to draw</returns>
        private unsafe int GenerateIndices(int verticeCount)
        {
            MappedResource indexMap = GraphicsDevice.MapSubresource(TerrainIndexBuffer, 0, MapMode.WriteDiscard);
            var indexBuffer = (int*)indexMap.DataBox.DataPointer;

            int index = 0;
            for (int i = 0; i < verticeCount / 4; i ++ )
            {
                indexBuffer[index++] = i * 4 + 0;
                indexBuffer[index++] = i * 4 + 1;
                indexBuffer[index++] = i * 4 + 2;
                indexBuffer[index++] = i * 4 + 2;
                indexBuffer[index++] = i * 4 + 3;
                indexBuffer[index++] = i * 4 + 0;
            }

            GraphicsDevice.UnmapSubresource(indexMap);

            return index;
        }

        /// <summary>
        /// Creates the vertices
        /// </summary>
        private unsafe int GenerateVertices()
        {
            MappedResource vertexMap = GraphicsDevice.MapSubresource(TerrainVertexBuffer, 0, MapMode.WriteDiscard);
            var vertexBuffer = (VertexTerrain*)vertexMap.DataBox.DataPointer;

            int index = 0;

            // Texture UV coordinates
            Half2
                textureTopLeft = new Half2(0, 1),
                textureTopRight = new Half2(1, 1),
                textureBtmLeft = new Half2(0, 0),
                textureBtmRight = new Half2(1, 0);

            // Size vector and corner positions
            Vector3
                corner1 = new Vector3(0, 1, 1),
                corner2 = new Vector3(0, 1, 0),
                corner3 = new Vector3(1, 1, 1),
                corner4 = new Vector3(1, 1, 0),
                corner5 = new Vector3(0, 0, 1),
                corner6 = new Vector3(0, 0, 0),
                corner7 = new Vector3(1, 0, 1),
                corner8 = new Vector3(1, 0, 0);


            foreach (var chunk in Blocks)
                foreach (GraphicalBlock block in chunk.Value)
                {
                    #region Top
                    if (block.HasSide(BlockSides.Top))
                    {
                        Vector3
                            cornerTopLeft = block.Position + corner1,
                            cornerBtmLeft = block.Position + corner2,
                            cornerTopRight = block.Position + corner3,
                            cornerBtmRight = block.Position + corner4;
                        Half4
                            normal = new Half4((Half)0, (Half)1, (Half)0, (Half)0);

                        vertexBuffer[index++] = new VertexTerrain(cornerTopRight, normal, textureBtmLeft, block.GetMaterialCode(BlockSides.Top));
                        vertexBuffer[index++] = new VertexTerrain(cornerTopLeft, normal, textureBtmRight, block.GetMaterialCode(BlockSides.Top));
                        vertexBuffer[index++] = new VertexTerrain(cornerBtmLeft, normal, textureTopRight, block.GetMaterialCode(BlockSides.Top));
                        vertexBuffer[index++] = new VertexTerrain(cornerBtmRight, normal, textureTopLeft, block.GetMaterialCode(BlockSides.Top));
                    }
                    #endregion

                    #region Bottom
                    if (block.HasSide(BlockSides.Bottom))
                    {
                        Vector3
                            cornerTopLeft = block.Position + corner5,
                            cornerBtmLeft = block.Position + corner6,
                            cornerTopRight = block.Position + corner7,
                            cornerBtmRight = block.Position + corner8;
                        Half4
                            normal = new Half4((Half)0, (Half)(-1), (Half)0, (Half)0);

                        vertexBuffer[index++] = new VertexTerrain(cornerBtmLeft, normal, textureTopRight, block.GetMaterialCode(BlockSides.Bottom));
                        vertexBuffer[index++] = new VertexTerrain(cornerTopLeft, normal, textureBtmRight, block.GetMaterialCode(BlockSides.Bottom));
                        vertexBuffer[index++] = new VertexTerrain(cornerTopRight, normal, textureBtmLeft, block.GetMaterialCode(BlockSides.Bottom));
                        vertexBuffer[index++] = new VertexTerrain(cornerBtmRight, normal, textureTopLeft, block.GetMaterialCode(BlockSides.Bottom));
                    }
                    #endregion

                    #region Front
                    if (block.HasSide(BlockSides.Front))
                    {
                        Vector3
                            cornerTopLeft = block.Position + corner3,
                            cornerBtmLeft = block.Position + corner7,
                            cornerTopRight = block.Position + corner1,
                            cornerBtmRight = block.Position + corner5;
                        Half4
                            normal = new Half4((Half)0, (Half)0, (Half)1, (Half)0);

                        vertexBuffer[index++] = new VertexTerrain(cornerTopRight, normal, textureBtmLeft, block.GetMaterialCode(BlockSides.Front));
                        vertexBuffer[index++] = new VertexTerrain(cornerTopLeft, normal, textureBtmRight, block.GetMaterialCode(BlockSides.Front));
                        vertexBuffer[index++] = new VertexTerrain(cornerBtmLeft, normal, textureTopRight, block.GetMaterialCode(BlockSides.Front));
                        vertexBuffer[index++] = new VertexTerrain(cornerBtmRight, normal, textureTopLeft, block.GetMaterialCode(BlockSides.Front));
                    }
                    #endregion

                    #region Back
                    if (block.HasSide(BlockSides.Back))
                    {
                        Vector3
                            cornerTopLeft = block.Position + corner4,
                            cornerBtmLeft = block.Position + corner8,
                            cornerTopRight = block.Position + corner2,
                            cornerBtmRight = block.Position + corner6;
                        Half4
                            normal = new Half4((Half)0, (Half)0, (Half)(-1), (Half)0);

                        vertexBuffer[index++] = new VertexTerrain(cornerBtmLeft, normal, textureTopRight, block.GetMaterialCode(BlockSides.Back));
                        vertexBuffer[index++] = new VertexTerrain(cornerTopLeft, normal, textureBtmRight, block.GetMaterialCode(BlockSides.Back));
                        vertexBuffer[index++] = new VertexTerrain(cornerTopRight, normal, textureBtmLeft, block.GetMaterialCode(BlockSides.Back));
                        vertexBuffer[index++] = new VertexTerrain(cornerBtmRight, normal, textureTopLeft, block.GetMaterialCode(BlockSides.Back));
                    }
                    #endregion

                    #region Left
                    if (block.HasSide(BlockSides.Left))
                    {
                        Vector3
                            cornerTopLeft = block.Position + corner1,
                            cornerBtmLeft = block.Position + corner5,
                            cornerTopRight = block.Position + corner2,
                            cornerBtmRight = block.Position + corner6;
                        Half4
                            normal = new Half4((Half)(-1), (Half)0, (Half)0, (Half)0);

                        vertexBuffer[index++] = new VertexTerrain(cornerTopRight, normal, textureBtmLeft, block.GetMaterialCode(BlockSides.Left));
                        vertexBuffer[index++] = new VertexTerrain(cornerTopLeft, normal, textureBtmRight, block.GetMaterialCode(BlockSides.Left));
                        vertexBuffer[index++] = new VertexTerrain(cornerBtmLeft, normal, textureTopRight, block.GetMaterialCode(BlockSides.Left));
                        vertexBuffer[index++] = new VertexTerrain(cornerBtmRight, normal, textureTopLeft, block.GetMaterialCode(BlockSides.Left));
                    }
                    #endregion

                    #region Right
                    if (block.HasSide(BlockSides.Right))
                    {
                        Vector3
                            cornerTopLeft = block.Position + corner3,
                            cornerBtmLeft = block.Position + corner7,
                            cornerTopRight = block.Position + corner4,
                            cornerBtmRight = block.Position + corner8;
                        Half4
                            normal = new Half4((Half)1, (Half)0, (Half)0, (Half)0);

                        vertexBuffer[index++] = new VertexTerrain(cornerBtmLeft, normal, textureTopRight, block.GetMaterialCode(BlockSides.Right));
                        vertexBuffer[index++] = new VertexTerrain(cornerTopLeft, normal, textureBtmRight, block.GetMaterialCode(BlockSides.Right));
                        vertexBuffer[index++] = new VertexTerrain(cornerTopRight, normal, textureBtmLeft, block.GetMaterialCode(BlockSides.Right));
                        vertexBuffer[index++] = new VertexTerrain(cornerBtmRight, normal, textureTopLeft, block.GetMaterialCode(BlockSides.Right));
                    }
                    #endregion
                }

            GraphicsDevice.UnmapSubresource(vertexMap);

            return index;
        }

        public static IEnumerable<TerrainMesh> New(GraphicsDevice device, int count)
        {
            for (int i = 0; i < count; i++)
                yield return new TerrainMesh(device);
        }

        /// <summary>
        /// Cast operator so we can add this instance 'as mesh' to the terrain instance
        /// </summary>
        public static implicit operator Mesh(TerrainMesh mesh)
        {
            return mesh.InnerMesh;
        }
    }
}
