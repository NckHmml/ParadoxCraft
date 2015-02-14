using SiliconStudio.Core.Mathematics;
using SiliconStudio.Paradox.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ParadoxCraft.Terrain
{
    public struct VertexTerrain : IEquatable<VertexTerrain>, IVertex
    {
        /// <summary>
        /// Initializes a new <see cref="VertexTerrain"/> instance.
        /// </summary>
        /// <param name="position">The position of this vertex.</param>
        /// <param name="normal">The vertex normal.</param>
        /// <param name="textureCoordinate">UV texture coordinates.</param>
        /// <param name="material">Material byte.</param>
        public VertexTerrain(Vector3 position, Half4 normal, Half2 textureCoordinate, uint material = 0)
            : this()
        {
            Position = position;
            Normal = normal;
            TextureCoordinate = textureCoordinate;
            Material = material;
        }

        /// <summary>
        /// XYZ position.
        /// </summary>
        public Vector3 Position;

        /// <summary>
        /// The vertex normal.
        /// </summary>
        public Half4 Normal;

        /// <summary>
        /// UV texture coordinates.
        /// </summary>
        public Half2 TextureCoordinate;

        public uint Material;
        
        /// <summary>
        /// Defines structure byte size.
        /// </summary>
        public static readonly int Size = 28;


        /// <summary>
        /// The vertex layout of this struct.
        /// </summary>
        public static readonly VertexDeclaration Layout = new VertexDeclaration(
            VertexElement.Position<Vector3>(),
            VertexElement.Normal<Half4>(),
            VertexElement.TextureCoordinate<Half2>(),
            new VertexElement("MATERIAL0", PixelFormat.R32_UInt)
            );


        public bool Equals(VertexTerrain other)
        {
            return Position.Equals(other.Position) && Normal.Equals(other.Normal) && TextureCoordinate.Equals(other.TextureCoordinate) && Material.Equals(other.Material);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            return obj is VertexTerrain && Equals((VertexTerrain) obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                int hashCode = Position.GetHashCode();
                hashCode = (hashCode * 397) ^ Normal.GetHashCode();
                hashCode = (hashCode * 397) ^ TextureCoordinate.GetHashCode();
                hashCode = (hashCode * 397) ^ Material.GetHashCode();
                return hashCode;
            }
        }

        public VertexDeclaration GetLayout()
        {
            return Layout;
        }

        public void FlipWinding()
        {
            TextureCoordinate.X = (Half)(1 - TextureCoordinate.X);
        }

        public static bool operator ==(VertexTerrain left, VertexTerrain right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(VertexTerrain left, VertexTerrain right)
        {
            return !left.Equals(right);
        }

        public override string ToString()
        {
            return string.Format("Position: {0}, Normal: {1}, Texcoord: {2}, Material: {3}", Position, Normal, TextureCoordinate, Material);
        }
    }
}
