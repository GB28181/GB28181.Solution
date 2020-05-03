using System;
using System.Globalization;
using System.IO;
using System.Net.Sockets;
using System.Resources;
using System.Security.Cryptography;
using System.Text;

namespace Common.Streams
{
	public class BitStream : Stream
	{
		private sealed class BitStreamResources
		{
			private static ResourceManager _resman;

			private static object _oResManLock;

			private static bool _blnLoadingResource;

			private static void InitialiseResourceManager()
			{
				if (_resman == null)
				{
					lock (typeof(BitStreamResources))
					{
						if (_resman == null)
						{
							_oResManLock = new object();
							_resman = new ResourceManager("BKSystem.IO.BitStream", typeof(BitStream).Assembly);
						}
					}
				}
			}

			public static string GetString(string name)
			{
				if (_resman == null)
				{
					InitialiseResourceManager();
				}
				string @string;
				lock (_oResManLock)
				{
					if (_blnLoadingResource)
					{
						return "The resource manager was unable to load the resource: " + name;
					}
					_blnLoadingResource = true;
					@string = _resman.GetString(name, null);
					_blnLoadingResource = false;
				}
				return @string;
			}
		}

		private const int SizeOfByte = 8;

		private const int SizeOfChar = 128;

		private const int SizeOfUInt16 = 16;

		private const int SizeOfUInt32 = 32;

		private const int SizeOfSingle = 32;

		private const int SizeOfUInt64 = 64;

		private const int SizeOfDouble = 64;

		private const uint BitBuffer_SizeOfElement = 32u;

		private const int BitBuffer_SizeOfElement_Shift = 5;

		private const uint BitBuffer_SizeOfElement_Mod = 31u;

		private static uint[] BitMaskHelperLUT = new uint[33]
		{
			0u,
			1u,
			3u,
			7u,
			15u,
			31u,
			63u,
			127u,
			255u,
			511u,
			1023u,
			2047u,
			4095u,
			8191u,
			16383u,
			32767u,
			65535u,
			131071u,
			262143u,
			524287u,
			1048575u,
			2097151u,
			4194303u,
			8388607u,
			16777215u,
			33554431u,
			67108863u,
			134217727u,
			268435455u,
			536870911u,
			1073741823u,
			2147483647u,
			4294967295u
		};

		private bool _blnIsOpen = true;

		private uint[] _auiBitBuffer;

		private uint _uiBitBuffer_Length;

		private uint _uiBitBuffer_Index;

		private uint _uiBitBuffer_BitIndex;

		private static IFormatProvider _ifp = CultureInfo.InvariantCulture;

		public override long Length
		{
			get
			{
				if (!_blnIsOpen)
				{
					throw new ObjectDisposedException(BitStreamResources.GetString("ObjectDisposed_BitStreamClosed"));
				}
				return _uiBitBuffer_Length;
			}
		}

		public virtual long Length8
		{
			get
			{
				if (!_blnIsOpen)
				{
					throw new ObjectDisposedException(BitStreamResources.GetString("ObjectDisposed_BitStreamClosed"));
				}
				return (_uiBitBuffer_Length >> 3) + (((_uiBitBuffer_Length & 7) != 0) ? 1 : 0);
			}
		}

		public virtual long Length16
		{
			get
			{
				if (!_blnIsOpen)
				{
					throw new ObjectDisposedException(BitStreamResources.GetString("ObjectDisposed_BitStreamClosed"));
				}
				return (_uiBitBuffer_Length >> 4) + (((_uiBitBuffer_Length & 0xF) != 0) ? 1 : 0);
			}
		}

		public virtual long Length32
		{
			get
			{
				if (!_blnIsOpen)
				{
					throw new ObjectDisposedException(BitStreamResources.GetString("ObjectDisposed_BitStreamClosed"));
				}
				return (_uiBitBuffer_Length >> 5) + (((_uiBitBuffer_Length & 0x1F) != 0) ? 1 : 0);
			}
		}

		public virtual long Length64
		{
			get
			{
				if (!_blnIsOpen)
				{
					throw new ObjectDisposedException(BitStreamResources.GetString("ObjectDisposed_BitStreamClosed"));
				}
				return (_uiBitBuffer_Length >> 6) + (((_uiBitBuffer_Length & 0x3F) != 0) ? 1 : 0);
			}
		}

		public virtual long Capacity
		{
			get
			{
				if (!_blnIsOpen)
				{
					throw new ObjectDisposedException(BitStreamResources.GetString("ObjectDisposed_BitStreamClosed"));
				}
				return (long)_auiBitBuffer.Length << 5;
			}
		}

		public override long Position
		{
			get
			{
				if (!_blnIsOpen)
				{
					throw new ObjectDisposedException(BitStreamResources.GetString("ObjectDisposed_BitStreamClosed"));
				}
				uint num = (_uiBitBuffer_Index << 5) + _uiBitBuffer_BitIndex;
				return num;
			}
			set
			{
				if (!_blnIsOpen)
				{
					throw new ObjectDisposedException(BitStreamResources.GetString("ObjectDisposed_BitStreamClosed"));
				}
				if (value < 0)
				{
					throw new ArgumentOutOfRangeException("value", BitStreamResources.GetString("ArgumentOutOfRange_NegativePosition"));
				}
				uint num = (uint)value;
				if (_uiBitBuffer_Length < num + 1)
				{
					throw new ArgumentOutOfRangeException("value", BitStreamResources.GetString("ArgumentOutOfRange_InvalidPosition"));
				}
				_uiBitBuffer_Index = num >> 5;
				if ((num & 0x1F) != 0)
				{
					_uiBitBuffer_BitIndex = (num & 0x1F);
				}
				else
				{
					_uiBitBuffer_BitIndex = 0u;
				}
			}
		}

		public override bool CanRead => _blnIsOpen;

		public override bool CanSeek => false;

		public override bool CanWrite => _blnIsOpen;

		public static bool CanSetLength => false;

		public static bool CanFlush => false;

		public BitStream()
		{
			_auiBitBuffer = new uint[1];
		}

		public BitStream(long capacity)
		{
			if (capacity <= 0)
			{
				throw new ArgumentOutOfRangeException(BitStreamResources.GetString("ArgumentOutOfRange_NegativeOrZeroCapacity"));
			}
			_auiBitBuffer = new uint[(capacity >> 5) + (((capacity & 0x1F) > 0) ? 1 : 0)];
		}

		public BitStream(byte[] bits)
			: this()
		{
			if (bits == null)
			{
				throw new ArgumentNullException("bits", BitStreamResources.GetString("ArgumentNull_BitBuffer"));
			}
			Write(bits, 0, bits.Length);
		}

		public BitStream(Stream bits)
			: this()
		{
			if (bits == null)
			{
				throw new ArgumentNullException("bits", BitStreamResources.GetString("ArgumentNull_BitBuffer"));
			}
			byte[] buffer = new byte[bits.Length];
			long position = bits.Position;
			bits.Position = 0L;
			bits.Read(buffer, 0, (int)bits.Length);
			bits.Position = position;
			Write(buffer, 0, (int)bits.Length);
		}

		private void Write(ref uint bits, ref uint bitIndex, ref uint count)
		{
			uint num = (_uiBitBuffer_Index << 5) + _uiBitBuffer_BitIndex;
			uint num2 = _uiBitBuffer_Length >> 5;
			uint num3 = bitIndex + count;
			int num4 = (int)bitIndex;
			uint num5 = BitMaskHelperLUT[count] << num4;
			bits &= num5;
			uint num6 = 32 - _uiBitBuffer_BitIndex;
			num4 = (int)(num6 - num3);
			uint num7 = 0u;
			num7 = ((num4 >= 0) ? (bits << num4) : (bits >> Math.Abs(num4)));
			if (_uiBitBuffer_Length >= num + 1)
			{
				int num8 = (int)(num6 - count);
				uint num9 = 0u;
				num9 = (uint)((num8 >= 0) ? (-1 ^ (int)(BitMaskHelperLUT[count] << num8)) : (-1 ^ (int)(BitMaskHelperLUT[count] >> Math.Abs(num8))));
				_auiBitBuffer[_uiBitBuffer_Index] &= num9;
				if (num2 == _uiBitBuffer_Index)
				{
					uint num10 = 0u;
					num10 = ((num6 < count) ? (num + num6) : (num + count));
					if (num10 > _uiBitBuffer_Length)
					{
						uint bits2 = num10 - _uiBitBuffer_Length;
						UpdateLengthForWrite(bits2);
					}
				}
			}
			else if (num6 >= count)
			{
				UpdateLengthForWrite(count);
			}
			else
			{
				UpdateLengthForWrite(num6);
			}
			_auiBitBuffer[_uiBitBuffer_Index] |= num7;
			if (num6 >= count)
			{
				UpdateIndicesForWrite(count);
				return;
			}
			UpdateIndicesForWrite(num6);
			uint count2 = count - num6;
			uint bitIndex2 = bitIndex;
			Write(ref bits, ref bitIndex2, ref count2);
		}

		public virtual void Write(bool bit)
		{
			if (!_blnIsOpen)
			{
				throw new ObjectDisposedException(BitStreamResources.GetString("ObjectDisposed_BitStreamClosed"));
			}
			uint bits = bit ? 1u : 0u;
			uint bitIndex = 0u;
			uint count = 1u;
			Write(ref bits, ref bitIndex, ref count);
		}

		public virtual void Write(bool[] bits)
		{
			if (!_blnIsOpen)
			{
				throw new ObjectDisposedException(BitStreamResources.GetString("ObjectDisposed_BitStreamClosed"));
			}
			if (bits == null)
			{
				throw new ArgumentNullException("bits", BitStreamResources.GetString("ArgumentNull_BitBuffer"));
			}
			Write(bits, 0, bits.Length);
		}

