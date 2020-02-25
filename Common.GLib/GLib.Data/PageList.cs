using System.Collections.Generic;

namespace GLib.Data
{
	public class PageList<T> : List<T>
	{
		public int PageIndex
		{
			get;
			set;
		}

		public int PageSize
		{
			get;
			set;
		}

		public int TotalCount
		{
			get;
			set;
		}

		public PageList(int pageIndex, int pageSize, int totalCount)
		{
			PageIndex = pageIndex;
			PageSize = pageSize;
			TotalCount = totalCount;
		}
	}
}
