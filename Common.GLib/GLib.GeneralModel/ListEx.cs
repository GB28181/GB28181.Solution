using System.Collections.Generic;

namespace GLib.GeneralModel
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
