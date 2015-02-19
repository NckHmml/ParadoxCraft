using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ParadoxCraft
{
    public class Constants
    {
        /// <summary>
        /// 90 degrees radial
        /// </summary>
        public const float Degrees90 = (float)Math.PI / 2;

        /// <summary>
        /// Default size of a chunk
        /// </summary>
        public const int ChunkSize = 16;

        /// <summary>
        /// Chunk drawing radius
        /// </summary>
        public const byte DrawRadius = 15;

        /// <summary>
        /// Time of a day/night cycle in seconds
        /// </summary>
        public const int DayNightCycle = 1 * 60;
    }
}
