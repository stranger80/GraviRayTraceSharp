using GraviRayTraceSharp.Engine;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace GraviRayTraceSharp
{
    class Program
    {
        static void Main(string[] args)
        {


            //var rayIllustrator = new RayIllustrationGenerator(new Scene() { ViewDistance = 30, ViewInclination = 90, CameraAperture = 3.5 });
            //rayIllustrator.Process();
            //return;

            int? frameNo = null;
            
            if(args.Length == 0)
            {
                Console.WriteLine("Gravitational Black Hole Ray Tracer.");
                Console.WriteLine("A control file name is required as parameter.");
            }
            
            var controller = new ProcessControl(args[0]);

            while(true)
            {
                frameNo = controller.GetNextFrameNo();

                if (frameNo == null)
                {
                    Console.WriteLine("No job to be done, waiting...");
                    Thread.Sleep(10000);
                }
                else
                {

                    var rayTracer = new RayProcessor(
                        controller.XResolution,
                        controller.YResolution,
                        controller.GetSceneGenerator().GetScene(frameNo.Value, controller.Fps),
                        frameNo.Value,
                        controller.RenderQuality,
                        controller.OutputPath);
                    rayTracer.Process();

                    controller.CommitFrameNo(frameNo.Value);
                }
            }

        }
    }
}