		public virtual void Write(bool[] bits, int offset, int count)
		{
			if (!_blnIsOpen)
			{
				throw new ObjectDisposedException(BitStreamResources.GetString("ObjectDisposed_BitStreamClosed"));
			}
			if (bits == null)
			{
				throw new ArgumentNullException("bits", BitStreamResources.GetString("ArgumentNull_BitBuffer"));
			}
			if (offset < 0)
			{
				throw new ArgumentOutOfRangeException("offset", BitStreamResources.GetString("ArgumentOutOfRange_NegativeParameter"));
			}
			if (count < 0)
			{
				throw new ArgumentOutOfRangeException("count", BitStreamResources.GetString("ArgumentOutOfRange_NegativeParameter"));
			}
			if (count > bits.Length - offset)
			{
				throw new ArgumentException(BitStreamResources.GetString("Argument_InvalidCountOrOffset"));
			}
			int num = offset + count;
			for (int i = offset; i < num; i++)
			{
				Write(bits[i]);
			}
		}

		public virtual void Write(byte bits)
		{
			Write(bits, 0, 8);
		}

		public virtual void Write(byte bits, int bitIndex, int count)
		{
			if (!_blnIsOpen)
			{
				throw new ObjectDisposedException(BitStreamResources.GetString("ObjectDisposed_BitStreamClosed"));
			}
			if (bitIndex < 0)
			{
				throw new ArgumentOutOfRangeException("bitIndex", BitStreamResources.GetString("ArgumentOutOfRange_NegativeParameter"));
			}
			if (count < 0)
			{
				throw new ArgumentOutOfRangeException("count", BitStreamResources.GetString("ArgumentOutOfRange_NegativeParameter"));
			}
			if (count > 8 - bitIndex)
			{
				throw new ArgumentException(BitStreamResources.GetString("Argument_InvalidCountOrBitIndex_Byte"));
			}
			uint bits2 = bits;
			uint bitIndex2 = (uint)bitIndex;
			uint count2 = (uint)count;
			Write(ref bits2, ref bitIndex2, ref count2);
		}

		public virtual void Write(byte[] bits)
		{
			if (!_blnIsOpen)
			{
				throw new ObjectDisposedException(BitStreamResources.GetString("ObjectDisposed_BitStreamClosed"));
			}
			if (bits == null)
			{
				throw new ArgumentNullException("bits", BitStreamResources.GetString("ArgumentNull_BitBuffer"));
			}
			Write(bits, 0, bits.Length);
		}

		public override void Write(byte[] bits, int offset, int count)
		{
			if (!_blnIsOpen)
			{
				throw new ObjectDisposedException(BitStreamResources.GetString("ObjectDisposed_BitStreamClosed"));
			}
			if (bits == null)
			{
				throw new ArgumentNullException("bits", BitStreamResources.GetString("ArgumentNull_BitBuffer"));
			}
			if (offset < 0)
			{
				throw new ArgumentOutOfRangeException("offset", BitStreamResources.GetString("ArgumentOutOfRange_NegativeParameter"));
			}
			if (count < 0)
			{
				throw new ArgumentOutOfRangeException("count", BitStreamResources.GetString("ArgumentOutOfRange_NegativeParameter"));
			}
			if (count > bits.Length - offset)
			{
				throw new ArgumentException(BitStreamResources.GetString("Argument_InvalidCountOrOffset"));
			}
			int num = offset + count;
			for (int i = offset; i < num; i++)
			{
				Write(bits[i]);
			}
		}

		public virtual void Write(sbyte bits)
		{
			Write(bits, 0, 8);
		}

		public virtual void Write(sbyte bits, int bitIndex, int count)
		{
			byte bits2 = (byte)bits;
			Write(bits2, bitIndex, count);
		}

		public virtual void Write(sbyte[] bits)
		{
			if (!_blnIsOpen)
			{
				throw new ObjectDisposedException(BitStreamResources.GetString("ObjectDisposed_BitStreamClosed"));
			}
			if (bits == null)
			{
				throw new ArgumentNullException("bits", BitStreamResources.GetString("ArgumentNull_BitBuffer"));
			}
			Write(bits, 0, bits.Length);
		}

		public virtual void Write(sbyte[] bits, int offset, int count)
		{
			if (!_blnIsOpen)
			{
				throw new ObjectDisposedException(BitStreamResources.GetString("ObjectDisposed_BitStreamClosed"));
			}
			if (bits == null)
			{
				throw new ArgumentNullException("bits", BitStreamResources.GetString("ArgumentNull_BitBuffer"));
			}
			if (offset < 0)
			{
				throw new ArgumentOutOfRangeException("offset", BitStreamResources.GetString("ArgumentOutOfRange_NegativeParameter"));
			}
			if (count < 0)
			{
				throw new ArgumentOutOfRangeException("count", BitStreamResources.GetString("ArgumentOutOfRange_NegativeParameter"));
			}
			if (count > bits.Length - offset)
			{
				throw new ArgumentException(BitStreamResources.GetString("Argument_InvalidCountOrOffset"));
			}
			byte[] array = new byte[count];
			Buffer.BlockCopy(bits, offset, array, 0, count);
			Write(array, 0, count);
		}

		public override void WriteByte(byte value)
		{
			Write(value);
		}

		public virtual void Write(char bits)
		{
			Write(bits, 0, 128);
		}

		public virtual void Write(char bits, int bitIndex, int count)
		{
			if (!_blnIsOpen)
			{
				throw new ObjectDisposedException(BitStreamResources.GetString("ObjectDisposed_BitStreamClosed"));
			}
			if (bitIndex < 0)
			{
				throw new ArgumentOutOfRangeException("bitIndex", BitStreamResources.GetString("ArgumentOutOfRange_NegativeParameter"));
			}
			if (count < 0)
			{
				throw new ArgumentOutOfRangeException("count", BitStreamResources.GetString("ArgumentOutOfRange_NegativeParameter"));
			}
			if (count > 128 - bitIndex)
			{
				throw new ArgumentException(BitStreamResources.GetString("Argument_InvalidCountOrBitIndex_Char"));
			}
			uint bits2 = bits;
			uint bitIndex2 = (uint)bitIndex;
			uint count2 = (uint)count;
			Write(ref bits2, ref bitIndex2, ref count2);
		}

		public virtual void Write(char[] bits)
		{
			if (!_blnIsOpen)
			{
				throw new ObjectDisposedException(BitStreamResources.GetString("ObjectDisposed_BitStreamClosed"));
			}
			if (bits == null)
			{
				throw new ArgumentNullException("bits", BitStreamResources.GetString("ArgumentNull_BitBuffer"));
			}
			Write(bits, 0, bits.Length);
		}

		public virtual void Write(char[] bits, int offset, int count)
		{
			if (!_blnIsOpen)
			{
				throw new ObjectDisposedException(BitStreamResources.GetString("ObjectDisposed_BitStreamClosed"));
			}
			if (bits == null)
			{
				throw new ArgumentNullException("bits", BitStreamResources.GetString("ArgumentNull_BitBuffer"));
			}
			if (offset < 0)
			{
				throw new ArgumentOutOfRangeException("offset", BitStreamResources.GetString("ArgumentOutOfRange_NegativeParameter"));
			}
			if (count < 0)
			{
				throw new ArgumentOutOfRangeException("count", BitStreamResources.GetString("ArgumentOutOfRange_NegativeParameter"));
			}
			if (count > bits.Length - offset)
			{
				throw new ArgumentException(BitStreamResources.GetString("Argument_InvalidCountOrOffset"));
			}
			int num = offset + count;
			for (int i = offset; i < num; i++)
			{
				Write(bits[i]);
			}
		}

		public virtual void Write(ushort bits)
		{
			Write(bits, 0, 16);
		}

		
		public virtual void Write(ushort bits, int bitIndex, int count)
		{
			if (!_blnIsOpen)
			{
				throw new ObjectDisposedException(BitStreamResources.GetString("ObjectDisposed_BitStreamClosed"));
			}
			if (bitIndex < 0)
			{
				throw new ArgumentOutOfRangeException("bitIndex", BitStreamResources.GetString("ArgumentOutOfRange_NegativeParameter"));
			}
			if (count < 0)
			{
				throw new ArgumentOutOfRangeException("count", BitStreamResources.GetString("ArgumentOutOfRange_NegativeParameter"));
			}
			if (count > 16 - bitIndex)
			{
				throw new ArgumentException(BitStreamResources.GetString("Argument_InvalidCountOrBitIndex_UInt16"));
			}
			uint bits2 = bits;
			uint bitIndex2 = (uint)bitIndex;
			uint count2 = (uint)count;
			Write(ref bits2, ref bitIndex2, ref count2);
		}

		
		public virtual void Write(ushort[] bits)
		{
			if (!_blnIsOpen)
			{
				throw new ObjectDisposedException(BitStreamResources.GetString("ObjectDisposed_BitStreamClosed"));
			}
			if (bits == null)
			{
				throw new ArgumentNullException("bits", BitStreamResources.GetString("ArgumentNull_BitBuffer"));
			}
			Write(bits, 0, bits.Length);
		}

		
		public virtual void Write(ushort[] bits, int offset, int count)
		{
			if (!_blnIsOpen)
			{
				throw new ObjectDisposedException(BitStreamResources.GetString("ObjectDisposed_BitStreamClosed"));
			}
			if (bits == null)
			{
				throw new ArgumentNullException("bits", BitStreamResources.GetString("ArgumentNull_BitBuffer"));
			}
			if (offset < 0)
			{
				throw new ArgumentOutOfRangeException("offset", BitStreamResources.GetString("ArgumentOutOfRange_NegativeParameter"));
			}
			if (count < 0)
			{
				throw new ArgumentOutOfRangeException("count", BitStreamResources.GetString("ArgumentOutOfRange_NegativeParameter"));
			}
			if (count > bits.Length - offset)
			{
				throw new ArgumentException(BitStreamResources.GetString("Argument_InvalidCountOrOffset"));
			}
			int num = offset + count;
			for (int i = offset; i < num; i++)
			{
				Write(bits[i]);
			}
		}

		public virtual void Write(short bits)
		{
			Write(bits, 0, 16);
		}

		public virtual void Write(short bits, int bitIndex, int count)
		{
			ushort bits2 = (ushort)bits;
			Write(bits2, bitIndex, count);
		}

		public virtual void Write(short[] bits)
		{
			if (!_blnIsOpen)
			{
				throw new ObjectDisposedException(BitStreamResources.GetString("ObjectDisposed_BitStreamClosed"));
			}
			if (bits == null)
			{
				throw new ArgumentNullException("bits", BitStreamResources.GetString("ArgumentNull_BitBuffer"));
			}
			Write(bits, 0, bits.Length);
		}

