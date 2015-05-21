using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GraviRayTraceSharp.Mappings
{
    /// <summary>
    /// Interface that specifies logic required to map spherical coordinates of the scene onto flat coordinates of a texture.
    /// </summary>
    interface IMapping
    {
        /// <summary>
        /// Map 3D spherical coordinates onto flat Cartesian coordinates.
        /// </summary>
        /// <param name="r"></param>
        /// <param name="theta"></param>
        /// <param name="phi"></param>
        /// <param name="x"></param>
        /// <param name="y"></param>
        void Map(double r, double theta, double phi, out int x, out int y);
    }
}
