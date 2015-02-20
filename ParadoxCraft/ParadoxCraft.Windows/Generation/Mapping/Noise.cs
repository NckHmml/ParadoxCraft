// SimplexNoise for C#
// Author: Heikki Törmälä

//This is free and unencumbered software released into the public domain.

//Anyone is free to copy, modify, publish, use, compile, sell, or
//distribute this software, either in source code form or as a compiled
//binary, for any purpose, commercial or non-commercial, and by any
//means.

//In jurisdictions that recognize copyright laws, the author or authors
//of this software dedicate any and all copyright interest in the
//software to the public domain. We make this dedication for the benefit
//of the public at large and to the detriment of our heirs and
//successors. We intend this dedication to be an overt act of
//relinquishment in perpetuity of all present and future rights to this
//software under copyright law.

//THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
//EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
//MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT.
//IN NO EVENT SHALL THE AUTHORS BE LIABLE FOR ANY CLAIM, DAMAGES OR
//OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE,
//ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR
//OTHER DEALINGS IN THE SOFTWARE.

//For more information, please refer to <http://unlicense.org/>

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ParadoxCraft.Generation.Mapping
{
    /// <summary>
    /// Implementation of the Perlin simplex noise, an improved Perlin noise algorithm.
    /// </summary>
    /// <remarks>
    /// Based loosely on SimplexNoise1234 by Stefan Gustavson <http://staffwww.itn.liu.se/~stegu/aqsis/aqsis-newnoise/>
    /// Cleaned up and modified by Nick Hummel
    /// </remarks>
    public class Noise
    {
        #region Static variables
        const double F3 = 1.0 / 3.0;
        const double G3 = 1.0 / 6.0;
        readonly static double F4 = (Math.Sqrt(5.0) - 1.0) / 4.0;
        readonly static double G4 = (5.0 - Math.Sqrt(5.0)) / 20.0;

        private static int[][] simplex = new int[][] {
            new int[] {0,1,2,3}, new int[] {0,1,3,2}, new int[] {0,0,0,0}, new int[] {0,2,3,1}, new int[] {0,0,0,0}, new int[] {0,0,0,0}, new int[] {0,0,0,0}, new int[] {1,2,3,0},
            new int[] {0,2,1,3}, new int[] {0,0,0,0}, new int[] {0,3,1,2}, new int[] {0,3,2,1}, new int[] {0,0,0,0}, new int[] {0,0,0,0}, new int[] {0,0,0,0}, new int[] {1,3,2,0},
            new int[] {0,0,0,0}, new int[] {0,0,0,0}, new int[] {0,0,0,0}, new int[] {0,0,0,0}, new int[] {0,0,0,0}, new int[] {0,0,0,0}, new int[] {0,0,0,0}, new int[] {0,0,0,0},
            new int[] {1,2,0,3}, new int[] {0,0,0,0}, new int[] {1,3,0,2}, new int[] {0,0,0,0}, new int[] {0,0,0,0}, new int[] {0,0,0,0}, new int[] {2,3,0,1}, new int[] {2,3,1,0},
            new int[] {1,0,2,3}, new int[] {1,0,3,2}, new int[] {0,0,0,0}, new int[] {0,0,0,0}, new int[] {0,0,0,0}, new int[] {2,0,3,1}, new int[] {0,0,0,0}, new int[] {2,1,3,0},
            new int[] {0,0,0,0}, new int[] {0,0,0,0}, new int[] {0,0,0,0}, new int[] {0,0,0,0}, new int[] {0,0,0,0}, new int[] {0,0,0,0}, new int[] {0,0,0,0}, new int[] {0,0,0,0},
            new int[] {2,0,1,3}, new int[] {0,0,0,0}, new int[] {0,0,0,0}, new int[] {0,0,0,0}, new int[] {3,0,1,2}, new int[] {3,0,2,1}, new int[] {0,0,0,0}, new int[] {3,1,2,0},
            new int[] {2,1,0,3}, new int[] {0,0,0,0}, new int[] {0,0,0,0}, new int[] {0,0,0,0}, new int[] {3,1,0,2}, new int[] {0,0,0,0}, new int[] {3,2,0,1}, new int[] {3,2,1,0}
        };

        public static byte[] perm = new byte[512] { 151,160,137,91,90,15,
              131,13,201,95,96,53,194,233,7,225,140,36,103,30,69,142,8,99,37,240,21,10,23,
              190, 6,148,247,120,234,75,0,26,197,62,94,252,219,203,117,35,11,32,57,177,33,
              88,237,149,56,87,174,20,125,136,171,168, 68,175,74,165,71,134,139,48,27,166,
              77,146,158,231,83,111,229,122,60,211,133,230,220,105,92,41,55,46,245,40,244,
              102,143,54, 65,25,63,161, 1,216,80,73,209,76,132,187,208, 89,18,169,200,196,
              135,130,116,188,159,86,164,100,109,198,173,186, 3,64,52,217,226,250,124,123,
              5,202,38,147,118,126,255,82,85,212,207,206,59,227,47,16,58,17,182,189,28,42,
              223,183,170,213,119,248,152, 2,44,154,163, 70,221,153,101,155,167, 43,172,9,
              129,22,39,253, 19,98,108,110,79,113,224,232,178,185, 112,104,218,246,97,228,
              251,34,242,193,238,210,144,12,191,179,162,241, 81,51,145,235,249,14,239,107,
              49,192,214, 31,181,199,106,157,184, 84,204,176,115,121,50,45,127, 4,150,254,
              138,236,205,93,222,114,67,29,24,72,243,141,128,195,78,66,215,61,156,180,
              151,160,137,91,90,15,
              131,13,201,95,96,53,194,233,7,225,140,36,103,30,69,142,8,99,37,240,21,10,23,
              190, 6,148,247,120,234,75,0,26,197,62,94,252,219,203,117,35,11,32,57,177,33,
              88,237,149,56,87,174,20,125,136,171,168, 68,175,74,165,71,134,139,48,27,166,
              77,146,158,231,83,111,229,122,60,211,133,230,220,105,92,41,55,46,245,40,244,
              102,143,54, 65,25,63,161, 1,216,80,73,209,76,132,187,208, 89,18,169,200,196,
              135,130,116,188,159,86,164,100,109,198,173,186, 3,64,52,217,226,250,124,123,
              5,202,38,147,118,126,255,82,85,212,207,206,59,227,47,16,58,17,182,189,28,42,
              223,183,170,213,119,248,152, 2,44,154,163, 70,221,153,101,155,167, 43,172,9,
              129,22,39,253, 19,98,108,110,79,113,224,232,178,185, 112,104,218,246,97,228,
              251,34,242,193,238,210,144,12,191,179,162,241, 81,51,145,235,249,14,239,107,
              49,192,214, 31,181,199,106,157,184, 84,204,176,115,121,50,45,127, 4,150,254,
              138,236,205,93,222,114,67,29,24,72,243,141,128,195,78,66,215,61,156,180 
        };
        #endregion

        #region Helper functions
        private static int FastFloor(double x)
        {
            var ret = (int)x;
            return (x > 0) ? ret : ret - 1;
        }

        private static double grad(int hash, double x, double y, double z)
        {
            int h = hash & 15;     // Convert low 4 bits of hash code into 12 simple
            double u = h < 8 ? x : y; // gradient directions, and compute dot product.
            double v = h < 4 ? y : h == 12 || h == 14 ? x : z; // Fix repeats at h = 12 to 15
            return ((h & 1) != 0 ? -u : u) + ((h & 2) != 0 ? -v : v);
        }

        private static double grad(int hash, double x, double y, double z, double t)
        {
            int h = hash & 31;      // Convert low 5 bits of hash code into 32 simple
            double u = h < 24 ? x : y; // gradient directions, and compute dot product.
            double v = h < 16 ? y : z;
            double w = h < 8 ? z : t;
            return ((h & 1) != 0 ? -u : u) + ((h & 2) != 0 ? -v : v) + ((h & 4) != 0 ? -w : w);
        }
        #endregion

        /// <summary>
        /// Generates a 3D SimplexNoise
        /// </summary>
        /// <param name="x">X</param>
        /// <param name="y">Y</param>
        /// <param name="z">Z</param>
        /// <returns>Noise value ranging -1 to 1</returns>
        public static double Generate(double x, double y, double z)
        {
            double
                n0, n1, n2, n3,
                s = (x + y + z) * F3;
            int
                i = FastFloor(x + s),
                j = FastFloor(y + s),
                k = FastFloor(z + s);

            double
                t = (i + j + k) * G3,
                X0 = i - t,
                Y0 = j - t,
                Z0 = k - t,
                x0 = x - X0,
                y0 = y - Y0,
                z0 = z - Z0;

            int
                i1, j1, k1,
                i2, j2, k2;

            if (x0 >= y0)
            {
                if (y0 >= z0)
                {
                    i1 = 1;
                    j1 = 0;
                    k1 = 0;
                    i2 = 1;
                    j2 = 1;
                    k2 = 0;
                }
                else if (x0 >= z0)
                {
                    i1 = 1;
                    j1 = 0;
                    k1 = 0;
                    i2 = 1;
                    j2 = 0;
                    k2 = 1;
                }
                else
                {
                    i1 = 0;
                    j1 = 0;
                    k1 = 1;
                    i2 = 1;
                    j2 = 0;
                    k2 = 1;
                }
            }
            else
            {
                if (y0 < z0)
                {
                    i1 = 0;
                    j1 = 0;
                    k1 = 1;
                    i2 = 0;
                    j2 = 1;
                    k2 = 1;
                }
                else if (x0 < z0)
                {
                    i1 = 0;
                    j1 = 1;
                    k1 = 0;
                    i2 = 0;
                    j2 = 1;
                    k2 = 1;
                }
                else
                {
                    i1 = 0;
                    j1 = 1;
                    k1 = 0;
                    i2 = 1;
                    j2 = 1;
                    k2 = 0;
                }
            }

            double
                x1 = x0 - i1 + G3,
                y1 = y0 - j1 + G3,
                z1 = z0 - k1 + G3,
                x2 = x0 - i2 + 2.0 * G3,
                y2 = y0 - j2 + 2.0 * G3,
                z2 = z0 - k2 + 2.0 * G3,
                x3 = x0 - 1.0 + 3.0 * G3,
                y3 = y0 - 1.0 + 3.0 * G3,
                z3 = z0 - 1.0 + 3.0 * G3;

            i &= 0xFF;
            j &= 0xFF;
            k &= 0xFF;

            double
                t0 = 0.6 - x0 * x0 - y0 * y0 - z0 * z0,
                t1 = 0.6 - x1 * x1 - y1 * y1 - z1 * z1,
                t2 = 0.6 - x2 * x2 - y2 * y2 - z2 * z2,
                t3 = 0.6 - x3 * x3 - y3 * y3 - z3 * z3;

            if (t0 < 0.0) n0 = 0.0;
            else
            {
                t0 *= t0;
                n0 = t0 * t0 * grad(perm[i + perm[j + perm[k]]], x0, y0, z0);
            }

            if (t1 < 0.0) n1 = 0.0;
            else
            {
                t1 *= t1;
                n1 = t1 * t1 * grad(perm[i + i1 + perm[j + j1 + perm[k + k1]]], x1, y1, z1);
            }

            if (t2 < 0.0) n2 = 0.0;
            else
            {
                t2 *= t2;
                n2 = t2 * t2 * grad(perm[i + i2 + perm[j + j2 + perm[k + k2]]], x2, y2, z2);
            }

            if (t3 < 0.0) n3 = 0.0;
            else
            {
                t3 *= t3;
                n3 = t3 * t3 * grad(perm[i + 1 + perm[j + 1 + perm[k + 1]]], x3, y3, z3);
            }

            return 32.0 * (n0 + n1 + n2 + n3);
        }

        /// <summary>
        /// Generates a 4D SimplexNoise
        /// </summary>
        /// <param name="x">X</param>
        /// <param name="y">Y</param>
        /// <param name="z">Z</param>
        /// <param name="w">W</param>
        /// <returns>Noise value ranging -1 to 1</returns>
        public static double Generate(double x, double y, double z, double w)
        {
            double
                n0, n1, n2, n3, n4,
                s = (x + y + z + w) * F4;
            int
                i = FastFloor(x + s),
                j = FastFloor(y + s),
                k = FastFloor(z + s),
                l = FastFloor(w + s);

            double
                t = (i + j + k + l) * G4,
                X0 = i - t,
                Y0 = j - t,
                Z0 = k - t,
                W0 = l - t,
                x0 = x - X0,
                y0 = y - Y0,
                z0 = z - Z0,
                w0 = w - W0;

            int
                c1 = (x0 > y0) ? 32 : 0,
                c2 = (x0 > z0) ? 16 : 0,
                c3 = (y0 > z0) ? 8 : 0,
                c4 = (x0 > w0) ? 4 : 0,
                c5 = (y0 > w0) ? 2 : 0,
                c6 = (z0 > w0) ? 1 : 0,
                c = c1 + c2 + c3 + c4 + c5 + c6,
                i1 = simplex[c][0] >= 3 ? 1 : 0,
                j1 = simplex[c][1] >= 3 ? 1 : 0,
                k1 = simplex[c][2] >= 3 ? 1 : 0,
                l1 = simplex[c][3] >= 3 ? 1 : 0,
                i2 = simplex[c][0] >= 2 ? 1 : 0,
                j2 = simplex[c][1] >= 2 ? 1 : 0,
                k2 = simplex[c][2] >= 2 ? 1 : 0,
                l2 = simplex[c][3] >= 2 ? 1 : 0,
                i3 = simplex[c][0] >= 1 ? 1 : 0,
                j3 = simplex[c][1] >= 1 ? 1 : 0,
                k3 = simplex[c][2] >= 1 ? 1 : 0,
                l3 = simplex[c][3] >= 1 ? 1 : 0;

            double
                x1 = x0 - i1 + G4,
                y1 = y0 - j1 + G4,
                z1 = z0 - k1 + G4,
                w1 = w0 - l1 + G4,
                x2 = x0 - i2 + 2.0 * G4,
                y2 = y0 - j2 + 2.0 * G4,
                z2 = z0 - k2 + 2.0 * G4,
                w2 = w0 - l2 + 2.0 * G4,
                x3 = x0 - i3 + 3.0 * G4,
                y3 = y0 - j3 + 3.0 * G4,
                z3 = z0 - k3 + 3.0 * G4,
                w3 = w0 - l3 + 3.0 * G4,
                x4 = x0 - 1.0 + 4.0 * G4,
                y4 = y0 - 1.0 + 4.0 * G4,
                z4 = z0 - 1.0 + 4.0 * G4,
                w4 = w0 - 1.0 + 4.0 * G4;

            i &= 0xFF;
            j &= 0xFF;
            k &= 0xFF;
            l &= 0xFF;

            int
                gi0 = perm[i + perm[j + perm[k + perm[l]]]] % 32,
                gi1 = perm[i + i1 + perm[j + j1 + perm[k + k1 + perm[l + l1]]]] % 32,
                gi2 = perm[i + i2 + perm[j + j2 + perm[k + k2 + perm[l + l2]]]] % 32,
                gi3 = perm[i + i3 + perm[j + j3 + perm[k + k3 + perm[l + l3]]]] % 32,
                gi4 = perm[i + 1 + perm[j + 1 + perm[k + 1 + perm[l + 1]]]] % 32;

            double
                t0 = 0.6 - x0 * x0 - y0 * y0 - z0 * z0 - w0 * w0,
                t1 = 0.6 - x1 * x1 - y1 * y1 - z1 * z1 - w1 * w1,
                t2 = 0.6 - x2 * x2 - y2 * y2 - z2 * z2 - w2 * w2,
                t3 = 0.6 - x3 * x3 - y3 * y3 - z3 * z3 - w3 * w3,
                t4 = 0.6 - x4 * x4 - y4 * y4 - z4 * z4 - w4 * w4;

            if (t0 < 0) n0 = 0.0;
            else
            {
                t0 *= t0;
                n0 = t0 * t0 * grad(gi0, x0, y0, z0, w0);
            }
            if (t1 < 0) n1 = 0.0;
            else
            {
                t1 *= t1;
                n1 = t1 * t1 * grad(gi1, x1, y1, z1, w1);
            }
            if (t2 < 0) n2 = 0.0;
            else
            {
                t2 *= t2;
                n2 = t2 * t2 * grad(gi2, x2, y2, z2, w2);
            }
            if (t3 < 0) n3 = 0.0;
            else
            {
                t3 *= t3;
                n3 = t3 * t3 * grad(gi3, x3, y3, z3, w3);
            }
            if (t4 < 0) n4 = 0.0;
            else
            {
                t4 *= t4;
                n4 = t4 * t4 * grad(gi4, x4, y4, z4, w4);
            }

            return 27.0 * (n0 + n1 + n2 + n3 + n4);
        }
    }
}
