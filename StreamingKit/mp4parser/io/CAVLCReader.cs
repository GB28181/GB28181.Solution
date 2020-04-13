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
namespace mp4parser.h264.read
{




    public class CAVLCReader : BitstreamReader
    {

        public CAVLCReader(Stream @is) : base(@is)
        {
        }

        public virtual long ReadNBit(int n, string message)
        {
            long val = readNBit(n);

            Trace(message, Convert.ToString(val));

            return val;
        }

        private int ReadUE()
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


        public virtual int ReadUE(string message)
        {
            int res = ReadUE();

            Trace(message, Convert.ToString(res));

            return res;
        }

        public virtual int ReadSE(string message)
        {
            int val = ReadUE();

            int sign = ((val & 0x1) << 1) - 1;
            val = ((val >> 1) + (val & 0x1)) * sign;

            Trace(message, Convert.ToString(val));

            return val;
        }


        public virtual bool ReadBool(string message)
        {

            bool res = read1Bit() == 0 ? false : true;

            Trace(message, res ? "1" : "0");

            return res;
        }

        public virtual int ReadU(int i, string @string)
        {
            return (int)ReadNBit(i, @string);
        }


        public virtual sbyte[] Read(int payloadSize)
        {
            sbyte[] result = new sbyte[payloadSize];
            for (int i = 0; i < payloadSize; i++)
            {
                result[i] = (sbyte)readByte();
            }
            return result;
        }

        public virtual bool ReadAE()
        {
            // TODO: do it!!
            throw new System.NotSupportedException("Stan");
        }


        public virtual int ReadTE(int max)
        {
            if (max > 1)
            {
                return ReadUE();
            }
            return ~read1Bit() & 0x1;
        }

        public virtual int ReadAEI()
        {
            // TODO: do it!!
            throw new System.NotSupportedException("Stan");
        }


        public virtual int ReadME(string @string)
        {
            return ReadUE(@string);
        }

        public virtual object ReadCE(BTree bt, string message)
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
                    Trace(message, i.ToString());
                    return i;
                }
            }
        }


        public virtual int ReadZeroBitCount(string message)
        {
            int count = 0;
            while (read1Bit() == 0)
            {
                count++;
            }

            Trace(message, Convert.ToString(count));

            return count;
        }


        public virtual void ReadTrailingBits()
        {
            read1Bit();
            readRemainingByte();
        }

        private void Trace(string message, string val)
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