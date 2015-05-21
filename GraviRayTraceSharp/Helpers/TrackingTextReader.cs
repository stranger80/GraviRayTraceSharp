using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GraviRayTraceSharp.Helpers
{
    public class TrackingTextReader : StreamReader
    {
        private TextReader _baseReader;
        private int _position;

        public TrackingTextReader(StreamReader baseReader) : base(baseReader.BaseStream)
        {
            _baseReader = baseReader;
        }

        public override int Read()
        {
            _position++;
            return _baseReader.Read();
        }

        public override string ReadLine()
        {
            var result = base.ReadLine();

            _position += this.CurrentEncoding.GetByteCount(result+Environment.NewLine);
            return result;
        }
        public override int Peek()
        {
            return _baseReader.Peek();
        }

        public int Position
        {
            get { return _position; }
        }
    }
}
