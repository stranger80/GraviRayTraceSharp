using System;

namespace GraviRayTraceSharp.Equation
{
    public class KerrBlackHoleEquation : IODESystem
    {
        // Motion constants
        private double L; // angular momentum in phi direction
        private double K; // Carter's constant's element
        private double a; // angular momentum in theta direction
        private double a2; // a-squared

        // Initial conditions :
        // Ray starting location in Boyer-Lindquist coordinates
        public double R0 { get; private set; }

        private double theta0;
        private double phi0;
        private double aperture;

        // Dimensions of the "accretion disc"
        public double Rhor { get; private set; }
        public double Rmstable { get; private set; }
        public double Rdisk { get; private set; }

        /// <summary>
        /// Public constructor.
        /// </summary>
        /// <param name="r">Starting camera distance (typically 1000.0)</param>
        /// <param name="theta">Starting camera theta (vertical angle) in degrees</param>
        /// <param name="phi">Starting camera phi (horizontal angle) in degrees</param>
        /// <param name="rDisc">Radius of the accretion disc (default 20.0)</param>
        public KerrBlackHoleEquation(double r, double theta, double phi, double rDisc, double aperture)
        {
            a = -0.5; // angular momentum in theta direction (hard-coded)

            R0 = r;
            theta0 = (Math.PI / 180.0) * theta;
            phi0 = (Math.PI / 180.0) * phi;

            a2 = a * a;

            Rhor = 1.0 + Math.Sqrt(1.0 - a2) + 1e-5;
            Rdisk = 16.0;
            Rmstable = this.InnermostStableOrbit();

            this.aperture = aperture;
        }

        /// <summary>
        /// Number of equations in the set
        /// </summary>
        public int N
        {
            get { return 5; }
        }
        
        /// <summary>
        /// Function returning the equations that are included in the ODE system.
        /// Coupled differential equations describing motion of photon.
        /// </summary>
        /// <param name="y">Vector describing current state of the ODE system</param>
        /// <param name="dydx">Coefficient vector of the differential equations</param>
        public unsafe void Function(double* y, double* dydx)
        {
            double r, theta, pr, ptheta;

            r = y[0];
            theta = y[1];
            pr = y[3];
            ptheta = y[4];

            double r2 = r * r;
            double twor = 2.0 * r;

            double sintheta, costheta;
            sintheta = Math.Sin(theta);
            costheta = Math.Cos(theta);
            double cos2 = costheta * costheta;
            double sin2 = sintheta * sintheta;

            double sigma = r2 + a2 * cos2;
            double delta = r2 - twor + a2;
            double sd = sigma * delta;
            double siginv = 1.0 / sigma;
            double sigdelinv = 1.0 / sd;

            /* Prevent problems with the axis */
            if (sintheta < 1e-8)
            {
                sintheta = 1e-8;
                sin2 = 1e-16;
            }

            dydx[0] = -pr * delta * siginv;
            dydx[1] = -ptheta * siginv;
            dydx[2] = -(twor * a + (sigma - twor) * L / sin2) * sigdelinv;
            //dydx[3] = -(1.0 + (twor * (r2 + a2) - twor * a * L) * sigdelinv); // this coef is not used anywhere!
            dydx[3] = -(((r - 1.0) * (-K) + twor * (r2 + a2) - 2.0 * a * L) * sigdelinv - 2.0 * pr * pr * (r - 1.0) * siginv);
            dydx[4] = -sintheta * costheta * (L * L / (sin2 * sin2) - a2) * siginv;
        }

        /// <summary>
        /// Set initial conditions for a starting point of the ray that is being simulated.
        /// </summary>
        /// <param name="y0">Vector of coefficients describing state of the system.</param>
        /// <param name="ydot0">Vector of ODE coefficients that is initialized by this funciton call</param>
        /// <param name="x">x-coordinate of the ray</param>
        /// <param name="y">y-coordinate of the ray</param>
        public unsafe void SetInitialConditions(double* y0, double* ydot0, double x, double y)
        {
            y0[0] = R0;
            y0[1] = theta0;
            y0[2] = phi0;

            double sintheta, costheta;
            sintheta = Math.Sin(theta0);
            costheta = Math.Cos(theta0);
            double cos2 = costheta * costheta;
            double sin2 = sintheta * sintheta;

            double rdot0 = Math.Cos(y) * Math.Cos(x);

            double thetadot0 = Math.Sin(y) / aperture; 

            double r2 = R0 * R0;
            double sigma = r2 + a2 * cos2;
            double delta = r2 - 2.0 * R0 + a2;
            double s1 = sigma - 2.0 * R0;

            y0[3] = rdot0 * sigma / delta;
            y0[4] = thetadot0 * sigma;

            double phidot0 = Math.Cos(y) * Math.Sin(x) / (aperture * Math.Sin(theta0));
            double energy2 = s1 * (rdot0 * rdot0 / delta + thetadot0 * thetadot0)
                            + delta * sin2 * phidot0 * phidot0;

            double energy = Math.Sqrt(energy2);

            y0[3] = y0[3] / energy;
            y0[4] = y0[4] / energy;

            L = ((sigma * delta * phidot0 - 2.0 * a * R0 * energy) * sin2 / s1) / energy;

            K = y0[4] * y0[4] + a2 * sin2 + L * L / sin2;

            /* Call the ODE function to scale the starting point by energy factor */
            this.Function(y0, ydot0);
        }
        
        private double InnermostStableOrbit()
        {
            double z1 = 1 + Math.Pow(1 - a2, 1.0 / 3.0) * (Math.Pow(1 + a, 1.0 / 3.0) + Math.Pow(1 - a, 1.0 / 3.0));
            double z2 = Math.Sqrt(3 * a2 + z1 * z1);
            return 3 + z2 - Math.Sqrt((3 - z1) * (3 + z1 + 2 * z2));
        }
    }
}
