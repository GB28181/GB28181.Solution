using System;
using System.IO;
using System.Text;

/*
Copyright (c) 2011 Stanislav Vitvitskiy

Permission is hereby granted, free of charge, to any person obtaining a copy of this
software and associated documentation files (the "Software"), to deal in the Software
without restriction, including without limitation the rights to use, copy, modify,
merge, publish, distribute, sublicense, and/or sell copies of the Software, and to
permit persons to whom the Software is furnished to do so, subject to the following
conditions:

The above copyright notice and this permission notice shall be included in all copies or
substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED,
INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR
PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE
FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT,
TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE
OR OTHER DEALINGS IN THE SOFTWARE.
*/
namespace com.googlecode.mp4parser.h264.read
{




    public class CAVLCReader : BitstreamReader
	{

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public CAVLCReader(java.io.Stream is) throws java.io.IOException
		public CAVLCReader(Stream @is) : base(@is)
		{
		}

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public long readNBit(int n, String message) throws java.io.IOException
		public virtual long readNBit(int n, string message)
		{
			long val = readNBit(n);

			trace(message, Convert.ToString(val));

			return val;
		}

		/// <summary>
		/// Read unsigned exp-golomb code
		/// 
		/// @return </summary>
		/// <exception cref="java.io.IOException"> </exception>
		/// <exception cref="java.io.IOException"> </exception>
//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: private int readUE() throws java.io.IOException
		private int readUE()
		{
			int cnt = 0;
			while (read1Bit() == 0)
			{
				cnt++;
			}

			int res = 0;
			if (cnt > 0)
			{
				long val = readNBit(cnt);

				res = (int)((1 << cnt) - 1 + val);
			}

			return res;
		}

		/*
		  * (non-Javadoc)
		  *
		  * @see
		  * ua.org.jplayer.javcodec.h264.H264BitStream#readUE(java.lang.String)
		  */
//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public int readUE(String message) throws java.io.IOException
		public virtual int readUE(string message)
		{
			int res = readUE();

			trace(message, Convert.ToString(res));

			return res;
		}

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public int readSE(String message) throws java.io.IOException
		public virtual int readSE(string message)
		{
			int val = readUE();

			int sign = ((val & 0x1) << 1) - 1;
			val = ((val >> 1) + (val & 0x1)) * sign;

			trace(message, Convert.ToString(val));

			return val;
		}

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public boolean readBool(String message) throws java.io.IOException
		public virtual bool readBool(string message)
		{

			bool res = read1Bit() == 0 ? false : true;

			trace(message, res ? "1" : "0");

			return res;
		}

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public int readU(int i, String string) throws java.io.IOException
		public virtual int readU(int i, string @string)
		{
			return (int) readNBit(i, @string);
		}

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public byte[] read(int payloadSize) throws java.io.IOException
		public virtual sbyte[] read(int payloadSize)
		{
			sbyte[] result = new sbyte[payloadSize];
			for (int i = 0; i < payloadSize; i++)
			{
				result[i] = (sbyte) readByte();
			}
			return result;
		}

		public virtual bool readAE()
		{
			// TODO: do it!!
			throw new System.NotSupportedException("Stan");
		}

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public int readTE(int max) throws java.io.IOException
		public virtual int readTE(int max)
		{
			if (max > 1)
			{
				return readUE();
			}
			return ~read1Bit() & 0x1;
		}

		public virtual int readAEI()
		{
			// TODO: do it!!
			throw new System.NotSupportedException("Stan");
		}

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public int readME(String string) throws java.io.IOException
		public virtual int readME(string @string)
		{
			return readUE(@string);
		}

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public Object readCE(com.googlecode.mp4parser.h264.BTree bt, String message) throws java.io.IOException
		public virtual object readCE(BTree bt, string message)
		{
			while (true)
			{
				int bit = read1Bit();
				bt = bt.down(bit);
				if (bt == null)
				{
					throw new Exception("Illegal code");
				}
				object i = bt.Value;
				if (i != null)
				{
					trace(message, i.ToString());
					return i;
				}
			}
		}

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public int readZeroBitCount(String message) throws java.io.IOException
		public virtual int readZeroBitCount(string message)
		{
			int count = 0;
			while (read1Bit() == 0)
			{
				count++;
			}

			trace(message, Convert.ToString(count));

			return count;
		}

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public void readTrailingBits() throws java.io.IOException
		public virtual void readTrailingBits()
		{
			read1Bit();
			readRemainingByte();
		}

		private void trace(string message, string val)
		{
			StringBuilder traceBuilder = new StringBuilder();
			int spaces;
			string pos = Convert.ToString(bitsRead - debugBits.length());
			spaces = 8 - pos.Length;

			traceBuilder.Append("@" + pos);

			for (int i = 0; i < spaces; i++)
			{
				traceBuilder.Append(' ');
			}

			traceBuilder.Append(message);
			spaces = 100 - traceBuilder.Length - debugBits.length();
			for (int i = 0; i < spaces; i++)
			{
				traceBuilder.Append(' ');
			}
			traceBuilder.Append(debugBits);
			traceBuilder.Append(" (" + val + ")");
			debugBits.clear();
             
		}
	}
}