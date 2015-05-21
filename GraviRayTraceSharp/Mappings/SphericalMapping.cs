using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GraviRayTraceSharp.Mappings
{
    /// <summary>
    /// Maps flat texture onto a spherical surface expressed in spherical coordinates.
    /// </summary>
    class SphericalMapping : IMapping
    {
        public int SizeX { get; set; }
        public int SizeY { get; set; }

        public SphericalMapping(int sizex, int sizey)
        {
            this.SizeX = sizex;
            this.SizeY = sizey;
        }

        public void Map(double r, double theta, double phi, out int x, out int y)
        {
            // do mapping of texture image
            double textureScale = 1.0;

            x = (int)(((phi * textureScale) / (2 * Math.PI)) * this.SizeX) % this.SizeX;
            y = (int)((theta * textureScale / Math.PI) * this.SizeY) % this.SizeY;

            if (x < 0) x = this.SizeX + x;
            if (y < 0) y = this.SizeY + y;

        }
    }
}