		public virtual void Write(short[] bits, int offset, int count)
		{
			if (!_blnIsOpen)
			{
				throw new ObjectDisposedException(BitStreamResources.GetString("ObjectDisposed_BitStreamClosed"));
			}
			if (bits == null)
			{
				throw new ArgumentNullException("bits", BitStreamResources.GetString("ArgumentNull_BitBuffer"));
			}
			if (offset < 0)
			{
				throw new ArgumentOutOfRangeException("offset", BitStreamResources.GetString("ArgumentOutOfRange_NegativeParameter"));
			}
			if (count < 0)
			{
				throw new ArgumentOutOfRangeException("count", BitStreamResources.GetString("ArgumentOutOfRange_NegativeParameter"));
			}
			if (count > bits.Length - offset)
			{
				throw new ArgumentException(BitStreamResources.GetString("Argument_InvalidCountOrOffset"));
			}
			ushort[] array = new ushort[count];
			Buffer.BlockCopy(bits, offset << 1, array, 0, count << 1);
			Write(array, 0, count);
		}

		
		public virtual void Write(uint bits)
		{
			Write(bits, 0, 32);
		}

		
		public virtual void Write(uint bits, int bitIndex, int count)
		{
			if (!_blnIsOpen)
			{
				throw new ObjectDisposedException(BitStreamResources.GetString("ObjectDisposed_BitStreamClosed"));
			}
			if (bitIndex < 0)
			{
				throw new ArgumentOutOfRangeException("bitIndex", BitStreamResources.GetString("ArgumentOutOfRange_NegativeParameter"));
			}
			if (count < 0)
			{
				throw new ArgumentOutOfRangeException("count", BitStreamResources.GetString("ArgumentOutOfRange_NegativeParameter"));
			}
			if (count > 32 - bitIndex)
			{
				throw new ArgumentException(BitStreamResources.GetString("Argument_InvalidCountOrBitIndex_UInt32"));
			}
			uint bitIndex2 = (uint)bitIndex;
			uint count2 = (uint)count;
			Write(ref bits, ref bitIndex2, ref count2);
		}

		
		public virtual void Write(uint[] bits)
		{
			if (!_blnIsOpen)
			{
				throw new ObjectDisposedException(BitStreamResources.GetString("ObjectDisposed_BitStreamClosed"));
			}
			if (bits == null)
			{
				throw new ArgumentNullException("bits", BitStreamResources.GetString("ArgumentNull_BitBuffer"));
			}
			Write(bits, 0, bits.Length);
		}

		
		public virtual void Write(uint[] bits, int offset, int count)
		{
			if (!_blnIsOpen)
			{
				throw new ObjectDisposedException(BitStreamResources.GetString("ObjectDisposed_BitStreamClosed"));
			}
			if (bits == null)
			{
				throw new ArgumentNullException("bits", BitStreamResources.GetString("ArgumentNull_BitBuffer"));
			}
			if (offset < 0)
			{
				throw new ArgumentOutOfRangeException("offset", BitStreamResources.GetString("ArgumentOutOfRange_NegativeParameter"));
			}
			if (count < 0)
			{
				throw new ArgumentOutOfRangeException("count", BitStreamResources.GetString("ArgumentOutOfRange_NegativeParameter"));
			}
			if (count > bits.Length - offset)
			{
				throw new ArgumentException(BitStreamResources.GetString("Argument_InvalidCountOrOffset"));
			}
			int num = offset + count;
			for (int i = offset; i < num; i++)
			{
				Write(bits[i]);
			}
		}

		public virtual void Write(int bits)
		{
			Write(bits, 0, 32);
		}

		public virtual void Write(int bits, int bitIndex, int count)
		{
			Write((uint)bits, bitIndex, count);
		}

		public virtual void Write(int[] bits)
		{
			if (!_blnIsOpen)
			{
				throw new ObjectDisposedException(BitStreamResources.GetString("ObjectDisposed_BitStreamClosed"));
			}
			if (bits == null)
			{
				throw new ArgumentNullException("bits", BitStreamResources.GetString("ArgumentNull_BitBuffer"));
			}
			Write(bits, 0, bits.Length);
		}

		public virtual void Write(int[] bits, int offset, int count)
		{
			if (!_blnIsOpen)
			{
				throw new ObjectDisposedException(BitStreamResources.GetString("ObjectDisposed_BitStreamClosed"));
			}
			if (bits == null)
			{
				throw new ArgumentNullException("bits", BitStreamResources.GetString("ArgumentNull_BitBuffer"));
			}
			if (offset < 0)
			{
				throw new ArgumentOutOfRangeException("offset", BitStreamResources.GetString("ArgumentOutOfRange_NegativeParameter"));
			}
			if (count < 0)
			{
				throw new ArgumentOutOfRangeException("count", BitStreamResources.GetString("ArgumentOutOfRange_NegativeParameter"));
			}
			if (count > bits.Length - offset)
			{
				throw new ArgumentException(BitStreamResources.GetString("Argument_InvalidCountOrOffset"));
			}
			uint[] array = new uint[count];
			Buffer.BlockCopy(bits, offset << 2, array, 0, count << 2);
			Write(array, 0, count);
		}

		public virtual void Write(float bits)
		{
			Write(bits, 0, 32);
		}

		public virtual void Write(float bits, int bitIndex, int count)
		{
			byte[] bytes = BitConverter.GetBytes(bits);
			uint bits2 = (uint)(bytes[0] | (bytes[1] << 8) | (bytes[2] << 16) | (bytes[3] << 24));
			Write(bits2, bitIndex, count);
		}

		public virtual void Write(float[] bits)
		{
			if (!_blnIsOpen)
			{
				throw new ObjectDisposedException(BitStreamResources.GetString("ObjectDisposed_BitStreamClosed"));
			}
			if (bits == null)
			{
				throw new ArgumentNullException("bits", BitStreamResources.GetString("ArgumentNull_BitBuffer"));
			}
			Write(bits, 0, bits.Length);
		}

		public virtual void Write(float[] bits, int offset, int count)
		{
			if (!_blnIsOpen)
			{
				throw new ObjectDisposedException(BitStreamResources.GetString("ObjectDisposed_BitStreamClosed"));
			}
			if (bits == null)
			{
				throw new ArgumentNullException("bits", BitStreamResources.GetString("ArgumentNull_BitBuffer"));
			}
			if (offset < 0)
			{
				throw new ArgumentOutOfRangeException("offset", BitStreamResources.GetString("ArgumentOutOfRange_NegativeParameter"));
			}
			if (count < 0)
			{
				throw new ArgumentOutOfRangeException("count", BitStreamResources.GetString("ArgumentOutOfRange_NegativeParameter"));
			}
			if (count > bits.Length - offset)
			{
				throw new ArgumentException(BitStreamResources.GetString("Argument_InvalidCountOrOffset"));
			}
			int num = offset + count;
			for (int i = offset; i < num; i++)
			{
				Write(bits[i]);
			}
		}

		
		public virtual void Write(ulong bits)
		{
			Write(bits, 0, 64);
		}

		
		public virtual void Write(ulong bits, int bitIndex, int count)
		{
			if (!_blnIsOpen)
			{
				throw new ObjectDisposedException(BitStreamResources.GetString("ObjectDisposed_BitStreamClosed"));
			}
			if (bitIndex < 0)
			{
				throw new ArgumentOutOfRangeException("bitIndex", BitStreamResources.GetString("ArgumentOutOfRange_NegativeParameter"));
			}
			if (count < 0)
			{
				throw new ArgumentOutOfRangeException("count", BitStreamResources.GetString("ArgumentOutOfRange_NegativeParameter"));
			}
			if (count > 64 - bitIndex)
			{
				throw new ArgumentException(BitStreamResources.GetString("Argument_InvalidCountOrBitIndex_UInt64"));
			}
			int num = (bitIndex >> 5 < 1) ? bitIndex : (-1);
			int num2 = (bitIndex + count <= 32) ? (-1) : ((num < 0) ? (bitIndex - 32) : 0);
			int num3 = (num > -1) ? ((num + count > 32) ? (32 - num) : count) : 0;
			int num4 = (num2 > -1) ? ((num3 == 0) ? count : (count - num3)) : 0;
			if (num3 > 0)
			{
				uint bits2 = (uint)bits;
				uint bitIndex2 = (uint)num;
				uint count2 = (uint)num3;
				Write(ref bits2, ref bitIndex2, ref count2);
			}
			if (num4 > 0)
			{
				uint bits3 = (uint)(bits >> 32);
				uint bitIndex3 = (uint)num2;
				uint count3 = (uint)num4;
				Write(ref bits3, ref bitIndex3, ref count3);
			}
		}

		
		public virtual void Write(ulong[] bits)
		{
			if (!_blnIsOpen)
			{
				throw new ObjectDisposedException(BitStreamResources.GetString("ObjectDisposed_BitStreamClosed"));
			}
			if (bits == null)
			{
				throw new ArgumentNullException("bits", BitStreamResources.GetString("ArgumentNull_BitBuffer"));
			}
			Write(bits, 0, bits.Length);
		}

		
		public virtual void Write(ulong[] bits, int offset, int count)
		{
			if (!_blnIsOpen)
			{
				throw new ObjectDisposedException(BitStreamResources.GetString("ObjectDisposed_BitStreamClosed"));
			}
			if (bits == null)
			{
				throw new ArgumentNullException("bits", BitStreamResources.GetString("ArgumentNull_BitBuffer"));
			}
			if (offset < 0)
			{
				throw new ArgumentOutOfRangeException("offset", BitStreamResources.GetString("ArgumentOutOfRange_NegativeParameter"));
			}
			if (count < 0)
			{
				throw new ArgumentOutOfRangeException("count", BitStreamResources.GetString("ArgumentOutOfRange_NegativeParameter"));
			}
			if (count > bits.Length - offset)
			{
				throw new ArgumentException(BitStreamResources.GetString("Argument_InvalidCountOrOffset"));
			}
			int num = offset + count;
			for (int i = offset; i < num; i++)
			{
				Write(bits[i]);
			}
		}

		public virtual void Write(long bits)
		{
			Write(bits, 0, 64);
		}

		public virtual void Write(long bits, int bitIndex, int count)
		{
			Write((ulong)bits, bitIndex, count);
		}

