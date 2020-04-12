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
namespace mp4parser.h264
{


    /// <summary>
    /// Simple BTree implementation needed for haffman tables
    /// 
    /// @author Stanislav Vitvitskiy
    /// </summary>
    public class BTree
	{
		private BTree zero;
		private BTree one;
		private object value;


		public virtual void addString(string path, object value)
		{
			if (path.Length == 0)
			{
				this.value = value;
				return;
			}
			char charAt = path[0];
			BTree branch;
			if (charAt == '0')
			{
				if (zero == null)
				{
					zero = new BTree();
				}
				branch = zero;
			}
			else
			{
				if (one == null)
				{
					one = new BTree();
				}
				branch = one;
			}
			branch.addString(path.Substring(1), value);
		}

		public virtual BTree down(int b)
		{
			if (b == 0)
			{
				return zero;
			}
			else
			{
				return one;
			}
		}

		public virtual object Value
		{
			get
			{
				return value;
			}
		}
	}
}