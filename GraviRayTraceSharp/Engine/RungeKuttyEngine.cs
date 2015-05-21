using GraviRayTraceSharp.Equation;
using GraviRayTraceSharp.Helpers;
using System;

namespace GraviRayTraceSharp
{
    /// <summary>
    /// Class that implements adaptive Runge-Kutta integration algorithm.
    /// Actually the implemented method variation is known as Cash-Karp.
    /// </summary>
    public class RungeKuttaEngine
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="equation"></param>
        /// <param name="y"></param>
        /// <param name="dydx"></param>
        /// <param name="htry">Initial step of integration calculation</param>
        /// <param name="escal">Error scale factor</param>
        /// <param name="yscal"></param>
        /// <param name="hdid">Adjusted integration step after the calculation</param>
        /// <returns></returns>
        unsafe public static double RKIntegrate(IODESystem equation, double* y, double* dydx, double htry, double escal, double* yscal, out double hdid)
        {
            int i;

            double hnext;

            double errmax, h = htry, htemp;
            double* yerr = stackalloc double[equation.N];
            double* ytemp = stackalloc double[equation.N];

            while (true)
            {
                // Run a single step of integration using step h 
                RKIntegrateStep(equation, y, dydx, h, ytemp, yerr);

                // Find the maximum calculation error of all equations after the calculation step
                errmax = 0.0;
                for (i = 0; i < equation.N; i++)
                {
                    double temp = Math.Abs(yerr[i] / yscal[i]);
                    if (temp > errmax) errmax = temp;
                }

                // Multiply by error scale factor and check if within accepted limits. 
                // If yes - exit loop.
                errmax *= escal;
                if (errmax <= 1.0) break;

                // Adjust the step to be 0.9*h / (quartic root of adjusted error), but not less than 0.1*h
                htemp = 0.9 * h / Math.Sqrt(Math.Sqrt(errmax));

                h *= 0.1;

                if (h >= 0.0)
                {
                    if (htemp > h) h = htemp;
                }
                else
                {
                    if (htemp < h) h = htemp;
                }
            }

            // Return next suggested step of integration.
            // If error was small, increase step 5 times (to save computing time).
            if (errmax > 1.89e-4)
            {
                hnext = 0.9 * h * Math.Pow(errmax, -0.2);
            }
            else
            {
                hnext = 5.0 * h;
            }

            hdid = h;

            MemHelper.memcpy((IntPtr)y, (IntPtr)ytemp, equation.N * sizeof(double));

            return hnext;
        }

        /// <summary>
        /// Calculate single step of integration algorithm.
        /// </summary>
        /// <param name="equation"></param>
        /// <param name="y"></param>
        /// <param name="dydx"></param>
        /// <param name="h"></param>
        /// <param name="yout"></param>
        /// <param name="yerr"></param>
        unsafe public static void RKIntegrateStep(IODESystem equation, double* y, double* dydx, double h, double* yout, double* yerr)
        {
            int i;

            double* ak = stackalloc double[equation.N];

            double* ytemp1 = stackalloc double[equation.N];
            double* ytemp2 = stackalloc double[equation.N];
            double* ytemp3 = stackalloc double[equation.N];
            double* ytemp4 = stackalloc double[equation.N];
            double* ytemp5 = stackalloc double[equation.N];

            for (i = 0; i < equation.N; i++)
            {
                double hdydx = h * dydx[i];
                double yi = y[i];
                ytemp1[i] = yi + 0.2 * hdydx;
                ytemp2[i] = yi + (3.0 / 40.0) * hdydx;
                ytemp3[i] = yi + 0.3 * hdydx;
                ytemp4[i] = yi - (11.0 / 54.0) * hdydx;
                ytemp5[i] = yi + (1631.0 / 55296.0) * hdydx;
                yout[i] = yi + (37.0 / 378.0) * hdydx;
                yerr[i] = ((37.0 / 378.0) - (2825.0 / 27648.0)) * hdydx;
            }

            equation.Function(ytemp1, ak);

            for (i = 0; i < equation.N; i++)
            {
                double yt = h * ak[i];
                ytemp2[i] += (9.0 / 40.0) * yt;
                ytemp3[i] -= 0.9 * yt;
                ytemp4[i] += 2.5 * yt;
                ytemp5[i] += (175.0 / 512.0) * yt;
            }

            equation.Function(ytemp2, ak);

            for (i = 0; i < equation.N; i++)
            {
                double yt = h * ak[i];
                ytemp3[i] += 1.2 * yt;
                ytemp4[i] -= (70.0 / 27.0) * yt;
                ytemp5[i] += (575.0 / 13824.0) * yt;
                yout[i] += (250.0 / 621.0) * yt;
                yerr[i] += ((250.0 / 621.0) - (18575.0 / 48384.0)) * yt;
            }

            equation.Function(ytemp3, ak);

            for (i = 0; i < equation.N; i++)
            {
                double yt = h * ak[i];
                ytemp4[i] += (35.0 / 27.0) * yt;
                ytemp5[i] += (44275.0 / 110592.0) * yt;
                yout[i] += (125.0 / 594.0) * yt;
                yerr[i] += ((125.0 / 594.0) - (13525.0 / 55296.0)) * yt;
            }

            equation.Function(ytemp4, ak);

            for (i = 0; i < equation.N; i++)
            {
                double yt = h * ak[i];
                ytemp5[i] += (253.0 / 4096.0) * yt;
                yerr[i] -= (277.0 / 14336.0) * yt;
            }

            equation.Function(ytemp5, ak);

            for (i = 0; i < equation.N; i++)
            {
                double yt = h * ak[i];
                yout[i] += (512.0 / 1771.0) * yt;
                yerr[i] += ((512.0 / 1771.0) - 0.25) * yt;
            }
        }


    }
}
