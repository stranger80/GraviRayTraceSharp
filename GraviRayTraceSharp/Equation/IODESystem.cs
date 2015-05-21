using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GraviRayTraceSharp.Equation
{
    /// <summary>
    /// Interface that defines a system of Ordinary Differential Equations in a way 
    /// that makes them integrable using Runge-Kutty algorithm.
    /// </summary>
    public interface IODESystem
    {
        /// <summary>
        /// Calculate the value of derivatives (dydx) from a set of variable values (y array)
        /// </summary>
        /// <param name="y"></param>
        /// <param name="dydx"></param>
        unsafe void Function(double* y, double* dydx);

        /// <summary>
        /// Number of equations in the ODE system
        /// </summary>
        int N
        {
            get;
        }

    }
}