		public virtual void Write(long[] bits)
		{
			if (!_blnIsOpen)
			{
				throw new ObjectDisposedException(BitStreamResources.GetString("ObjectDisposed_BitStreamClosed"));
			}
			if (bits == null)
			{
				throw new ArgumentNullException("bits", BitStreamResources.GetString("ArgumentNull_BitBuffer"));
			}
			Write(bits, 0, bits.Length);
		}

		public virtual void Write(long[] bits, int offset, int count)
		{
			if (!_blnIsOpen)
			{
				throw new ObjectDisposedException(BitStreamResources.GetString("ObjectDisposed_BitStreamClosed"));
			}
			if (bits == null)
			{
				throw new ArgumentNullException("bits", BitStreamResources.GetString("ArgumentNull_BitBuffer"));
			}
			if (offset < 0)
			{
				throw new ArgumentOutOfRangeException("offset", BitStreamResources.GetString("ArgumentOutOfRange_NegativeParameter"));
			}
			if (count < 0)
			{
				throw new ArgumentOutOfRangeException("count", BitStreamResources.GetString("ArgumentOutOfRange_NegativeParameter"));
			}
			if (count > bits.Length - offset)
			{
				throw new ArgumentException(BitStreamResources.GetString("Argument_InvalidCountOrOffset"));
			}
			ulong[] array = new ulong[count];
			Buffer.BlockCopy(bits, offset << 4, array, 0, count << 4);
			Write(array, 0, count);
		}

		public virtual void Write(double bits)
		{
			Write(bits, 0, 64);
		}

		public virtual void Write(double bits, int bitIndex, int count)
		{
			byte[] bytes = BitConverter.GetBytes(bits);
			ulong bits2 = bytes[0] | ((ulong)bytes[1] << 8) | ((ulong)bytes[2] << 16) | ((ulong)bytes[3] << 24) | ((ulong)bytes[4] << 32) | ((ulong)bytes[5] << 40) | ((ulong)bytes[6] << 48) | ((ulong)bytes[7] << 56);
			Write(bits2, bitIndex, count);
		}

		public virtual void Write(double[] bits)
		{
			if (!_blnIsOpen)
			{
				throw new ObjectDisposedException(BitStreamResources.GetString("ObjectDisposed_BitStreamClosed"));
			}
			if (bits == null)
			{
				throw new ArgumentNullException("bits", BitStreamResources.GetString("ArgumentNull_BitBuffer"));
			}
			Write(bits, 0, bits.Length);
		}

		public virtual void Write(double[] bits, int offset, int count)
		{
			if (!_blnIsOpen)
			{
				throw new ObjectDisposedException(BitStreamResources.GetString("ObjectDisposed_BitStreamClosed"));
			}
			if (bits == null)
			{
				throw new ArgumentNullException("bits", BitStreamResources.GetString("ArgumentNull_BitBuffer"));
			}
			if (offset < 0)
			{
				throw new ArgumentOutOfRangeException("offset", BitStreamResources.GetString("ArgumentOutOfRange_NegativeParameter"));
			}
			if (count < 0)
			{
				throw new ArgumentOutOfRangeException("count", BitStreamResources.GetString("ArgumentOutOfRange_NegativeParameter"));
			}
			if (count > bits.Length - offset)
			{
				throw new ArgumentException(BitStreamResources.GetString("Argument_InvalidCountOrOffset"));
			}
			int num = offset + count;
			for (int i = offset; i < num; i++)
			{
				Write(bits[i]);
			}
		}

		public virtual void WriteTo(Stream bits)
		{
			if (!_blnIsOpen)
			{
				throw new ObjectDisposedException(BitStreamResources.GetString("ObjectDisposed_BitStreamClosed"));
			}
			if (bits == null)
			{
				throw new ArgumentNullException("bits", BitStreamResources.GetString("ArgumentNull_Stream"));
			}
			byte[] array = ToByteArray();
			bits.Write(array, 0, array.Length);
		}

		private uint Read(ref uint bits, ref uint bitIndex, ref uint count)
		{
			uint num = (_uiBitBuffer_Index << 5) + _uiBitBuffer_BitIndex;
			uint num2 = count;
			if (_uiBitBuffer_Length < num + num2)
			{
				num2 = _uiBitBuffer_Length - num;
			}
			uint num3 = _auiBitBuffer[_uiBitBuffer_Index];
			int num4 = (int)(32 - (_uiBitBuffer_BitIndex + num2));
			if (num4 < 0)
			{
				num4 = Math.Abs(num4);
				uint num5 = BitMaskHelperLUT[num2] >> num4;
				num3 &= num5;
				num3 <<= num4;
				uint count2 = (uint)num4;
				uint bitIndex2 = 0u;
				uint bits2 = 0u;
				UpdateIndicesForRead(num2 - count2);
				Read(ref bits2, ref bitIndex2, ref count2);
				num3 |= bits2;
			}
			else
			{
				uint num5 = BitMaskHelperLUT[num2] << num4;
				num3 &= num5;
				num3 >>= num4;
				UpdateIndicesForRead(num2);
			}
			bits = num3 << (int)bitIndex;
			return num2;
		}

		public virtual int Read(out bool bit)
		{
			if (!_blnIsOpen)
			{
				throw new ObjectDisposedException(BitStreamResources.GetString("ObjectDisposed_BitStreamClosed"));
			}
			uint bitIndex = 0u;
			uint count = 1u;
			uint bits = 0u;
			uint result = Read(ref bits, ref bitIndex, ref count);
			bit = Convert.ToBoolean(bits);
			return (int)result;
		}

		public virtual int Read(bool[] bits)
		{
			if (!_blnIsOpen)
			{
				throw new ObjectDisposedException(BitStreamResources.GetString("ObjectDisposed_BitStreamClosed"));
			}
			if (bits == null)
			{
				throw new ArgumentNullException("bits", BitStreamResources.GetString("ArgumentNull_BitBuffer"));
			}
			return Read(bits, 0, bits.Length);
		}

		public virtual int Read(bool[] bits, int offset, int count)
		{
			if (!_blnIsOpen)
			{
				throw new ObjectDisposedException(BitStreamResources.GetString("ObjectDisposed_BitStreamClosed"));
			}
			if (bits == null)
			{
				throw new ArgumentNullException("bits", BitStreamResources.GetString("ArgumentNull_BitBuffer"));
			}
			if (offset < 0)
			{
				throw new ArgumentOutOfRangeException("offset", BitStreamResources.GetString("ArgumentOutOfRange_NegativeParameter"));
			}
			if (count < 0)
			{
				throw new ArgumentOutOfRangeException("count", BitStreamResources.GetString("ArgumentOutOfRange_NegativeParameter"));
			}
			if (count > bits.Length - offset)
			{
				throw new ArgumentException(BitStreamResources.GetString("Argument_InvalidCountOrOffset"));
			}
			int num = offset + count;
			int num2 = 0;
			for (int i = offset; i < num; i++)
			{
				num2 += Read(out bits[i]);
			}
			return num2;
		}

		public virtual int Read(out byte bits)
		{
			return Read(out bits, 0, 8);
		}

		public virtual int Read(out byte bits, int bitIndex, int count)
		{
			if (!_blnIsOpen)
			{
				throw new ObjectDisposedException(BitStreamResources.GetString("ObjectDisposed_BitStreamClosed"));
			}
			if (bitIndex < 0)
			{
				throw new ArgumentOutOfRangeException("bitIndex", BitStreamResources.GetString("ArgumentOutOfRange_NegativeParameter"));
			}
			if (count < 0)
			{
				throw new ArgumentOutOfRangeException("count", BitStreamResources.GetString("ArgumentOutOfRange_NegativeParameter"));
			}
			if (count > 8 - bitIndex)
			{
				throw new ArgumentException(BitStreamResources.GetString("Argument_InvalidCountOrBitIndex_Byte"));
			}
			uint bitIndex2 = (uint)bitIndex;
			uint count2 = (uint)count;
			uint bits2 = 0u;
			uint result = Read(ref bits2, ref bitIndex2, ref count2);
			bits = (byte)bits2;
			return (int)result;
		}

		public virtual int Read(byte[] bits)
		{
			if (!_blnIsOpen)
			{
				throw new ObjectDisposedException(BitStreamResources.GetString("ObjectDisposed_BitStreamClosed"));
			}
			if (bits == null)
			{
				throw new ArgumentNullException("bits", BitStreamResources.GetString("ArgumentNull_BitBuffer"));
			}
			return Read(bits, 0, bits.Length);
		}

		public override int Read(byte[] bits, int offset, int count)
		{
			if (!_blnIsOpen)
			{
				throw new ObjectDisposedException(BitStreamResources.GetString("ObjectDisposed_BitStreamClosed"));
			}
			if (bits == null)
			{
				throw new ArgumentNullException("bits", BitStreamResources.GetString("ArgumentNull_BitBuffer"));
			}
			if (offset < 0)
			{
				throw new ArgumentOutOfRangeException("offset", BitStreamResources.GetString("ArgumentOutOfRange_NegativeParameter"));
			}
			if (count < 0)
			{
				throw new ArgumentOutOfRangeException("count", BitStreamResources.GetString("ArgumentOutOfRange_NegativeParameter"));
			}
			if (count > bits.Length - offset)
			{
				throw new ArgumentException(BitStreamResources.GetString("Argument_InvalidCountOrOffset"));
			}
			int num = offset + count;
			int num2 = 0;
			for (int i = offset; i < num; i++)
			{
				num2 += Read(out bits[i]);
			}
			return num2;
		}

		
		public virtual int Read(out sbyte bits)
		{
			return Read(out bits, 0, 8);
		}

		
		public virtual int Read(out sbyte bits, int bitIndex, int count)
		{
			byte bits2 = 0;
			int result = Read(out bits2, bitIndex, count);
			bits = (sbyte)bits2;
			return result;
		}

		
		public virtual int Read(sbyte[] bits)
		{
			if (!_blnIsOpen)
			{
				throw new ObjectDisposedException(BitStreamResources.GetString("ObjectDisposed_BitStreamClosed"));
			}
			if (bits == null)
			{
				throw new ArgumentNullException("bits", BitStreamResources.GetString("ArgumentNull_BitBuffer"));
			}
			return Read(bits, 0, bits.Length);
		}

		
		public virtual int Read(sbyte[] bits, int offset, int count)
		{
			if (!_blnIsOpen)
			{
				throw new ObjectDisposedException(BitStreamResources.GetString("ObjectDisposed_BitStreamClosed"));
			}
			if (bits == null)
			{
				throw new ArgumentNullException("bits", BitStreamResources.GetString("ArgumentNull_BitBuffer"));
			}
			if (offset < 0)
			{
				throw new ArgumentOutOfRangeException("offset", BitStreamResources.GetString("ArgumentOutOfRange_NegativeParameter"));
			}
			if (count < 0)
			{
				throw new ArgumentOutOfRangeException("count", BitStreamResources.GetString("ArgumentOutOfRange_NegativeParameter"));
			}
			if (count > bits.Length - offset)
			{
				throw new ArgumentException(BitStreamResources.GetString("Argument_InvalidCountOrOffset"));
			}
			int num = offset + count;
			int num2 = 0;
			for (int i = offset; i < num; i++)
			{
				num2 += Read(out bits[i]);
			}
			return num2;
		}

