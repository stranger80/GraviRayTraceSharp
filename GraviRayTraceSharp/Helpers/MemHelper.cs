using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace GraviRayTraceSharp.Helpers
{
    public class MemHelper
    {
        [DllImport("msvcrt.dll", SetLastError = false, CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr memcpy(IntPtr dest, IntPtr src, int count);
    }
}
