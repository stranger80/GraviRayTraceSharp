using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GraviRayTraceSharp.Mappings
{
    /// <summary>
    /// Mapping of spherical coordinates of a disc onto a flat texture.
    /// </summary>
    class DiscMapping : IMapping
    {
        private double rMin;
        private double rMax;
        private int sizeX;
        private int sizeY;

        public DiscMapping(double rMin, double rMax, int sizex, int sizey)
        {
            this.rMax = rMax;
            this.rMin = rMin;
            this.sizeX = sizex;
            this.sizeY = sizey;
        }


        public void Map(double r, double theta, double phi, out int x, out int y)
        {
            if (r < rMin || r > rMax)
            {
                x = 0;
                y = sizeY;
            }

            x = (int)(phi / (2 * Math.PI) * this.sizeX) % this.sizeX;
            if (x < 0) x = sizeX + x;
            y = (int)((r - rMin) / (rMax - rMin) * this.sizeY);
            if (y > sizeY-1)
                y = sizeY-1;
        }
    }
}
