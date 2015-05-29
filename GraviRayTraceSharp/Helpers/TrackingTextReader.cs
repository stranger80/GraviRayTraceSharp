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
        private StreamReader _baseReader;
        private int _position;

        public TrackingTextReader(StreamReader baseReader)
            : base(baseReader.BaseStream)
        {
            _baseReader = baseReader;
        }

        public override int Read()
        {

            int c = _baseReader.Read();
            _position += _baseReader.CurrentEncoding.GetByteCount("" + (char)c);
            return c;
        }

        public override string ReadLine()
        {
            var text = new StringBuilder();

            //text.Append(_baseReader.ReadLine());

            while (_baseReader.Peek() != 10 && _baseReader.Peek() != 13 && _baseReader.Peek() != -1)
            {
                text.Append((char)this.Read());
            }

            while (_baseReader.Peek() == 10 || _baseReader.Peek() == 13)
            {
                this.Read();
            }

            return text.ToString();
        }
        public override int Peek()
        {
            return _baseReader.Peek();
        }

        public bool EndOfStream
        {
            get { return _baseReader.BaseStream.Position == this._position + 3; }
        }

        public int Position
        {
            get { return _position; }
        }
    }
}
