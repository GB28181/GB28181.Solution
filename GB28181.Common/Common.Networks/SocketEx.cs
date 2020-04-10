using System.IO;
using System.Net.Sockets;

namespace Common.Networks
{
	public class SocketEx : Socket
	{
		private NetworkStream __ns = null;

		private BinaryReader __br = null;

		private BinaryWriter __bw = null;

		private NetworkStream _ns
		{
			get
			{
				if (__ns == null)
				{
					__ns = new NetworkStream(this);
				}
				return __ns;
			}
		}

		private BinaryReader _br
		{
			get
			{
				if (__br == null)
				{
					__br = new BinaryReader(_ns);
				}
				return __br;
			}
		}

		private BinaryWriter _bw
		{
			get
			{
				if (__bw == null)
				{
					__bw = new BinaryWriter(_ns);
				}
				return __bw;
			}
		}

		public SocketEx()
			: this(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp)
		{
		}

		public SocketEx(AddressFamily addressFamily, SocketType socketType, ProtocolType protocolType)
			: base(addressFamily, socketType, protocolType)
		{
		}

		public virtual byte[] ReadBytes(int len)
		{
			return _br.ReadBytes(len);
		}

		public virtual byte ReadByte()
		{
			return _br.ReadByte();
		}

		public virtual short ReadInt16()
		{
			return _br.ReadInt16();
		}

		public virtual int ReadInt32()
		{
			return _br.ReadInt32();
		}

		public virtual long ReadInt64()
		{
			return _br.ReadInt64();
		}

		public virtual void Write(byte[] buffer, int index, int count)
		{
			_bw.Write(buffer, index, count);
		}

		public virtual void Write(byte[] buffer)
		{
			_bw.Write(buffer);
		}

		public virtual void Write(byte value)
		{
			_bw.Write(value);
		}

		public virtual void Write(short value)
		{
			_bw.Write(value);
		}

		public virtual void Write(int value)
		{
			_bw.Write(value);
		}

		public virtual void Write(long value)
		{
			_bw.Write(value);
		}

		protected override void Dispose(bool disposing)
		{
			if (__bw != null)
			{
				__bw.Close();
			}
			if (__br != null)
			{
				__br.Close();
			}
			if (__ns != null)
			{
				__ns.Close();
			}
			base.Dispose(disposing);
		}
	}
}
