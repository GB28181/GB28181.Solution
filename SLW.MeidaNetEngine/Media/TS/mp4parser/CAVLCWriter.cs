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
using System;
using System.Diagnostics;
using System.IO;
namespace com.googlecode.mp4parser.h264.write
{



    /// <summary>
    /// A class responsible for outputting exp-Golumb values into binary stream
    /// 
    /// @author Stanislav Vitvitskiy
    /// </summary>
    public class CAVLCWriter : BitstreamWriter
	{

        public CAVLCWriter(Stream @out)
            : base(@out)
		{
		}

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public void writeU(int value, int n, String string) throws java.io.IOException
		public virtual void writeU(int value, int n, string @string)
		{
            Debug.Print(@string + "\t");
			writeNBit(value, n);
            Debug.Print("\t" + value);
		}

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public void writeUE(int value) throws java.io.IOException
		public virtual void writeUE(int value)
		{
			int bits = 0;
			int cumul = 0;
			for (int i = 0; i < 15; i++)
			{
				if (value < cumul + (1 << i))
				{
					bits = i;
					break;
				}
				cumul += (1 << i);
			}
			writeNBit(0, bits);
			write1Bit(1);
			writeNBit(value - cumul, bits);
		}

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public void writeUE(int value, String string) throws java.io.IOException
		public virtual void writeUE(int value, string @string)
		{
			Debug.Print(@string + "\t");
			writeUE(value);
			Debug.Print("\t" + value);
		}

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public void writeSE(int value, String string) throws java.io.IOException
		public virtual void writeSE(int value, string @string)
		{
			Debug.Print(@string + "\t");
			writeUE((value << 1) * (value < 0 ? - 1 : 1) + (value > 0 ? 1 : 0));
			Debug.Print("\t" + value);
		}

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public void writeBool(boolean value, String string) throws java.io.IOException
		public virtual void writeBool(bool value, string @string)
		{
			Debug.Print(@string + "\t");
			write1Bit(value ? 1 : 0);
			Debug.Print("\t" + value);
		}

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public void writeU(int i, int n) throws java.io.IOException
		public virtual void writeU(int i, int n)
		{
			writeNBit(i, n);
		}

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public void writeNBit(long value, int n, String string) throws java.io.IOException
		public virtual void writeNBit(long value, int n, string @string)
		{
			Debug.Print(@string + "\t");
			for (int i = 0; i < n; i++)
			{
				write1Bit((int)(value >> (n - i - 1)) & 0x1);
			}
			Debug.Print("\t" + value);
		}

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public void writeTrailingBits() throws java.io.IOException
		public virtual void writeTrailingBits()
		{
			write1Bit(1);
			writeRemainingZero();
			flush();
		}

		public virtual void writeSliceTrailingBits()
		{
			throw new  Exception("todo");
		}
	}
}