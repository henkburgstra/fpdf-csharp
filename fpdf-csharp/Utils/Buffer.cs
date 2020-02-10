using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;

namespace FpdfCsharp.Utils
{
    public class Buffer: TextReader
    {
        private Stream stream;
        private StreamReader reader;
        private StreamWriter writer;

        private void Init()
        {
            this.reader = new StreamReader(stream);
            this.writer = new StreamWriter(stream);
        }

        public Buffer()
        {
            stream = new MemoryStream();
            Init();
        }

        public Buffer(Stream stream)
        {
            this.stream = stream;
            Init();
        }

        public void Write(byte[] data)
        {
            stream.Seek(0, SeekOrigin.End);
            stream.Write(data, 0, data.Length);
        }
        public void Writef(string fmtStr, params object[] args)
        {
            Write(Encoding.Unicode.GetBytes(String.Format(CultureInfo.InvariantCulture, fmtStr, args)));
        }

        public void WriteByte(byte data)
        {
            stream.Seek(0, SeekOrigin.End);
            stream.WriteByte(data);
        }

        public void WriteString(string data)
        {
            stream.Seek(0, SeekOrigin.End);
            writer.Write(data);
        }

             
        public override int Read()
        {
            return reader.Read();
        }

        public override string ReadToEnd()
        {
            return reader.ReadToEnd();
        }

        public string String()
        {
            return ReadToEnd();
        }

        public void ReadFrom(Buffer buf)
        {
            buf.Seek(0, SeekOrigin.Begin);
            var read = buf.Read();
            while (read != -1)
            {
                WriteByte((byte)read);
                read = buf.Read();
            }
        }

        public void ReadFrom(TextReader buf)
        {
            
        }

        public long Seek(long offset, SeekOrigin loc)
        {
            return stream.Seek(offset, loc);
        }
    }
}
