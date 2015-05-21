using System;
using System.Drawing;
using System.IO;
using GraviRayTraceSharp.Scene;
using GraviRayTraceSharp.Equation;

namespace GraviRayTraceSharp
{
    /// <summary>
    /// This helper class does trigger rendering of a set of rays, which are recorded.
    /// The recorded data points can be used to plot ray paths for illustration purposes.
    /// </summary>
    class RayIllustrationGenerator
    {
        static Bitmap texture = new Bitmap("bgedit.jpg");
        static Bitmap coronaTexture = new Bitmap("adisk_skewed.png");

        SceneDescription sceneDescription;
        
        public RayIllustrationGenerator(SceneDescription scene)
        {
            this.sceneDescription = scene;
        }

        public void Process()
        {
            int height = 400;
            int n = 10;

            var tracer = new RayTracer(
                            new KerrBlackHoleEquation(sceneDescription.ViewDistance, sceneDescription.ViewInclination, sceneDescription.ViewAngle, 20.0, sceneDescription.CameraAperture),
                            200, height, coronaTexture, texture, sceneDescription.CameraTilt, sceneDescription.CameraYaw, true); 

            for (int i = 0; i < n; i++)
            {
                Color pixel = tracer.Calculate(100, height - height*i/(n*2));

                using (var file = File.CreateText(String.Format("ray_{0}.dat", i)))
                {
                    foreach (var point in tracer.RayPoints)
                    {
                        var cartPoint = SphericalToCartesian(point);
                        file.WriteLine(String.Format("{0:0.000000} {1:0.000000}", cartPoint.Item1, cartPoint.Item3).Replace(",", "."));
                    }
                    file.Close();
                }

                using (var file = File.CreateText(String.Format("ray_spherical_{0}.dat", i)))
                {
                    foreach (var point in tracer.RayPoints)
                    {
                        file.WriteLine(String.Format("{0:0.000000} {1:0.000000} {2:0.000000}", point.Item1, point.Item2, point.Item3).Replace(",", "."));
                    }
                    file.Close();
                }

            }

        }

        private Tuple<double, double, double> SphericalToCartesian(Tuple<double, double, double> point)
        {
            return new Tuple<double, double, double>(
                point.Item1 * Math.Cos(point.Item3) * Math.Sin(point.Item2),
                point.Item1 * Math.Sin(point.Item3) * Math.Sin(point.Item2),
                point.Item1 * Math.Cos(point.Item2)
                );
        }

    }
}
