using Helpers;
using System;
using System.IO;

namespace StreamingKit.Media.TS
{
    /// <summary>
    /// 非线程安全
    /// </summary>
    public class IOMemoryStream : MemoryStream
    {
        private long _lastReadPosition = 0;
        private long _lastWritePosition = 0;
        private readonly object _sync = new object();
        //一定注意这里是new
        public new long Position { get { return base.Position; } set { throw new Exception(""); } }
        public long ReadPosition { get { return _lastReadPosition; } }
        public IOMemoryStream()
        {
        }

        public IOMemoryStream(byte[] data)
        {
            Write(data, 0, data.Length);

        }

        public override int Read(byte[] buffer, int offset, int count)
        {

            while (Length - _lastReadPosition < count)
            {
                ThreadEx.Sleep();
            }

            int read = 0;
            lock (_sync)
            {
                base.Position = _lastReadPosition;
                read = base.Read(buffer, offset, count);
                _lastReadPosition = Position;
            }
            return read;
        }

        public override int ReadByte()
        {
            try
            {
                while (Length - _lastReadPosition < 1)
                    ThreadEx.Sleep();

                int read = 0;
                lock (_sync)
                {
                    base.Position = _lastReadPosition;
                    read = base.ReadByte();
                    _lastReadPosition = Position;
                }
                return read;
            }
            catch (Exception)
            {
                throw;
            }
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            lock (_sync)
            {
                base.Position = _lastWritePosition;
                base.Write(buffer, offset, count);
                _lastWritePosition = Position;
            }
        }

        public override void WriteByte(byte value)
        {
            lock (_sync)
            {
                base.Position = _lastWritePosition;
                base.WriteByte(value);
                _lastWritePosition = Position;
            }

        }

        public override long Seek(long offset, SeekOrigin loc)
        {
            lock (_sync)
            {
                long pos = base.Seek(offset, loc);
                _lastReadPosition = Position;
                return pos;
            }
        }

        public IOMemoryStream Tolave()
        {
            lock (_sync)
            {
                var bytes = new byte[Length - _lastReadPosition];
                Read(bytes, 0, bytes.Length);
                return new IOMemoryStream(bytes);
            }
        }

    }

}
