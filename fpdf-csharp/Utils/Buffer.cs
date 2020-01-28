using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace FpdfCsharp.Utils
{
    public class Buffer
    {
        private MemoryStream stream;
        private StreamReader reader;
        private StreamWriter writer;
        private long readPos;
        private long writePos;

        private void Init()
        {
            this.readPos = stream.Position;
            this.writePos = stream.Position;
            this.reader = new StreamReader(stream);
            this.writer = new StreamWriter(stream);
        }

        public Buffer()
        {
            stream = new MemoryStream();
        }

        public void Write(byte[] data)
        {
            stream.Seek(0, SeekOrigin.End);
            stream.Write(data, 0, data.Length);
        }

        public void WriteString(string data)
        {
            stream.Seek(0, SeekOrigin.End);
            writer.Write(data);
        }
    }
}
