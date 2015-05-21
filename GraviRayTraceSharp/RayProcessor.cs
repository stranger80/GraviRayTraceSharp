using GraviRayTraceSharp.Helpers;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.Threading;
using GraviRayTraceSharp.Scene;
using GraviRayTraceSharp.Equation;
using System.IO;


namespace GraviRayTraceSharp
{
    public enum RenderQuality
    {
        Low, // Low resolution
        Medium, // Single stage - adaptive antialiasing
        High, // Two stage - adaptive antialiasing
        UltraHigh // Multi-sampling
    }

    /// <summary>
    /// Main raytracer processing class.
    /// Responsible for image generation and enhancing.
    /// Multithreading is leveraged to achieve optimal utilization of hardware.
    /// </summary>
    class RayProcessor
    {
        int sizex; 
        int sizey; 

        int frame;
        SceneDescription sceneDescription;

        // antialiasing params
        int samplesPerCell = 4; // use n x n samples per each pixel for antialiasing

        static Bitmap texture = new Bitmap("bgedit.jpg");
        static Bitmap coronaTexture = new Bitmap("adisk_skewed.png");

        public RenderQuality Quality { get; private set; }

        public string OutputPath { get; private set; }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="sizex">Image width</param>
        /// <param name="sizey">Image height</param>
        /// <param name="scene">Scene description</param>
        /// <param name="frame">Number of frame rendered</param>
        /// <param name="quality">Render quality (default: Medium)</param>
        public RayProcessor(int sizex, int sizey, SceneDescription scene, int frame, RenderQuality quality, string outputPath)
        {
            this.sizex = sizex; 
            this.sizey = sizey; 
            this.frame = frame;
            sceneDescription = scene;
            this.Quality = quality;
            this.OutputPath = outputPath;
        }

        /// <summary>
        /// Main processing.
        /// </summary>
        /// <returns></returns>
        public int Process()
        {
	        int i, j;

            Bitmap result = new Bitmap(sizex, sizey);

            // Run n threads simultaneously, so that all machine's processors are busy
            int nJobs = Environment.ProcessorCount;

            DateTime startTime = DateTime.Now;

            Console.WriteLine("Launching {0} jobs...", nJobs);

            List<List<int>> lineLists = new List<List<int>>();
            List<RTScanLineJob> jobDescriptions = new List<RTScanLineJob>();

            var semaphore = new object();

            for (i = 0; i < nJobs; i++)
            {
                var lineList = new List<int>(); 
                lineLists.Add(lineList);
                jobDescriptions.Add(new RTScanLineJob() 
                    { 
                        JobId = i, 
                        RayTracer = new RayTracer(
                            new KerrBlackHoleEquation(sceneDescription.ViewDistance, sceneDescription.ViewInclination, sceneDescription.ViewAngle, 20.0, sceneDescription.CameraAperture),
                            sizex, sizey, coronaTexture, texture, sceneDescription.CameraTilt, sceneDescription.CameraYaw), 
                        RenderImage = result, 
                        LinesList = lineList,
                        Thread = new Thread(new ParameterizedThreadStart(RayTraceJob)),
                        Semaphore = semaphore
                    });

            }

            // Use adaptive antialiasing - divide the resolution by number of vertical samples per line

            // distribute scan lines over job descriptions
            for (j = 0; j < sizey; j+=this.samplesPerCell)
            {
                if (j == 0)
                {
                    lineLists[0].Add(j);
                }
                else
                {
                    lineLists[(j/samplesPerCell) % nJobs].Add(j);
                }
            }

            if (this.Quality != RenderQuality.UltraHigh) // first pass of rendering is not needed for UltraHigh - multisampling covers everything!
            {
                // launch jobs
                for (i = 0; i < nJobs; i++)
                {
                    jobDescriptions[i].Thread.Start(jobDescriptions[i]);
                }

                // wait for jobs to finish
                for (i = 0; i < nJobs; i++)
                {
                    jobDescriptions[i].Thread.Join();
                }
            }

            if (this.Quality == RenderQuality.Medium || this.Quality == RenderQuality.High) // enable first pass of antialiasing (note that for UltraHigh this is not needed, as multisampling covers everything)
            {
                // Do adaptive antialiasing
                Console.WriteLine("Adaptive antialiasing started...");

                // launch jobs
                for (i = 0; i < nJobs; i++)
                {
                    jobDescriptions[i].Thread = new Thread(new ParameterizedThreadStart(AntiAliasJob1));
                    jobDescriptions[i].Thread.Start(jobDescriptions[i]);
                }
                // wait for jobs to finish
                for (i = 0; i < nJobs; i++)
                {
                    jobDescriptions[i].Thread.Join();
                }
            }

            if (this.Quality == RenderQuality.High || this.Quality == RenderQuality.UltraHigh) // enable second pass of antialiasing
            {
                // Antialiasing pass 2
                // launch jobs
                for (i = 0; i < nJobs; i++)
                {
                    jobDescriptions[i].Thread = new Thread(new ParameterizedThreadStart(AntiAliasJob2));
                    jobDescriptions[i].Thread.Start(jobDescriptions[i]);
                }
                // wait for jobs to finish
                for (i = 0; i < nJobs; i++)
                {
                    jobDescriptions[i].Thread.Join();
                }
            }

            Console.WriteLine("Antialiasing finished");

            result.Save(Path.Combine(this.OutputPath, String.Format("render_{0:00000}.png", this.frame)), ImageFormat.Png);

            DateTime endTime = DateTime.Now;

            Console.WriteLine("Processing finished in {0} seconds", (endTime - startTime).TotalSeconds);
            
	        return 0;
        }

