using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GraviRayTraceSharp.Scene
{
    /// <summary>
    /// Scene description class
    /// </summary>
    public class SceneDescription
    {
        /// <summary>
        /// Camera position - Distance from black hole
        /// </summary>
        public double ViewDistance { get; set; }

        /// <summary>
        /// Camera position - Inclination (vertical angle) in degrees
        /// </summary>
        public double ViewInclination { get; set; }

        /// <summary>
        /// Camera position - Angle (horizontal) in degrees
        /// </summary>
        public double ViewAngle { get; set; }

        /// <summary>
        /// Camera tilt - in degrees
        /// </summary>
        public double CameraTilt { get; set; }

        /// <summary>
        /// Camera aperture - need to manipulate the camera angle.
        /// </summary>
        public double CameraAperture { get; set; }

        /// <summary>
        /// Camera yaw - if we want to look sideways.
        /// Note: this is expressed in % of image width.
        /// </summary>
        public double CameraYaw { get; set; }

    }
}