		public override int ReadByte()
		{
			if (Read(out byte bits) == 0)
			{
				return -1;
			}
			return bits;
		}

		public virtual byte[] ToByteArray()
		{
			long position = Position;
			Position = 0L;
			byte[] array = new byte[Length8];
			Read(array, 0, (int)Length8);
			if (Position != position)
			{
				Position = position;
			}
			return array;
		}

		public virtual int Read(out char bits)
		{
			return Read(out bits, 0, 128);
		}

		public virtual int Read(out char bits, int bitIndex, int count)
		{
			if (!_blnIsOpen)
			{
				throw new ObjectDisposedException(BitStreamResources.GetString("ObjectDisposed_BitStreamClosed"));
			}
			if (bitIndex < 0)
			{
				throw new ArgumentOutOfRangeException("bitIndex", BitStreamResources.GetString("ArgumentOutOfRange_NegativeParameter"));
			}
			if (count < 0)
			{
				throw new ArgumentOutOfRangeException("count", BitStreamResources.GetString("ArgumentOutOfRange_NegativeParameter"));
			}
			if (count > 128 - bitIndex)
			{
				throw new ArgumentException(BitStreamResources.GetString("Argument_InvalidCountOrBitIndex_Char"));
			}
			uint bitIndex2 = (uint)bitIndex;
			uint count2 = (uint)count;
			uint bits2 = 0u;
			uint result = Read(ref bits2, ref bitIndex2, ref count2);
			bits = (char)bits2;
			return (int)result;
		}

		public virtual int Read(char[] bits)
		{
			if (!_blnIsOpen)
			{
				throw new ObjectDisposedException(BitStreamResources.GetString("ObjectDisposed_BitStreamClosed"));
			}
			if (bits == null)
			{
				throw new ArgumentNullException("bits", BitStreamResources.GetString("ArgumentNull_BitBuffer"));
			}
			return Read(bits, 0, bits.Length);
		}

		public virtual int Read(char[] bits, int offset, int count)
		{
			if (!_blnIsOpen)
			{
				throw new ObjectDisposedException(BitStreamResources.GetString("ObjectDisposed_BitStreamClosed"));
			}
			if (bits == null)
			{
				throw new ArgumentNullException("bits", BitStreamResources.GetString("ArgumentNull_BitBuffer"));
			}
			if (offset < 0)
			{
				throw new ArgumentOutOfRangeException("offset", BitStreamResources.GetString("ArgumentOutOfRange_NegativeParameter"));
			}
			if (count < 0)
			{
				throw new ArgumentOutOfRangeException("count", BitStreamResources.GetString("ArgumentOutOfRange_NegativeParameter"));
			}
			if (count > bits.Length - offset)
			{
				throw new ArgumentException(BitStreamResources.GetString("Argument_InvalidCountOrOffset"));
			}
			int num = offset + count;
			int num2 = 0;
			for (int i = offset; i < num; i++)
			{
				num2 += Read(out bits[i]);
			}
			return num2;
		}

		
		public virtual int Read(out ushort bits)
		{
			return Read(out bits, 0, 16);
		}

		
		public virtual int Read(out ushort bits, int bitIndex, int count)
		{
			if (!_blnIsOpen)
			{
				throw new ObjectDisposedException(BitStreamResources.GetString("ObjectDisposed_BitStreamClosed"));
			}
			if (bitIndex < 0)
			{
				throw new ArgumentOutOfRangeException("bitIndex", BitStreamResources.GetString("ArgumentOutOfRange_NegativeParameter"));
			}
			if (count < 0)
			{
				throw new ArgumentOutOfRangeException("count", BitStreamResources.GetString("ArgumentOutOfRange_NegativeParameter"));
			}
			if (count > 16 - bitIndex)
			{
				throw new ArgumentException(BitStreamResources.GetString("Argument_InvalidCountOrBitIndex_UInt16"));
			}
			uint bitIndex2 = (uint)bitIndex;
			uint count2 = (uint)count;
			uint bits2 = 0u;
			uint result = Read(ref bits2, ref bitIndex2, ref count2);
			bits = (ushort)bits2;
			return (int)result;
		}

		
		public virtual int Read(ushort[] bits)
		{
			if (!_blnIsOpen)
			{
				throw new ObjectDisposedException(BitStreamResources.GetString("ObjectDisposed_BitStreamClosed"));
			}
			if (bits == null)
			{
				throw new ArgumentNullException("bits", BitStreamResources.GetString("ArgumentNull_BitBuffer"));
			}
			return Read(bits, 0, bits.Length);
		}

		
		public virtual int Read(ushort[] bits, int offset, int count)
		{
			if (!_blnIsOpen)
			{
				throw new ObjectDisposedException(BitStreamResources.GetString("ObjectDisposed_BitStreamClosed"));
			}
			if (bits == null)
			{
				throw new ArgumentNullException("bits", BitStreamResources.GetString("ArgumentNull_BitBuffer"));
			}
			if (offset < 0)
			{
				throw new ArgumentOutOfRangeException("offset", BitStreamResources.GetString("ArgumentOutOfRange_NegativeParameter"));
			}
			if (count < 0)
			{
				throw new ArgumentOutOfRangeException("count", BitStreamResources.GetString("ArgumentOutOfRange_NegativeParameter"));
			}
			if (count > bits.Length - offset)
			{
				throw new ArgumentException(BitStreamResources.GetString("Argument_InvalidCountOrOffset"));
			}
			int num = offset + count;
			int num2 = 0;
			for (int i = offset; i < num; i++)
			{
				num2 += Read(out bits[i]);
			}
			return num2;
		}

		public virtual int Read(out short bits)
		{
			return Read(out bits, 0, 16);
		}

		public virtual int Read(out short bits, int bitIndex, int count)
		{
			ushort bits2 = 0;
			int result = Read(out bits2, bitIndex, count);
			bits = (short)bits2;
			return result;
		}

		public virtual int Read(short[] bits)
		{
			if (!_blnIsOpen)
			{
				throw new ObjectDisposedException(BitStreamResources.GetString("ObjectDisposed_BitStreamClosed"));
			}
			if (bits == null)
			{
				throw new ArgumentNullException("bits", BitStreamResources.GetString("ArgumentNull_BitBuffer"));
			}
			return Read(bits, 0, bits.Length);
		}

		public virtual int Read(short[] bits, int offset, int count)
		{
			if (!_blnIsOpen)
			{
				throw new ObjectDisposedException(BitStreamResources.GetString("ObjectDisposed_BitStreamClosed"));
			}
			if (bits == null)
			{
				throw new ArgumentNullException("bits", BitStreamResources.GetString("ArgumentNull_BitBuffer"));
			}
			if (offset < 0)
			{
				throw new ArgumentOutOfRangeException("offset", BitStreamResources.GetString("ArgumentOutOfRange_NegativeParameter"));
			}
			if (count < 0)
			{
				throw new ArgumentOutOfRangeException("count", BitStreamResources.GetString("ArgumentOutOfRange_NegativeParameter"));
			}
			if (count > bits.Length - offset)
			{
				throw new ArgumentException(BitStreamResources.GetString("Argument_InvalidCountOrOffset"));
			}
			int num = offset + count;
			int num2 = 0;
			for (int i = offset; i < num; i++)
			{
				num2 += Read(out bits[i]);
			}
			return num2;
		}

		
		public virtual int Read(out uint bits)
		{
			return Read(out bits, 0, 32);
		}

		
		public virtual int Read(out uint bits, int bitIndex, int count)
		{
			if (!_blnIsOpen)
			{
				throw new ObjectDisposedException(BitStreamResources.GetString("ObjectDisposed_BitStreamClosed"));
			}
			if (bitIndex < 0)
			{
				throw new ArgumentOutOfRangeException("bitIndex", BitStreamResources.GetString("ArgumentOutOfRange_NegativeParameter"));
			}
			if (count < 0)
			{
				throw new ArgumentOutOfRangeException("count", BitStreamResources.GetString("ArgumentOutOfRange_NegativeParameter"));
			}
			if (count > 32 - bitIndex)
			{
				throw new ArgumentException(BitStreamResources.GetString("Argument_InvalidCountOrBitIndex_UInt32"));
			}
			uint bitIndex2 = (uint)bitIndex;
			uint count2 = (uint)count;
			uint bits2 = 0u;
			uint result = Read(ref bits2, ref bitIndex2, ref count2);
			bits = bits2;
			return (int)result;
		}

		
		public virtual int Read(uint[] bits)
		{
			if (!_blnIsOpen)
			{
				throw new ObjectDisposedException(BitStreamResources.GetString("ObjectDisposed_BitStreamClosed"));
			}
			if (bits == null)
			{
				throw new ArgumentNullException("bits", BitStreamResources.GetString("ArgumentNull_BitBuffer"));
			}
			return Read(bits, 0, bits.Length);
		}

		
		public virtual int Read(uint[] bits, int offset, int count)
		{
			if (!_blnIsOpen)
			{
				throw new ObjectDisposedException(BitStreamResources.GetString("ObjectDisposed_BitStreamClosed"));
			}
			if (bits == null)
			{
				throw new ArgumentNullException("bits", BitStreamResources.GetString("ArgumentNull_BitBuffer"));
			}
			if (offset < 0)
			{
				throw new ArgumentOutOfRangeException("offset", BitStreamResources.GetString("ArgumentOutOfRange_NegativeParameter"));
			}
			if (count < 0)
			{
				throw new ArgumentOutOfRangeException("count", BitStreamResources.GetString("ArgumentOutOfRange_NegativeParameter"));
			}
			if (count > bits.Length - offset)
			{
				throw new ArgumentException(BitStreamResources.GetString("Argument_InvalidCountOrOffset"));
			}
			int num = offset + count;
			int num2 = 0;
			for (int i = offset; i < num; i++)
			{
				num2 += Read(out bits[i]);
			}
			return num2;
		}

