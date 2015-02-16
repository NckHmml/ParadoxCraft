using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ParadoxCraft.Helpers
{
    /// <summary>
    /// Default 3d struct for chunks positions
    /// </summary>
    public struct Point3
    {
        public int X { get; set; }
        public short Y { get; set; }
        public int Z { get; set; }
    }

    /// <summary>
    /// 3d struct
    /// </summary>
    /// <typeparam name="T">Type to be used for the positions</typeparam>
    public struct Point3<T>
    {
        public T X { get; set; }
        public T Y { get; set; }
        public T Z { get; set; }
    }

    /// <summary>
    /// 3d struct
    /// </summary>
    /// <typeparam name="T1">Type to be used for X</typeparam>
    /// <typeparam name="T2">Type to be used for Y</typeparam>
    /// <typeparam name="T3">Type to be used for Z</typeparam>
    public struct Point3<T1, T2, T3>
    {
        public T1 X { get; set; }
        public T2 Y { get; set; }
        public T3 Z { get; set; }
    }
}
