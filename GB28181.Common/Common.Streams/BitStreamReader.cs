using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;

namespace Common.Streams
{
	public class BitStreamReader
	{
		private Stream _stream = null;

		private BinaryReader _reader = null;

		private int byte_point = -1;

		private BitArray bit_arr = null;

		public BitStreamReader(byte[] bytes)
			: this(new MemoryStream(bytes))
		{
		}

		public BitStreamReader(Stream stream)
		{
			_stream = stream;
			_reader = new BinaryReader(_stream);
		}

		public bool ReadBit()
		{
			if (byte_point == -1 || bit_arr == null)
			{
				bit_arr = new BitArray(new byte[1]
				{
					_reader.ReadByte()
				});
				byte_point = 7;
			}
			bool result = bit_arr.Get(byte_point);
			byte_point--;
			return result;
		}

		public byte[] ReadBit(int len)
		{
			int num = 0;
			int num2 = len / 8 + ((len % 8 != 0) ? 1 : 0);
			List<byte> list = new List<byte>();
			for (int i = 0; i < num2; i++)
			{
				byte b = 0;
				int num3 = (len - num >= 8) ? 7 : (len - num - 1);
				for (int num4 = num3; num4 >= 0; num4--)
				{
					b = ((!ReadBit()) ? ((byte)(b << ((num4 > 0) ? 1 : 0))) : ((byte)((1 | b) << ((num4 > 0) ? 1 : 0))));
					num++;
					if (num == len)
					{
						break;
					}
				}
				list.Add(b);
				if (num == len)
				{
					break;
				}
			}
			return list.ToArray();
		}

		public void Skip(int len)
		{
			ReadBit(len);
		}

		public byte ReadByte(int len)
		{
			byte[] array = ReadBit(len);
			return array[0];
		}

		public short ReadShort(int len)
		{
			byte[] array = ReadBit(len);
			if (array.Length < 2)
			{
				List<byte> list = new List<byte>();
				list.AddRange(new byte[2 - array.Length]);
				list.AddRange(array);
				array = list.ToArray();
			}
			Array.Reverse(array);
			return BitConverter.ToInt16(array, 0);
		}

		public int ReadInt(int len)
		{
			byte[] array = ReadBit(len);
			if (array.Length < 4)
			{
				List<byte> list = new List<byte>();
				list.AddRange(new byte[4 - array.Length]);
				list.AddRange(array);
				array = list.ToArray();
			}
			Array.Reverse(array);
			return BitConverter.ToInt32(array, 0);
		}
	}
}