		public virtual int Read(out int bits)
		{
			return Read(out bits, 0, 32);
		}

		public virtual int Read(out int bits, int bitIndex, int count)
		{
			uint bits2 = 0u;
			int result = Read(out bits2, bitIndex, count);
			bits = (int)bits2;
			return result;
		}

		public virtual int Read(int[] bits)
		{
			if (!_blnIsOpen)
			{
				throw new ObjectDisposedException(BitStreamResources.GetString("ObjectDisposed_BitStreamClosed"));
			}
			if (bits == null)
			{
				throw new ArgumentNullException("bits", BitStreamResources.GetString("ArgumentNull_BitBuffer"));
			}
			return Read(bits, 0, bits.Length);
		}

		public virtual int Read(int[] bits, int offset, int count)
		{
			if (!_blnIsOpen)
			{
				throw new ObjectDisposedException(BitStreamResources.GetString("ObjectDisposed_BitStreamClosed"));
			}
			if (bits == null)
			{
				throw new ArgumentNullException("bits", BitStreamResources.GetString("ArgumentNull_BitBuffer"));
			}
			if (offset < 0)
			{
				throw new ArgumentOutOfRangeException("offset", BitStreamResources.GetString("ArgumentOutOfRange_NegativeParameter"));
			}
			if (count < 0)
			{
				throw new ArgumentOutOfRangeException("count", BitStreamResources.GetString("ArgumentOutOfRange_NegativeParameter"));
			}
			if (count > bits.Length - offset)
			{
				throw new ArgumentException(BitStreamResources.GetString("Argument_InvalidCountOrOffset"));
			}
			int num = offset + count;
			int num2 = 0;
			for (int i = offset; i < num; i++)
			{
				num2 += Read(out bits[i]);
			}
			return num2;
		}

		public virtual int Read(out float bits)
		{
			return Read(out bits, 0, 32);
		}

		public virtual int Read(out float bits, int bitIndex, int count)
		{
			int bits2 = 0;
			int result = Read(out bits2, bitIndex, count);
			bits = BitConverter.ToSingle(BitConverter.GetBytes(bits2), 0);
			return result;
		}

		public virtual int Read(float[] bits)
		{
			if (!_blnIsOpen)
			{
				throw new ObjectDisposedException(BitStreamResources.GetString("ObjectDisposed_BitStreamClosed"));
			}
			if (bits == null)
			{
				throw new ArgumentNullException("bits", BitStreamResources.GetString("ArgumentNull_BitBuffer"));
			}
			return Read(bits, 0, bits.Length);
		}

		public virtual int Read(float[] bits, int offset, int count)
		{
			if (!_blnIsOpen)
			{
				throw new ObjectDisposedException(BitStreamResources.GetString("ObjectDisposed_BitStreamClosed"));
			}
			if (bits == null)
			{
				throw new ArgumentNullException("bits", BitStreamResources.GetString("ArgumentNull_BitBuffer"));
			}
			if (offset < 0)
			{
				throw new ArgumentOutOfRangeException("offset", BitStreamResources.GetString("ArgumentOutOfRange_NegativeParameter"));
			}
			if (count < 0)
			{
				throw new ArgumentOutOfRangeException("count", BitStreamResources.GetString("ArgumentOutOfRange_NegativeParameter"));
			}
			if (count > bits.Length - offset)
			{
				throw new ArgumentException(BitStreamResources.GetString("Argument_InvalidCountOrOffset"));
			}
			int num = offset + count;
			int num2 = 0;
			for (int i = offset; i < num; i++)
			{
				num2 += Read(out bits[i]);
			}
			return num2;
		}

		
		public virtual int Read(out ulong bits)
		{
			return Read(out bits, 0, 64);
		}

		
		public virtual int Read(out ulong bits, int bitIndex, int count)
		{
			if (!_blnIsOpen)
			{
				throw new ObjectDisposedException(BitStreamResources.GetString("ObjectDisposed_BitStreamClosed"));
			}
			if (bitIndex < 0)
			{
				throw new ArgumentOutOfRangeException("bitIndex", BitStreamResources.GetString("ArgumentOutOfRange_NegativeParameter"));
			}
			if (count < 0)
			{
				throw new ArgumentOutOfRangeException("count", BitStreamResources.GetString("ArgumentOutOfRange_NegativeParameter"));
			}
			if (count > 64 - bitIndex)
			{
				throw new ArgumentException(BitStreamResources.GetString("Argument_InvalidCountOrBitIndex_UInt64"));
			}
			int num = (bitIndex >> 5 < 1) ? bitIndex : (-1);
			int num2 = (bitIndex + count <= 32) ? (-1) : ((num < 0) ? (bitIndex - 32) : 0);
			int num3 = (num > -1) ? ((num + count > 32) ? (32 - num) : count) : 0;
			int num4 = (num2 > -1) ? ((num3 == 0) ? count : (count - num3)) : 0;
			uint num5 = 0u;
			uint bits2 = 0u;
			uint bits3 = 0u;
			if (num3 > 0)
			{
				uint bitIndex2 = (uint)num;
				uint count2 = (uint)num3;
				num5 = Read(ref bits2, ref bitIndex2, ref count2);
			}
			if (num4 > 0)
			{
				uint bitIndex3 = (uint)num2;
				uint count3 = (uint)num4;
				num5 += Read(ref bits3, ref bitIndex3, ref count3);
			}
			bits = (((ulong)bits3 << 32) | bits2);
			return (int)num5;
		}

		
		public virtual int Read(ulong[] bits)
		{
			if (!_blnIsOpen)
			{
				throw new ObjectDisposedException(BitStreamResources.GetString("ObjectDisposed_BitStreamClosed"));
			}
			if (bits == null)
			{
				throw new ArgumentNullException("bits", BitStreamResources.GetString("ArgumentNull_BitBuffer"));
			}
			return Read(bits, 0, bits.Length);
		}

		
		public virtual int Read(ulong[] bits, int offset, int count)
		{
			if (!_blnIsOpen)
			{
				throw new ObjectDisposedException(BitStreamResources.GetString("ObjectDisposed_BitStreamClosed"));
			}
			if (bits == null)
			{
				throw new ArgumentNullException("bits", BitStreamResources.GetString("ArgumentNull_BitBuffer"));
			}
			if (offset < 0)
			{
				throw new ArgumentOutOfRangeException("offset", BitStreamResources.GetString("ArgumentOutOfRange_NegativeParameter"));
			}
			if (count < 0)
			{
				throw new ArgumentOutOfRangeException("count", BitStreamResources.GetString("ArgumentOutOfRange_NegativeParameter"));
			}
			if (count > bits.Length - offset)
			{
				throw new ArgumentException(BitStreamResources.GetString("Argument_InvalidCountOrOffset"));
			}
			int num = offset + count;
			int num2 = 0;
			for (int i = offset; i < num; i++)
			{
				num2 += Read(out bits[i]);
			}
			return num2;
		}

		public virtual int Read(out long bits)
		{
			return Read(out bits, 0, 64);
		}

		public virtual int Read(out long bits, int bitIndex, int count)
		{
			ulong bits2 = 0uL;
			int result = Read(out bits2, bitIndex, count);
			bits = (long)bits2;
			return result;
		}

		public virtual int Read(long[] bits)
		{
			if (!_blnIsOpen)
			{
				throw new ObjectDisposedException(BitStreamResources.GetString("ObjectDisposed_BitStreamClosed"));
			}
			if (bits == null)
			{
				throw new ArgumentNullException("bits", BitStreamResources.GetString("ArgumentNull_BitBuffer"));
			}
			return Read(bits, 0, bits.Length);
		}

		public virtual int Read(long[] bits, int offset, int count)
		{
			if (!_blnIsOpen)
			{
				throw new ObjectDisposedException(BitStreamResources.GetString("ObjectDisposed_BitStreamClosed"));
			}
			if (bits == null)
			{
				throw new ArgumentNullException("bits", BitStreamResources.GetString("ArgumentNull_BitBuffer"));
			}
			if (offset < 0)
			{
				throw new ArgumentOutOfRangeException("offset", BitStreamResources.GetString("ArgumentOutOfRange_NegativeParameter"));
			}
			if (count < 0)
			{
				throw new ArgumentOutOfRangeException("count", BitStreamResources.GetString("ArgumentOutOfRange_NegativeParameter"));
			}
			if (count > bits.Length - offset)
			{
				throw new ArgumentException(BitStreamResources.GetString("Argument_InvalidCountOrOffset"));
			}
			int num = offset + count;
			int num2 = 0;
			for (int i = offset; i < num; i++)
			{
				num2 += Read(out bits[i]);
			}
			return num2;
		}

		public virtual int Read(out double bits)
		{
			return Read(out bits, 0, 64);
		}

		public virtual int Read(out double bits, int bitIndex, int count)
		{
			ulong bits2 = 0uL;
			int result = Read(out bits2, bitIndex, count);
			bits = BitConverter.ToDouble(BitConverter.GetBytes(bits2), 0);
			return result;
		}

		public virtual int Read(double[] bits)
		{
			if (!_blnIsOpen)
			{
				throw new ObjectDisposedException(BitStreamResources.GetString("ObjectDisposed_BitStreamClosed"));
			}
			if (bits == null)
			{
				throw new ArgumentNullException("bits", BitStreamResources.GetString("ArgumentNull_BitBuffer"));
			}
			return Read(bits, 0, bits.Length);
		}

