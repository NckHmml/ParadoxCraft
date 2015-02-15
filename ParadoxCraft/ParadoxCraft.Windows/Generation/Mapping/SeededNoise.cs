using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ParadoxCraft.Generation.Mapping
{
    /// <summary>
    /// SimplexNoise implementation using one coordinate as seed
    /// </summary>
    public class SeededNoise
    {
        private double Seed { get; set; }
        private double Factor { get; set; }

        /// <summary>
        /// Creates a new instance of <see cref="SeededNoise"/>
        /// </summary>
        public SeededNoise(double seed, double factor)
        {
            Seed = seed;
            Factor = factor;
        }

        /// <summary>
        /// Generates a 2D SimplexNoise
        /// </summary>
        /// <param name="x">X</param>
        /// <param name="y">Y</param>
        /// <returns>Noise value ranging 0 to 1</returns>
        public double Generate(int x, int y)
        {
            double noise = Noise.Generate(x / Factor, y / Factor, Seed);
            noise += 1;
            noise *= 0.5;
            return noise;
        }

        /// <summary>
        /// Generates a 3D SimplexNoise
        /// </summary>
        /// <param name="x">X</param>
        /// <param name="y">Y</param>
        /// <param name="z">Z</param>
        /// <returns>Noise value ranging 0 to 1</returns>
        public double Generate(int x, int y, int z)
        {
            double noise = Noise.Generate(x / Factor, y / Factor, z / Factor, Seed);
            noise += 1;
            noise *= 0.5;
            return noise;
        }
    }
}
