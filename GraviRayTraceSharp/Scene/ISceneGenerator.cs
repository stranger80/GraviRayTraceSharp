using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GraviRayTraceSharp.Scene
{
    /// <summary>
    /// Interface of scene generators.
    /// </summary>
    public interface ISceneGenerator
    {
        /// <summary>
        /// Get scene description for a given frame.
        /// Note that Scene generator is expected to take into account also the frame rate.
        /// </summary>
        /// <param name="frame">Requested frame number.</param>
        /// <param name="fps">Animation's frame rate.</param>
        /// <returns></returns>
        SceneDescription GetScene(int frame, double fps);
    }
}
