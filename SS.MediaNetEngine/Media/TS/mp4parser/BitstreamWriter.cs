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
using System.IO;
namespace com.googlecode.mp4parser.h264.write
{


    /// <summary>
    /// A dummy implementation of H264 RBSP output stream
    /// 
    /// @author Stanislav Vitvitskiy
    /// </summary>
    public class BitstreamWriter
	{

		private readonly Stream os;
		private int[] curByte = new int[8];
		private int curBit;

        public BitstreamWriter(Stream @out)
		{
			this.os = @out;
		}

		/*
		 * (non-Javadoc)
		 * 
		 * @see ua.org.jplayer.javcodec.h264.H264BitOutputStream#flush()
		 */
//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public void flush() throws java.io.IOException
		public virtual void flush()
		{
			for (int i = curBit; i < 8; i++)
			{
				curByte[i] = 0;
			}
			curBit = 0;
			writeCurByte();
		}

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: private void writeCurByte() throws java.io.IOException
		private void writeCurByte()
		{
			int toWrite = (curByte[0] << 7) | (curByte[1] << 6) | (curByte[2] << 5) | (curByte[3] << 4) | (curByte[4] << 3) | (curByte[5] << 2) | (curByte[6] << 1) | curByte[7];
			os.WriteByte((byte)toWrite);
		}

		/*
		 * (non-Javadoc)
		 * 
		 * @see ua.org.jplayer.javcodec.h264.H264BitOutputStream#write1Bit(int)
		 */
//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public void write1Bit(int value) throws java.io.IOException
		public virtual void write1Bit(int value)
		{
		 
			if (curBit == 8)
			{
				curBit = 0;
				writeCurByte();
			}
			curByte[curBit++] = value;
		}

		/*
		 * (non-Javadoc)
		 * 
		 * @see ua.org.jplayer.javcodec.h264.H264BitOutputStream#writeNBit(long,
		 * int)
		 */
//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public void writeNBit(long value, int n) throws java.io.IOException
		public virtual void writeNBit(long value, int n)
		{
			for (int i = 0; i < n; i++)
			{
				write1Bit((int)(value >> (n - i - 1)) & 0x1);
			}
		}

		/*
		 * (non-Javadoc)
		 * 
		 * @see
		 * ua.org.jplayer.javcodec.h264.H264BitOutputStream#writeRemainingZero()
		 */
//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public void writeRemainingZero() throws java.io.IOException
		public virtual void writeRemainingZero()
		{
			writeNBit(0, 8 - curBit);
		}

		/*
		 * (non-Javadoc)
		 * 
		 * @see ua.org.jplayer.javcodec.h264.H264BitOutputStream#writeByte(int)
		 */
//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public void writeByte(int b) throws java.io.IOException
		public virtual void writeByte(int b)
		{
			os.WriteByte((byte)b);

		}
	}
}