        private void DoAntialiasPass1(RTScanLineJob job)
        {
            Bitmap image = job.RenderImage;

            int i, n = 0;

            double colourDiffThreshold = 5;

            // first pass
            Console.WriteLine("Antialiasing first pass...");

            int imgX;
            int imgY;

            lock (job.Semaphore)
            {
                imgX = image.Width;
                imgY = image.Height;
            }

            foreach (int j in job.LinesList)
            {
                n = 0; // count rays
                for (i = 0; i < imgX - this.samplesPerCell; i += this.samplesPerCell)
                {
                    // determine if there are differences in colour between neighbouring pixels. If no - then colour all pixels the same
                    // otherwise - launch additional samples

                    Color p00, p01, p11, p10;

                    lock (job.Semaphore)
                    {
                        p00 = image.GetPixel(i, imgY - j - 1);
                        
                        // check if we hit an empty pixel - if yes, then skip to next line
                        if (p00.A == 0)
                        {
                            break;
                        }

                        p10 = image.GetPixel(i + samplesPerCell, imgY - j - 1);
                        p11 = image.GetPixel(i + samplesPerCell, Math.Max(imgY - (j + samplesPerCell)-1, 0));
                        p01 = image.GetPixel(i, Math.Max(imgY - (j + samplesPerCell) - 1, 0));
                    }



                    if (ColorHelper.ColorDifference(p00, p10) < colourDiffThreshold
                        && ColorHelper.ColorDifference(p00, p11) < colourDiffThreshold
                        && ColorHelper.ColorDifference(p00, p01) < colourDiffThreshold)
                    {
                        for(int y = j; y < j+samplesPerCell; y++)
                            for (int x = i; x < i + samplesPerCell; x++)
                            {
                                lock (job.Semaphore)
                                {
                                    image.SetPixel(x, imgY - y - 1, p00);
                                }
                            }
                    }
                    else
                    {
                        for (int y = j; y < j + samplesPerCell; y++)
                            for (int x = i; x < i + samplesPerCell; x++)
                            {
                                if (!(x == i && y == j))
                                {
                                    var pixel = job.RayTracer.Calculate(x, y);
                                    n++;
                                    lock (job.Semaphore)
                                    {
                                        image.SetPixel(x, imgY - y - 1, pixel);
                                    }
                                }
                            }
                    }
                }
                Console.WriteLine("Job {0}: Line {1} antialiased, {2} rays", job.JobId, j, n);
            }

        }


