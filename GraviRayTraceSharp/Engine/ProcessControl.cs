using GraviRayTraceSharp.Helpers;
using GraviRayTraceSharp.Scene;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Remoting;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace GraviRayTraceSharp.Engine
{
    /// <summary>
    /// Class responsible for process control.
    /// It uses a shared file to read:
    /// - next frame to be rendered.
    /// - name of class that implements the ISceneGenerator to be used for rendering.
    /// </summary>
    public class ProcessControl
    {
        private string sceneGeneratorClassName;
        private string controlFileName;

        public ProcessControl(string controlFileName)
        {
            this.controlFileName = controlFileName;
        }

        /// <summary>
        /// 
        /// </summary>
        public int XResolution { get; private set; }

        /// <summary>
        /// 
        /// </summary>
        public int YResolution { get; private set; }

        /// <summary>
        /// 
        /// </summary>
        public int Fps { get; private set; }

        /// <summary>
        /// 
        /// </summary>
        public RenderQuality RenderQuality { get; private set; }

        /// <summary>
        /// 
        /// </summary>
        public string OutputPath { get; private set; }

        /// <summary>
        /// Fetch next frame number from the control file.
        /// Mark the frame no as reserved.
        /// </summary>
        /// <returns></returns>
        public int? GetNextFrameNo()
        {
            using (var fileReader = this.OpenControlFile())
            {
                if (fileReader != null)
                {
                    int? frameNo = null;
                    var position = 0;
                    do
                    {
                        position = fileReader.Position;
                        var line = fileReader.ReadLine();
                        var tokens = line.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                        if (tokens.Length == 2)
                        {
                            if (tokens[1] == "1") // found unrendered frame no
                            {
                                fileReader.BaseStream.Seek(position + fileReader.CurrentEncoding.GetByteCount(line) + 2, SeekOrigin.Begin); // move the stream pointer back
                                byte[] tokenChar = UTF8Encoding.UTF8.GetBytes("2");
                                fileReader.BaseStream.Write(tokenChar, 0, tokenChar.Length);  // write the "reserved" status back in file.

                                frameNo = Int32.Parse(tokens[0]);

                                return frameNo;
                            }
                        }
                        else
                            throw new Exception(String.Format("Invalid line [{0}] in control file!", line));
                    }
                    while (!fileReader.EndOfStream && frameNo == null);
                }

                return null;
            }
        }

        /// <summary>
        /// Mark the frame number as complete in the control file.
        /// </summary>
        /// <param name="frameNo"></param>
        public void CommitFrameNo(int frameNo)
        {
            using (var fileReader = this.OpenControlFile())
            {
                if (fileReader != null)
                {
                    bool finished = false;
                    do
                    {
                        var position = fileReader.Position;
                        var line = fileReader.ReadLine();
                        var tokens = line.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                        if (tokens.Length == 2)
                        {
                            if (tokens[0] == "" + frameNo) // found frame
                            {
                                fileReader.BaseStream.Seek(position + fileReader.CurrentEncoding.GetByteCount(line) + 2, SeekOrigin.Begin); // move the stream pointer back
                                byte[] tokenChar = UTF8Encoding.UTF8.GetBytes("3");
                                fileReader.BaseStream.Write(tokenChar, 0, tokenChar.Length);  // write the "committed" status back in file.
                                finished = true;
                            }
                        }
                        else
                            throw new Exception(String.Format("Invalid line [{0}] in control file!", line));
                    }
                    while (!fileReader.EndOfStream && !finished);
                }
            }
        }

        /// <summary>
        /// Get instance of the scene generator.
        /// </summary>
        /// <returns></returns>
        public ISceneGenerator GetSceneGenerator()
        {
            var tokens = this.sceneGeneratorClassName.Split(new char[] {','});
            ObjectHandle instance = (ObjectHandle)Activator.CreateInstance(tokens[1].Trim(), tokens[0].Trim());
            return instance.Unwrap() as ISceneGenerator;
        }

        protected TrackingTextReader OpenControlFile()
        {
            while (true)
            {
                try
                {
                    var file = File.Open(this.controlFileName, FileMode.Open, FileAccess.ReadWrite, FileShare.None);

                    // if open succeeded, read the first line - to refresh the SceneGenerator information
                    var reader = new TrackingTextReader(new StreamReader(file));

                    this.sceneGeneratorClassName = reader.ReadLine();
                    this.OutputPath = reader.ReadLine();
                    var res = reader.ReadLine();
                    var fps = reader.ReadLine();
                    var qual = reader.ReadLine();

                    var tokens = res.Split(' ');
                    this.XResolution = Int32.Parse(tokens[0]);
                    this.YResolution = Int32.Parse(tokens[1]);
                    this.Fps = Int32.Parse(fps);
                    this.RenderQuality = (RenderQuality)Enum.Parse(typeof(RenderQuality), qual);

                    return reader;
                }
                catch (IOException exc)
                {
                    Console.WriteLine("Control file locked, retrying...");
                }
                catch(Exception exc)
                {
                    Console.WriteLine("Error reading control file!");
                    Console.WriteLine(exc.ToString());
                    Console.WriteLine(exc.StackTrace);
                    return null;
                }
                Thread.Sleep(1000);
            }
            
            return null;
        }

    }
}
