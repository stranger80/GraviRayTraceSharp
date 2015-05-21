using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GraviRayTraceSharp.Scene
{
    /// <summary>
    /// Default scene generator, defining a sequence to be animated.
    /// </summary>
    public class DefaultPathSceneGenerator : ISceneGenerator
    {
        public SceneDescription GetScene(int frame, double fps)
        {
            SceneDescription result = new SceneDescription();
            result.CameraAperture = 2.0;

            double t = frame / fps;

            double r = 600 - t * 2.53;

            // factor of attenuation of camera's sinusoidal motions (the closer to black hole - the calmer the flight is)
            double calmFactor = Math.Pow((600 - r) / 575, 20); 
            
            double phi = t*3;
            double theta = 84
                + 8 * Math.Sin(phi * Math.PI / 180) * (1 - calmFactor) // precession
                + 3 * calmFactor;

            result.ViewAngle = phi;
            result.ViewDistance = r;
            result.ViewInclination = theta;
            result.CameraAperture = 24.00/500.0*r + 3.2;
            result.CameraTilt = 8.0 * Math.Cos(phi * Math.PI / 180) * (1 - calmFactor);
            result.CameraYaw =  calmFactor * 1.0; // we will be 'landing' on the accretion disc...

            return result;
        }
    }
}
