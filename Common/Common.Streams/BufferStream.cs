using Common.Generic;
using System;
using System.IO;
using System.Threading;

namespace Common.Streams
{
	public class BufferStream : Stream
	{
		private AQueue<MemoryStream> _queueMS = new AQueue<MemoryStream>();

		private MemoryStream _curMS = null;

		private object _lock = new object();

		public int _size = 0;

		public override long Length => _size;

		public virtual int Size => (int)Length;

		public override long Position
		{
			get
			{
				return 0L;
			}
			set
			{
				throw new NotImplementedException();
			}
		}

		public override bool CanRead => true;

		public override bool CanSeek => false;

		public override bool CanWrite => true;

		public void Write(byte[] bs)
		{
			Write(bs, 0, bs.Length);
		}

		public override void Write(byte[] bs, int index, int count)
		{
			lock (_lock)
			{
				_Write(bs, index, count);
			}
		}

		public byte[] Read(int len)
		{
			lock (_lock)
			{
				return _Read(len);
			}
		}

		public override int Read(byte[] buf, int offset, int len)
		{
			byte[] array = Read(len);
			Array.Copy(array, 0, buf, offset, len);
			return array.Length;
		}

		private void _Write(byte[] bs, int index, int count)
		{
			MemoryStream memoryStream = new MemoryStream(bs, index, count);
			memoryStream.Position = 0L;
			lock (_queueMS.GetLock())
			{
				_queueMS.Enqueue(memoryStream);
				_size += count;
			}
		}

		private byte[] _Read(int len)
		{
			while (_size < len)
			{
				Thread.Sleep(10);
		//		bool flag = true;
			}
			int size = _size;
			byte[] array = new byte[len];
			int num = 0;
			lock (_queueMS.GetLock())
			{
				while (num < len)
				{
					if (_curMS == null)
					{
						_curMS = _queueMS.Dequeue();
					}
					num += _curMS.Read(array, num, len - num);
					if (_curMS.Length == _curMS.Position)
					{
						_curMS = null;
					}
				}
				if (num != len)
				{
					throw new Exception();
				}
				_size -= len;
			}
			return array;
		}

		public override void Flush()
		{
		}

		public override void SetLength(long value)
		{
			throw new NotImplementedException();
		}

		public override long Seek(long offset, SeekOrigin origin)
		{
			throw new NotImplementedException();
		}

		public byte[] ToArray()
		{
			byte[] array = new byte[_size];
			Read(array, 0, array.Length);
			return array;
		}

		public override void Close()
		{
			base.Close();
			if (_curMS != null)
			{
				_queueMS.Clear();
			}
			if (_curMS != null)
			{
				_curMS.Close();
			}
		}
	}
}