        private void DoAntialiasPass2(RTScanLineJob job)
        {
            Bitmap image = job.RenderImage;

            int i, j, n = 0;

            double colourDiffThreshold = 5;

            // second pass - now on individual pixel level

            int imgX;
            int imgY;

            lock (job.Semaphore)
            {
                imgX = image.Width;
                imgY = image.Height;
            }

            Console.WriteLine("Antialiasing second pass...");
            foreach (int line in job.LinesList)
            {
                for (j = line; j < line + this.samplesPerCell; j++) // at this point we only get every second line to process
                {
                    n = 0; // count rays

                    for (i = 0; i < imgX - 1; i++)
                    {
                        // determine if there are differences in colour between neighbouring pixels. If no - then colour all pixels the same
                        // otherwise - launch additional samples

                        Color p00, p01, p11, p10;

                        lock (job.Semaphore)
                        {
                            p00 = image.GetPixel(i, imgY - j - 1);
                            p10 = image.GetPixel(i + 1, imgY - j - 1);
                            p11 = image.GetPixel(i + 1, Math.Max(imgY - (j + 1) - 1, 0));
                            p01 = image.GetPixel(i, Math.Max(imgY - (j + 1) - 1, 0));
                        }

                        if (ColorHelper.ColorDifference(p00, p10) < colourDiffThreshold
                            && ColorHelper.ColorDifference(p00, p11) < colourDiffThreshold
                            && ColorHelper.ColorDifference(p00, p01) < colourDiffThreshold
                            && this.Quality != RenderQuality.UltraHigh) // for UltraHigh quality do multismapling regardless of colour difference
                        {
                            // do nothing
                        }
                        else
                        {
                            Color sAvg;

                            if (this.Quality != RenderQuality.UltraHigh)
                            {
                                var s10 = job.RayTracer.Calculate((double)i + 0.5, (double)j);
                                var s11 = job.RayTracer.Calculate((double)i + 0.5, (double)j + 0.5);
                                var s01 = job.RayTracer.Calculate((double)i, (double)j + 0.5);

                                n += 3;
                                sAvg = Color.FromArgb(
                                        (p00.R + s10.R + s11.R + s01.R) / 4,
                                        (p00.G + s10.G + s11.G + s01.G) / 4,
                                        (p00.B + s10.B + s11.B + s01.B) / 4);
                            }
                            else // UltraHigh - multi-sampling
                            {
                                int nRays = 4;
                                int rAcc = 0;
                                int gAcc = 0;
                                int bAcc = 0;

                                for (int k = 0; k < nRays; k++)
                                {
                                    for (int l = 0; l < nRays; l++)
                                    {
                                        var rayColor = job.RayTracer.Calculate((double)i + (double)k / (double)nRays, (double)j + (double)l / (double)nRays);
                                        rAcc += rayColor.R;
                                        gAcc += rayColor.G;
                                        bAcc += rayColor.B;
                                        n++;
                                    }
                                }

                                sAvg = Color.FromArgb(
                                    rAcc / nRays / nRays,
                                    gAcc / nRays / nRays,
                                    bAcc / nRays / nRays);

                            }


                            lock (job.Semaphore)
                            {
                                image.SetPixel(i, imgY - j - 1, sAvg);
                            }

                        }

                    }
                    Console.WriteLine("Job {0}: Line {1} antialiased, {2} rays", job.JobId, j, n);
                }
            }

        }

        public void RayTraceJob(object jobObj)
        {

            var job = jobObj as RTScanLineJob;

            int i, k;

            try
            {
                Console.WriteLine("Starting job {0}", job.JobId);

                //job.RayTracer.initialize();

                for (k = 0; k < job.LinesList.Count; k++)
                {
                    for (i = 0; i < sizex; i+=this.samplesPerCell)
                    {
                        //Console.WriteLine("Job {0}: {1}", job.JobId, i);

                        Color pixel = job.RayTracer.Calculate(i, job.LinesList[k]);
                        lock (job.Semaphore)
                        {
                            job.RenderImage.SetPixel(i, sizey - job.LinesList[k] - 1, pixel);
                            if (this.Quality == RenderQuality.Low)
                            {
                                for (int x = i; x < i + this.samplesPerCell; x++)
                                {
                                    for (int y = job.LinesList[k]; y < job.LinesList[k] + this.samplesPerCell; y++)
                                    {
                                        job.RenderImage.SetPixel(x, sizey - y - 1, pixel);
                                    }
                                }
                            }

                        }
                    }
                    Console.WriteLine("Job {0}: Line {1} rendered", job.JobId, job.LinesList[k]);
                }

                Console.WriteLine("Job {0} finished", job.JobId);

            }
            catch (Exception exc)
            {
                Console.WriteLine("Job {0} errored", job.JobId);

            }
        }

        public void AntiAliasJob1(object jobObj)
        {

            var job = jobObj as RTScanLineJob;

            try
            {
                this.DoAntialiasPass1(job);
                Console.WriteLine("Job {0} finished", job.JobId);
            }
            catch (Exception exc)
            {
                Console.WriteLine("Job {0} errored", job.JobId);

            }
        }

        public void AntiAliasJob2(object jobObj)
        {

            var job = jobObj as RTScanLineJob;

            try
            {
                this.DoAntialiasPass2(job);
                Console.WriteLine("Job {0} finished", job.JobId);
            }
            catch (Exception exc)
            {
                Console.WriteLine("Job {0} errored", job.JobId);

            }
        }


    }
}