		public virtual int Read(double[] bits, int offset, int count)
		{
			if (!_blnIsOpen)
			{
				throw new ObjectDisposedException(BitStreamResources.GetString("ObjectDisposed_BitStreamClosed"));
			}
			if (bits == null)
			{
				throw new ArgumentNullException("bits", BitStreamResources.GetString("ArgumentNull_BitBuffer"));
			}
			if (offset < 0)
			{
				throw new ArgumentOutOfRangeException("offset", BitStreamResources.GetString("ArgumentOutOfRange_NegativeParameter"));
			}
			if (count < 0)
			{
				throw new ArgumentOutOfRangeException("count", BitStreamResources.GetString("ArgumentOutOfRange_NegativeParameter"));
			}
			if (count > bits.Length - offset)
			{
				throw new ArgumentException(BitStreamResources.GetString("Argument_InvalidCountOrOffset"));
			}
			int num = offset + count;
			int num2 = 0;
			for (int i = offset; i < num; i++)
			{
				num2 += Read(out bits[i]);
			}
			return num2;
		}

		public virtual BitStream And(BitStream bits)
		{
			if (!_blnIsOpen)
			{
				throw new ObjectDisposedException(BitStreamResources.GetString("ObjectDisposed_BitStreamClosed"));
			}
			if (bits == null)
			{
				throw new ArgumentNullException("bits", BitStreamResources.GetString("ArgumentNull_BitStream"));
			}
			if (bits.Length != _uiBitBuffer_Length)
			{
				throw new ArgumentException(BitStreamResources.GetString("Argument_DifferentBitStreamLengths"));
			}
			BitStream bitStream = new BitStream(_uiBitBuffer_Length);
			uint num = _uiBitBuffer_Length >> 5;
			uint num2 = 0u;
			for (num2 = 0u; num2 < num; num2++)
			{
				bitStream._auiBitBuffer[num2] = (_auiBitBuffer[num2] & bits._auiBitBuffer[num2]);
			}
			if ((_uiBitBuffer_Length & 0x1F) != 0)
			{
				uint num3 = (uint)(-1 << (int)(32 - (_uiBitBuffer_Length & 0x1F)));
				bitStream._auiBitBuffer[num2] = (_auiBitBuffer[num2] & bits._auiBitBuffer[num2] & num3);
			}
			return bitStream;
		}

		public virtual BitStream Or(BitStream bits)
		{
			if (!_blnIsOpen)
			{
				throw new ObjectDisposedException(BitStreamResources.GetString("ObjectDisposed_BitStreamClosed"));
			}
			if (bits == null)
			{
				throw new ArgumentNullException("bits", BitStreamResources.GetString("ArgumentNull_BitStream"));
			}
			if (bits.Length != _uiBitBuffer_Length)
			{
				throw new ArgumentException(BitStreamResources.GetString("Argument_DifferentBitStreamLengths"));
			}
			BitStream bitStream = new BitStream(_uiBitBuffer_Length);
			uint num = _uiBitBuffer_Length >> 5;
			uint num2 = 0u;
			for (num2 = 0u; num2 < num; num2++)
			{
				bitStream._auiBitBuffer[num2] = (_auiBitBuffer[num2] | bits._auiBitBuffer[num2]);
			}
			if ((_uiBitBuffer_Length & 0x1F) != 0)
			{
				uint num3 = (uint)(-1 << (int)(32 - (_uiBitBuffer_Length & 0x1F)));
				bitStream._auiBitBuffer[num2] = (_auiBitBuffer[num2] | (bits._auiBitBuffer[num2] & num3));
			}
			return bitStream;
		}

		public virtual BitStream Xor(BitStream bits)
		{
			if (!_blnIsOpen)
			{
				throw new ObjectDisposedException(BitStreamResources.GetString("ObjectDisposed_BitStreamClosed"));
			}
			if (bits == null)
			{
				throw new ArgumentNullException("bits", BitStreamResources.GetString("ArgumentNull_BitStream"));
			}
			if (bits.Length != _uiBitBuffer_Length)
			{
				throw new ArgumentException(BitStreamResources.GetString("Argument_DifferentBitStreamLengths"));
			}
			BitStream bitStream = new BitStream(_uiBitBuffer_Length);
			uint num = _uiBitBuffer_Length >> 5;
			uint num2 = 0u;
			for (num2 = 0u; num2 < num; num2++)
			{
				bitStream._auiBitBuffer[num2] = (_auiBitBuffer[num2] ^ bits._auiBitBuffer[num2]);
			}
			if ((_uiBitBuffer_Length & 0x1F) != 0)
			{
				uint num3 = (uint)(-1 << (int)(32 - (_uiBitBuffer_Length & 0x1F)));
				bitStream._auiBitBuffer[num2] = (_auiBitBuffer[num2] ^ (bits._auiBitBuffer[num2] & num3));
			}
			return bitStream;
		}

		public virtual BitStream Not()
		{
			if (!_blnIsOpen)
			{
				throw new ObjectDisposedException(BitStreamResources.GetString("ObjectDisposed_BitStreamClosed"));
			}
			BitStream bitStream = new BitStream(_uiBitBuffer_Length);
			uint num = _uiBitBuffer_Length >> 5;
			uint num2 = 0u;
			for (num2 = 0u; num2 < num; num2++)
			{
				bitStream._auiBitBuffer[num2] = ~_auiBitBuffer[num2];
			}
			if ((_uiBitBuffer_Length & 0x1F) != 0)
			{
				uint num3 = (uint)(-1 << (int)(32 - (_uiBitBuffer_Length & 0x1F)));
				bitStream._auiBitBuffer[num2] = (~_auiBitBuffer[num2] & num3);
			}
			return bitStream;
		}

		public virtual BitStream ShiftLeft(long count)
		{
			if (!_blnIsOpen)
			{
				throw new ObjectDisposedException(BitStreamResources.GetString("ObjectDisposed_BitStreamClosed"));
			}
			BitStream bitStream = Copy();
			uint num = (uint)count;
			uint num2 = (uint)bitStream.Length;
			if (num >= num2)
			{
				bitStream.Position = 0L;
				for (uint num3 = 0u; num3 < num2; num3++)
				{
					bitStream.Write(bit: false);
				}
			}
			else
			{
				bool bit = false;
				for (uint num3 = 0u; num3 < num2 - num; num3++)
				{
					bitStream.Position = num + num3;
					bitStream.Read(out bit);
					bitStream.Position = num3;
					bitStream.Write(bit);
				}
				for (uint num3 = num2 - num; num3 < num2; num3++)
				{
					bitStream.Write(bit: false);
				}
			}
			bitStream.Position = 0L;
			return bitStream;
		}

		public virtual BitStream ShiftRight(long count)
		{
			if (!_blnIsOpen)
			{
				throw new ObjectDisposedException(BitStreamResources.GetString("ObjectDisposed_BitStreamClosed"));
			}
			BitStream bitStream = Copy();
			uint num = (uint)count;
			uint num2 = (uint)bitStream.Length;
			if (num >= num2)
			{
				bitStream.Position = 0L;
				for (uint num3 = 0u; num3 < num2; num3++)
				{
					bitStream.Write(bit: false);
				}
			}
			else
			{
				bool bit = false;
				for (uint num3 = 0u; num3 < num2 - num; num3++)
				{
					bitStream.Position = num3;
					bitStream.Read(out bit);
					bitStream.Position = num3 + num;
					bitStream.Write(bit);
				}
				bitStream.Position = 0L;
				for (uint num3 = 0u; num3 < num; num3++)
				{
					bitStream.Write(bit: false);
				}
			}
			bitStream.Position = 0L;
			return bitStream;
		}

		public override string ToString()
		{
			uint num = _uiBitBuffer_Length >> 5;
			uint num2 = 0u;
			int num3 = 0;
			uint num4 = 1u;
			StringBuilder stringBuilder = new StringBuilder((int)_uiBitBuffer_Length);
			for (num2 = 0u; num2 < num; num2++)
			{
				stringBuilder.Append("[" + num2.ToString(_ifp) + "]:{");
				for (num3 = 31; num3 >= 0; num3--)
				{
					uint num5 = num4 << num3;
					if ((_auiBitBuffer[num2] & num5) == num5)
					{
						stringBuilder.Append('1');
					}
					else
					{
						stringBuilder.Append('0');
					}
				}
				stringBuilder.Append("}\r\n");
			}
			if ((_uiBitBuffer_Length & 0x1F) != 0)
			{
				stringBuilder.Append("[" + num2.ToString(_ifp) + "]:{");
				int num6 = (int)(32 - (_uiBitBuffer_Length & 0x1F));
				for (num3 = 31; num3 >= num6; num3--)
				{
					uint num5 = num4 << num3;
					if ((_auiBitBuffer[num2] & num5) == num5)
					{
						stringBuilder.Append('1');
					}
					else
					{
						stringBuilder.Append('0');
					}
				}
				for (num3 = num6 - 1; num3 >= 0; num3--)
				{
					stringBuilder.Append('.');
				}
				stringBuilder.Append("}\r\n");
			}
			return stringBuilder.ToString();
		}

		public static string ToString(bool bit)
		{
			return "Boolean{" + (bit ? 1 : 0) + "}";
		}

		public static string ToString(byte bits)
		{
			StringBuilder stringBuilder = new StringBuilder(8);
			uint num = 1u;
			stringBuilder.Append("Byte{");
			for (int num2 = 7; num2 >= 0; num2--)
			{
				uint num3 = num << num2;
				if ((bits & num3) == num3)
				{
					stringBuilder.Append('1');
				}
				else
				{
					stringBuilder.Append('0');
				}
			}
			stringBuilder.Append("}");
			return stringBuilder.ToString();
		}

		
		public static string ToString(sbyte bits)
		{
			byte b = (byte)bits;
			StringBuilder stringBuilder = new StringBuilder(8);
			uint num = 1u;
			stringBuilder.Append("SByte{");
			for (int num2 = 7; num2 >= 0; num2--)
			{
				uint num3 = num << num2;
				if ((b & num3) == num3)
				{
					stringBuilder.Append('1');
				}
				else
				{
					stringBuilder.Append('0');
				}
			}
			stringBuilder.Append("}");
			return stringBuilder.ToString();
		}

