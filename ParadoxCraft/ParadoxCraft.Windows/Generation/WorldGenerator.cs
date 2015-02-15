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
        private const int SurfaceStart = 0;
        private const int SurfaceEnd = 4;
        private const int MaxMountainHight = (SurfaceEnd + 1) * 0xF;
        private const int MinSurfaceHeight = 4;

        private SeededNoise HeightRoughner { get; set; }
        private SeededNoise HeightGenerator { get; set; }
        private SeededNoise MountainRangeGenerator { get; set; }

        /// <summary>
        /// Creates a new instance of <see cref="WorldGenerator"/>
        /// </summary>
        /// <param name="seed">Seed to base the world generation on</param>
        public WorldGenerator(double seed)
        {
            HeightRoughner = new SeededNoise(seed, 20);
            HeightGenerator = new SeededNoise(seed, 100);
            MountainRangeGenerator = new SeededNoise(seed, 3000);
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
            double y, fX;
            for (ushort xz = 0; xz < 16 * 16; xz++)
            {
                x = (xz & 0x00F);
                z = (xz & 0x0F0) / 0x10;
                x += pos.X * 0x10;
                z += pos.Z * 0x10;

                y = HeightGenerator.Generate(x, z);
                // Apply the MountainRange noise
                y *= MountainRangeGenerator.Generate(x, z);
                // Roughen up the surface a little 10%
                y *= .9 + HeightRoughner.Generate(x, z) * .1;
                // Apply the max height
                y *= MaxMountainHight - MinSurfaceHeight;
                y += MinSurfaceHeight;

                // This is were we seperate the mountains from the hills
                // .5(x+4(x-.5)^3+.5)
                fX = y / (MaxMountainHight - MinSurfaceHeight);
                fX = fX + (4 * ((fX - .5) * (fX - .5) * (fX - .5)) + .5);
                fX /= 2;
                y *= fX;

                // Round it and apply chunk height
                y = Math.Round(y);
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
                chunk.Blocks[position] = new DataBlock()
                {
                    //Material = maxY - curY <= 3 ? maxY - curY == 0 ? MaterialTypes.Grass : MaterialTypes.Dirt : MaterialTypes.Stone,
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
                        //Material = maxY - curY <= 3 ? MaterialTypes.Dirt : MaterialTypes.Stone,
                        Position = (ushort)position
                    };
                }
            }
        }
    }
}
