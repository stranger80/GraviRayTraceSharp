using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace GraviRayTraceSharp
{
    /// <summary>
    /// Class defining a single renderer job - scope of calculation to be executed within one processor core.
    /// </summary>
    class RTScanLineJob
    {
        public int JobId { get; set; }
        public RayTracer RayTracer { get; set; }
        public Bitmap RenderImage { get; set; }
        public List<int> LinesList { get; set; }
        public Thread Thread { get; set; }
        public object Semaphore { get; set; }
    }
}
