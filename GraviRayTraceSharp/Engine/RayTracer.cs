using GraviRayTraceSharp.Equation;
using GraviRayTraceSharp.Helpers;
using GraviRayTraceSharp.Mappings;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GraviRayTraceSharp
{
    /// <summary>
    /// Ray tracer class that is responsible for all image calculations.
    /// </summary>
    public class RayTracer
    {
        private KerrBlackHoleEquation equation;
        private int sizex;
        private int sizey;

        private Bitmap discTexture;
        private Bitmap backgroundTexture;

        private double cameraTilt;
        private double cameraYaw;

        private bool trace;

        private SphericalMapping backgroundMap;
        private DiscMapping discMap;

        /// <summary>
        /// List of points recorded during RungeKutty integration.
        /// This can be used to plot the trajectory of photons for illustration purposes.
        /// </summary>
        public List<Tuple<double,double,double>> RayPoints { get; private set; }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="equation"></param>
        /// <param name="sizex"></param>
        /// <param name="sizey"></param>
        /// <param name="discTexture"></param>
        /// <param name="backgroundTexture"></param>
        /// <param name="cameraTilt"></param>
        /// <param name="trace">If true, the steps of RungeKutta integration will be stored in a list, so that they can be studied after.</param>
        public RayTracer(KerrBlackHoleEquation equation, int sizex, int sizey, Bitmap discTexture, Bitmap backgroundTexture, double cameraTilt, double cameraYaw, bool trace = false)
        {
            this.equation = equation;
            this.sizex = sizex;
            this.sizey = sizey;
            this.discTexture = discTexture;
            this.backgroundTexture = backgroundTexture;
            this.cameraTilt = cameraTilt;
            this.trace = trace;
            this.cameraYaw = cameraYaw;

            lock (backgroundTexture)
            {
                this.backgroundMap = new SphericalMapping(backgroundTexture.Width, backgroundTexture.Height);
            }

            lock (discTexture)
            {
                this.discMap = new DiscMapping(equation.Rmstable, equation.Rdisk, discTexture.Width, discTexture.Height);
            }

            if (trace)
            {
                this.RayPoints = new List<Tuple<double, double, double>>();
            }
        }

        /// <summary>
        /// Shoot the ray through pixel (x1,y1).
        /// Returns color of the pixel.
        /// </summary>
        /// <param name="x1"></param>
        /// <param name="y1"></param>
        /// <returns></returns>
        public unsafe Color Calculate(double x1, double y1)
        {
            Color? pixel = null;
            Color hitPixel;

            double htry = 0.5, escal = 1e11, hdid = 0.0, hnext = 0.0;

            double range = 0.0025 * this.equation.Rdisk / (sizex - 1);

            double yaw = this.cameraYaw * sizex;


            double* y = stackalloc double[this.equation.N];
            double* dydx = stackalloc double[this.equation.N];
            double* yscal = stackalloc double[this.equation.N];
            double* ylaststep = stackalloc double[this.equation.N];

            int side;
            int i;

            double tiltSin = Math.Sin((cameraTilt / 180) * Math.PI);
            double tiltCos = Math.Cos((cameraTilt / 180) * Math.PI);

            double xRot = x1 - (sizex + 1) / 2 - yaw;
            double yRot = y1 - (sizey + 1) / 2;

            this.equation.SetInitialConditions(y, dydx,
                (int)(xRot * tiltCos - yRot * tiltSin) * range,
                (int)(yRot * tiltCos + xRot * tiltSin) * range
            );

            // if tracing on, store the initial point
            if (this.trace)
            {
                this.RayPoints.Clear();
                this.RayPoints.Add(new Tuple<double, double, double>(y[0], y[1], y[2]));
            }

            int rCount = 0;

            while (true)
            {
                MemHelper.memcpy((IntPtr)ylaststep, (IntPtr)y, this.equation.N * sizeof(double));

                this.equation.Function(y, dydx);

                for (i = 0; i < this.equation.N; i++)
                {
                    yscal[i] = Math.Abs(y[i]) + Math.Abs(dydx[i] * htry) + 1.0e-3;
                }

                if (y[1] > Math.PI / 2)
                {
                    side = 1;
                }
                else if (y[1] < Math.PI / 2)
                {
                    side = -1;
                }
                else
                {
                    side = 0;
                }

                hnext = RungeKuttaEngine.RKIntegrate(this.equation, y, dydx, htry, escal, yscal, out hdid);

                if ((y[1] - Math.PI / 2) * side < 0)
                {
                    MemHelper.memcpy((IntPtr)ylaststep, (IntPtr)y, this.equation.N * sizeof(double));

                    this.IntersectionSearch(y, dydx, hdid);

                    // Ray hits accretion disc?
                    if ((y[0] <= this.equation.Rdisk) && (y[0] >= this.equation.Rmstable))
                    {
                        // y[0] - radial position
                        // y[2] - phi (horizontal) angular position

                        int r = (int)(450 * (y[0] - this.equation.Rmstable) / (this.equation.Rdisk - this.equation.Rmstable));

                        int xPos, yPos;

                        // do mapping of texture image
                        this.discMap.Map(y[0], y[1], y[2], out xPos, out yPos);

                        lock (discTexture)
                        {
                            if (pixel != null)
                            {
                                pixel = ColorHelper.AddColor(discTexture.GetPixel(xPos, yPos), pixel.Value);
                            }
                            else
                            {
                                pixel = discTexture.GetPixel(xPos, yPos);
                            }
                            // don't return yet, just remember the color to 'tint' the texture later 
                        }

                    }
                }

                // Ray hits the event horizon?
                if ((y[0] < this.equation.Rhor))
                {
                    hitPixel = Color.FromArgb(0, 0, 0);

                    // tint the color
                    if (pixel != null)
                    {
                        return ColorHelper.AddColor(hitPixel, pixel.Value);
                    }
                    else
                    {
                        return hitPixel;
                    }
                }

                // Ray escaped to infinity?
                if (y[0] > this.equation.R0)
                {
                    int xPos, yPos;

                    this.backgroundMap.Map(y[0], y[1], y[2], out xPos, out yPos);
                    
                    lock (this.backgroundTexture)
                    {
                        hitPixel = this.backgroundTexture.GetPixel(xPos, yPos);
                        // tint the color
                        if (pixel != null)
                        {
                            return ColorHelper.AddColor(hitPixel, pixel.Value);
                        }
                        else
                        {
                            return hitPixel;
                        }
                    }
                }

                // if tracing on, store the calculated point
                if (this.trace)
                {
                    this.RayPoints.Add(new Tuple<double, double, double>(y[0], y[1], y[2]));
                }

                htry = hnext;

                if (rCount++ > 1000000) // failsafe...
                {
                    Console.WriteLine("Error - solution not converging!");
                    return Color.Fuchsia;
                }
            }

        }

        /// <summary>
        /// Use Runge-Kutta steps to find intersection with horizontal plane of the scene.
        /// This is necessary to stop integrating when the ray hits the accretion disc.
        /// </summary>
        /// <param name="y"></param>
        /// <param name="dydx"></param>
        /// <param name="hupper"></param>
        private unsafe void IntersectionSearch(double* y, double* dydx, double hupper)
        {
            unsafe
            {
                double hlower = 0.0;

                int side;
                if (y[1] > Math.PI / 2.0)
                {
                    side = 1;
                }
                else if (y[1] < Math.PI / 2.0)
                {
                    side = -1;
                }
                else
                {
                    // unlikely, but needs to handle a situation when ray hits the plane EXACTLY
                    return;
                }

                this.equation.Function(y, dydx);

                while ((y[0] > this.equation.Rhor) && (y[0] < this.equation.R0) && (side != 0))
                {
                    double* yout = stackalloc double[this.equation.N];
                    double* yerr = stackalloc double[this.equation.N];

                    double hdiff = hupper - hlower;

                    if (hdiff < 1e-7)
                    {
                        RungeKuttaEngine.RKIntegrateStep(this.equation, y, dydx, hupper, yout, yerr);

                        MemHelper.memcpy((IntPtr)y, (IntPtr)yout, this.equation.N * sizeof(double));

                        return;
                    }

                    double hmid = (hupper + hlower) / 2;

                    RungeKuttaEngine.RKIntegrateStep(this.equation, y, dydx, hmid, yout, yerr);

                    if (side * (yout[1] - Math.PI / 2.0) > 0)
                    {
                        hlower = hmid;
                    }
                    else
                    {
                        hupper = hmid;
                    }
                }
            }
        }
    }
}