		public static string ToString(char bits)
		{
			StringBuilder stringBuilder = new StringBuilder(16);
			uint num = 1u;
			stringBuilder.Append("Char{");
			for (int num2 = 15; num2 >= 0; num2--)
			{
				uint num3 = num << num2;
				if ((bits & num3) == num3)
				{
					stringBuilder.Append('1');
				}
				else
				{
					stringBuilder.Append('0');
				}
			}
			stringBuilder.Append("}");
			return stringBuilder.ToString();
		}

		
		public static string ToString(ushort bits)
		{
			short num = (short)bits;
			StringBuilder stringBuilder = new StringBuilder(16);
			uint num2 = 1u;
			stringBuilder.Append("UInt16{");
			for (int num3 = 15; num3 >= 0; num3--)
			{
				uint num4 = num2 << num3;
				if ((num & num4) == num4)
				{
					stringBuilder.Append('1');
				}
				else
				{
					stringBuilder.Append('0');
				}
			}
			stringBuilder.Append("}");
			return stringBuilder.ToString();
		}

		public static string ToString(short bits)
		{
			StringBuilder stringBuilder = new StringBuilder(16);
			uint num = 1u;
			stringBuilder.Append("Int16{");
			for (int num2 = 15; num2 >= 0; num2--)
			{
				uint num3 = num << num2;
				if ((bits & num3) == num3)
				{
					stringBuilder.Append('1');
				}
				else
				{
					stringBuilder.Append('0');
				}
			}
			stringBuilder.Append("}");
			return stringBuilder.ToString();
		}

		
		public static string ToString(uint bits)
		{
			StringBuilder stringBuilder = new StringBuilder(32);
			uint num = 1u;
			stringBuilder.Append("UInt32{");
			for (int num2 = 31; num2 >= 0; num2--)
			{
				uint num3 = num << num2;
				if ((bits & num3) == num3)
				{
					stringBuilder.Append('1');
				}
				else
				{
					stringBuilder.Append('0');
				}
			}
			stringBuilder.Append("}");
			return stringBuilder.ToString();
		}

		public static string ToString(int bits)
		{
			StringBuilder stringBuilder = new StringBuilder(32);
			uint num = 1u;
			stringBuilder.Append("Int32{");
			for (int num2 = 31; num2 >= 0; num2--)
			{
				uint num3 = num << num2;
				if ((bits & (int)num3) == (int)num3)
				{
					stringBuilder.Append('1');
				}
				else
				{
					stringBuilder.Append('0');
				}
			}
			stringBuilder.Append("}");
			return stringBuilder.ToString();
		}

		
		public static string ToString(ulong bits)
		{
			StringBuilder stringBuilder = new StringBuilder(64);
			ulong num = 1uL;
			stringBuilder.Append("UInt64{");
			for (int num2 = 63; num2 >= 0; num2--)
			{
				ulong num3 = num << num2;
				if ((bits & num3) == num3)
				{
					stringBuilder.Append('1');
				}
				else
				{
					stringBuilder.Append('0');
				}
			}
			stringBuilder.Append("}");
			return stringBuilder.ToString();
		}

		public static string ToString(long bits)
		{
			StringBuilder stringBuilder = new StringBuilder(64);
			ulong num = 1uL;
			stringBuilder.Append("Int64{");
			for (int num2 = 63; num2 >= 0; num2--)
			{
				ulong num3 = num << num2;
				if ((bits & (long)num3) == (long)num3)
				{
					stringBuilder.Append('1');
				}
				else
				{
					stringBuilder.Append('0');
				}
			}
			stringBuilder.Append("}");
			return stringBuilder.ToString();
		}

		public static string ToString(float bits)
		{
			byte[] bytes = BitConverter.GetBytes(bits);
			uint num = (uint)(bytes[0] | (bytes[1] << 8) | (bytes[2] << 16) | (bytes[3] << 24));
			StringBuilder stringBuilder = new StringBuilder(32);
			uint num2 = 1u;
			stringBuilder.Append("Single{");
			for (int num3 = 31; num3 >= 0; num3--)
			{
				uint num4 = num2 << num3;
				if ((num & num4) == num4)
				{
					stringBuilder.Append('1');
				}
				else
				{
					stringBuilder.Append('0');
				}
			}
			stringBuilder.Append("}");
			return stringBuilder.ToString();
		}

		public static string ToString(double bits)
		{
			byte[] bytes = BitConverter.GetBytes(bits);
			ulong num = bytes[0] | ((ulong)bytes[1] << 8) | ((ulong)bytes[2] << 16) | ((ulong)bytes[3] << 24) | ((ulong)bytes[4] << 32) | ((ulong)bytes[5] << 40) | ((ulong)bytes[6] << 48) | ((ulong)bytes[7] << 56);
			StringBuilder stringBuilder = new StringBuilder(64);
			ulong num2 = 1uL;
			stringBuilder.Append("Double{");
			for (int num3 = 63; num3 >= 0; num3--)
			{
				ulong num4 = num2 << num3;
				if ((num & num4) == num4)
				{
					stringBuilder.Append('1');
				}
				else
				{
					stringBuilder.Append('0');
				}
			}
			stringBuilder.Append("}");
			return stringBuilder.ToString();
		}

		private void UpdateLengthForWrite(uint bits)
		{
			_uiBitBuffer_Length += bits;
		}

		private void UpdateIndicesForWrite(uint bits)
		{
			_uiBitBuffer_BitIndex += bits;
			if (_uiBitBuffer_BitIndex == 32)
			{
				_uiBitBuffer_Index++;
				_uiBitBuffer_BitIndex = 0u;
				if (_auiBitBuffer.Length == _uiBitBuffer_Length >> 5)
				{
					_auiBitBuffer = ReDimPreserve(_auiBitBuffer, (uint)(_auiBitBuffer.Length << 1));
				}
			}
			else if (_uiBitBuffer_BitIndex > 32)
			{
				throw new InvalidOperationException(BitStreamResources.GetString("InvalidOperation_BitIndexGreaterThan32"));
			}
		}

		private void UpdateIndicesForRead(uint bits)
		{
			_uiBitBuffer_BitIndex += bits;
			if (_uiBitBuffer_BitIndex == 32)
			{
				_uiBitBuffer_Index++;
				_uiBitBuffer_BitIndex = 0u;
			}
			else if (_uiBitBuffer_BitIndex > 32)
			{
				throw new InvalidOperationException(BitStreamResources.GetString("InvalidOperation_BitIndexGreaterThan32"));
			}
		}

		private static uint[] ReDimPreserve(uint[] buffer, uint newLength)
		{
			uint[] array = new uint[newLength];
			uint num = (uint)buffer.Length;
			if (num < newLength)
			{
				Buffer.BlockCopy(buffer, 0, array, 0, (int)(num << 2));
			}
			else
			{
				Buffer.BlockCopy(buffer, 0, array, 0, (int)(newLength << 2));
			}
			buffer = null;
			return array;
		}

		public override void Close()
		{
			_blnIsOpen = false;
			_uiBitBuffer_Index = 0u;
			_uiBitBuffer_BitIndex = 0u;
		}

		
		public virtual uint[] GetBuffer()
		{
			return _auiBitBuffer;
		}

		public virtual BitStream Copy()
		{
			BitStream bitStream = new BitStream(Length);
			Buffer.BlockCopy(_auiBitBuffer, 0, bitStream._auiBitBuffer, 0, bitStream._auiBitBuffer.Length << 2);
			bitStream._uiBitBuffer_Length = _uiBitBuffer_Length;
			return bitStream;
		}

		public override IAsyncResult BeginRead(byte[] buffer, int offset, int count, AsyncCallback callback, object state)
		{
			throw new NotSupportedException(BitStreamResources.GetString("NotSupported_AsyncOps"));
		}

		public override IAsyncResult BeginWrite(byte[] buffer, int offset, int count, AsyncCallback callback, object state)
		{
			throw new NotSupportedException(BitStreamResources.GetString("NotSupported_AsyncOps"));
		}

		public override int EndRead(IAsyncResult asyncResult)
		{
			throw new NotSupportedException(BitStreamResources.GetString("NotSupported_AsyncOps"));
		}

		public override void EndWrite(IAsyncResult asyncResult)
		{
			throw new NotSupportedException(BitStreamResources.GetString("NotSupported_AsyncOps"));
		}

		public override long Seek(long offset, SeekOrigin origin)
		{
			throw new NotSupportedException(BitStreamResources.GetString("NotSupported_Seek"));
		}

		public override void SetLength(long value)
		{
			throw new NotSupportedException(BitStreamResources.GetString("NotSupported_SetLength"));
		}

		public override void Flush()
		{
			throw new NotSupportedException(BitStreamResources.GetString("NotSupported_Flush"));
		}

		public static implicit operator BitStream(MemoryStream bits)
		{
			if (bits == null)
			{
				throw new ArgumentNullException("bits", BitStreamResources.GetString("ArgumentNull_MemoryStream"));
			}
			return new BitStream(bits);
		}

		public static implicit operator MemoryStream(BitStream bits)
		{
			if (bits == null)
			{
				throw new ArgumentNullException("bits", BitStreamResources.GetString("ArgumentNull_BitStream"));
			}
			return new MemoryStream(bits.ToByteArray());
		}

		public static implicit operator BitStream(FileStream bits)
		{
			if (bits == null)
			{
				throw new ArgumentNullException("bits", BitStreamResources.GetString("ArgumentNull_FileStream"));
			}
			return new BitStream(bits);
		}

		public static implicit operator BitStream(BufferedStream bits)
		{
			if (bits == null)
			{
				throw new ArgumentNullException("bits", BitStreamResources.GetString("ArgumentNull_BufferedStream"));
			}
			return new BitStream(bits);
		}

		public static implicit operator BufferedStream(BitStream bits)
		{
			if (bits == null)
			{
				throw new ArgumentNullException("bits", BitStreamResources.GetString("ArgumentNull_BitStream"));
			}
			return new BufferedStream((MemoryStream)bits);
		}

		public static implicit operator BitStream(NetworkStream bits)
		{
			if (bits == null)
			{
				throw new ArgumentNullException("bits", BitStreamResources.GetString("ArgumentNull_NetworkStream"));
			}
			return new BitStream(bits);
		}

		public static implicit operator BitStream(CryptoStream bits)
		{
			if (bits == null)
			{
				throw new ArgumentNullException("bits", BitStreamResources.GetString("ArgumentNull_CryptoStream"));
			}
			return new BitStream(bits);
		}
	}
}
