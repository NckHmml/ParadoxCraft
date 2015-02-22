using ParadoxCraft.Blocks;
using ParadoxCraft.Blocks.Chunks;
using ParadoxCraft.Generation.Mapping;
using ParadoxCraft.Helpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ParadoxCraft.Generation
{
    public class WorldGenerator
    {
        private const int SurfaceStart = -1;
        private const int SurfaceEnd = 4;
        private const int MaxMountainHight = (SurfaceEnd + 1) * 0xF;
        private const int MaxOceanDepth = SurfaceStart * 0xF;
        private const int MinSurfaceHeight = 4;

        /// <summary>
        /// The roughner prevents terrain from getting to symmetrical
        /// </summary>
        private SeededNoise HeightRoughner { get; set; }

        /// <summary>
        /// The main generator responsible for generating the height of mountains
        /// </summary>
        private SeededNoise HeightGenerator { get; set; }

        /// <summary>
        /// Generator used for generating the max height of mountains, beaches, and depth of oceans 
        /// </summary>
        private SeededNoise TerrainRangeGenerator { get; set; }

        private SeededNoise TreeGenerator { get; set; }

        /// <summary>
        /// Creates a new instance of <see cref="WorldGenerator"/>
        /// </summary>
        /// <param name="seed">Seed to base the world generation on</param>
        public WorldGenerator(double seed)
        {
            HeightRoughner = new SeededNoise(seed, 20);
            HeightGenerator = new SeededNoise(seed, 100);
            TerrainRangeGenerator = new SeededNoise(seed, 1000);

            TreeGenerator = new SeededNoise(seed, 1);
        }

        /// <summary>
        /// Fills a chunk with block data
        /// </summary>
        public void FillChunk(ref DataChunk chunk, Point3 pos)
        {
            if (pos.Y >= SurfaceStart && pos.Y <= SurfaceEnd)
                GenerateSurface(ref chunk, pos);
        }

        private void GenerateSurface(ref DataChunk chunk, Point3 pos)
        {
            int x, z, position, maxY, curY;
            double y, fX, terrainRange, treeChance;
            for (ushort xz = 0; xz < 16 * 16; xz++)
            {
                x = (xz & 0x00F);
                z = (xz & 0x0F0) / 0x10;
                x += pos.X * 0x10;
                z += pos.Z * 0x10;

                terrainRange = TerrainRangeGenerator.Generate(x, z);
                // Roughen up the surface a little 10%
                y = .9 + HeightRoughner.Generate(x, z) * .1;
                if (terrainRange >= .5)
                {
                    #region Above sealevel
                    y *= HeightGenerator.Generate(x, z);

                    // Apply the TerrainRange noise
                    y *= (terrainRange - .5) * 2;

                    // Apply the max height
                    y *= MaxMountainHight - MinSurfaceHeight;

                    // This is were we seperate the mountains from the hills
                    // .5(x+4(x-.5)^3+.5)
                    fX = y / (MaxMountainHight - MinSurfaceHeight);
                    fX = fX + (4 * ((fX - .5) * (fX - .5) * (fX - .5)) + .5);
                    fX /= 2;
                    y *= fX;

                    // Apply the min height
                    y += MinSurfaceHeight;

                    // Round it and apply chunk height
                    y = Math.Ceiling(y);
                    maxY = (int)y;
                    y -= pos.Y * 0x10;

                    // Ceiling check
                    if (y > 0xF)
                        y = 0xF;
                    // Air check
                    if (y < 0)
                        continue;

                    y *= 0x100;
                    position = xz + (int)y;

                    // Surface block
                    curY = (int)(pos.Y * 0x10 + y / 0x100);

                    if (maxY - curY == 0)
                    {
                        if (SurfaceEnd == pos.Y)
                        {
                            treeChance = 0;
                        }
                        else
                        {
                            treeChance = TreeGenerator.Generate(x, z);
                            treeChance *= Math.Round((terrainRange - .5) * 40 > 1 ? 1 : (terrainRange - .5) * 40, 1);
                            treeChance = Math.Round(treeChance, 1);
                        }
                        chunk.Blocks[position] = new DataBlock()
                        {
                            Material = treeChance < 1 ? MaterialType.Grass : MaterialType.Stone,
                            Position = (ushort)position
                        };
                    }
                    else
                    {
                        chunk.Blocks[position] = new DataBlock()
                        {
                            Material = maxY - curY <= 3 ? MaterialType.Dirt : MaterialType.Stone,
                            Position = (ushort)position
                        };
                    }

                    // Non-surface blocks
                    while (y >= 0x100)
                    {
                        y -= 0x100;
                        curY = (int)(pos.Y * 0x10 + y / 0x100);
                        position = xz + (int)y;
                        chunk.Blocks[position] = new DataBlock()
                        {
                            Material = maxY - curY <= 3 ? MaterialType.Dirt : MaterialType.Stone,
                            Position = (ushort)position
                        };
                    }
                    #endregion
                }
                else if (terrainRange >= .475)
                {
                    #region Beach
                    // Apply the TerrainRange noise
                    y *= 1 - (.5 - terrainRange) * 40;

                    // Apply the min height (which is basically the max height for beaches)
                    y *= MinSurfaceHeight + 1;

                    // Round it and apply chunk height
                    y = Math.Ceiling(y);
                    maxY = (int)y;
                    y -= pos.Y * 0x10;

                    // Ceiling check
                    if (y > 0xF)
                        y = 0xF;
                    // Air check
                    if (y < 0)
                        continue;

                    y *= 0x100;
                    // Surface block
                    position = xz + (int)y;
                    chunk.Blocks[position] = new DataBlock()
                    {
                        Material = MaterialType.Sand,
                        Position = (ushort)position
                    };
                    // Non-surface blocks
                    while (y >= 0x100)
                    {
                        y -= 0x100;
                        curY = (int)(pos.Y * 0x10 + y / 0x100);
                        position = xz + (int)y;
                        chunk.Blocks[position] = new DataBlock()
                        {
                            Material = MaterialType.Sand,
                            Position = (ushort)position
                        };
                    }
                    #endregion
                }
                else // terrainRange < .475
                {
                    #region Ocean
                    // TODO: Water

                    // Apply the TerrainRange noise
                    y *= (.475 - terrainRange) * (1 / .475);

                    // Apply the max depth
                    y *= MaxOceanDepth;

                    // Round it and apply chunk height
                    y = Math.Ceiling(y);
                    maxY = (int)y;
                    y -= pos.Y * 0x10;

                    // Ceiling check
                    if (y > 0xF)
                        y = 0xF;
                    // Air check
                    if (y < 0)
                        continue;

                    y *= 0x100;
                    // Surface block
                    position = xz + (int)y;
                    chunk.Blocks[position] = new DataBlock()
                    {
                        Material = MaterialType.Stone,
                        Position = (ushort)position
                    };
                    // Non-surface blocks
                    while (y >= 0x100)
                    {
                        y -= 0x100;
                        curY = (int)(pos.Y * 0x10 + y / 0x100);
                        position = xz + (int)y;
                        chunk.Blocks[position] = new DataBlock()
                        {
                            Material = MaterialType.Stone,
                            Position = (ushort)position
                        };
                    }
                    #endregion
                }
            }
        }
    }
}
