using SiliconStudio.Paradox.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ParadoxCraft.Helpers
{
    public struct PrimitiveRender
    {
        public SimpleEffect DrawEffect { get; set; }
        public GeometricPrimitive[] Primitives { get; set; }
    }
}
