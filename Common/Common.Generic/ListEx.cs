using System.Collections.Generic;

namespace Common.Generic
{
	public class ListEx<T> : List<T>
	{
		public ListEx()
		{
		}

		public ListEx(IEnumerable<T> collection)
			: base(collection)
		{
		}
	}
}
