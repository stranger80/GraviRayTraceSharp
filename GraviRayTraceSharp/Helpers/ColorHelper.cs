using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GraviRayTraceSharp.Helpers
{
    /// <summary>
    /// Rudimentary RGB color operations.
    /// </summary>
    public class ColorHelper
    {
        /// <summary>
        /// Method to add/sum two colors.
        /// </summary>
        /// <param name="hitColor"></param>
        /// <param name="tintColor"></param>
        /// <returns></returns>
        public static Color AddColor(Color hitColor, Color tintColor)
        {
            float brightness = tintColor.GetBrightness();
            var result = Color.FromArgb(
                    (int)Cap((int)((1 - brightness) * hitColor.R) + CapMin(tintColor.R - 20, 0) * 255 / 205, 255),
                    (int)Cap((int)((1 - brightness) * hitColor.G) + CapMin(tintColor.G - 20, 0) * 255 / 205, 255),
                    (int)Cap((int)((1 - brightness) * hitColor.B) + CapMin(tintColor.B - 20, 0) * 255 / 205, 255)
                );
            return result;
        }

        /// <summary>
        /// Colour distance metric
        /// </summary>
        /// <param name="c1"></param>
        /// <param name="c2"></param>
        /// <returns></returns>
        public static double ColorDifference(Color c1, Color c2)
        {
            return Math.Sqrt(Math.Pow(c1.R - c2.R, 2) + Math.Pow(c1.G - c2.G, 2) + Math.Pow(c1.B - c2.B, 2));
        }

        private static int Cap(int x, int max)
        {
            return x > max ? max : x;
        }

        private static int CapMin(int x, int min)
        {
            return x < min ? min : x;
        }



    }
}
