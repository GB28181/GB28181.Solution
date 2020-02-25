using System.Collections.Generic;
using System.Linq;

namespace GLib.Linq
{
	public static class DynamicQueryable
	{
		public static IQueryable<T> Where<T>(this IQueryable<T> source, IFilterExpressionContainer container)
		{
			//IList<FilterExpression> expressions = container.GetExpressions();
			//foreach (FilterExpression item in expressions)
			//{
			//	//source = DynamicQueryable.Where<T>(source, item.Predicate, item.Values);			
			//}
			source = Where(source, container);

			return source;
		}

		public static IQueryable<T> OrderBy<T>(this IQueryable<T> source, string ordering)
		{
			if (!string.IsNullOrEmpty(ordering))
			{
				//	return DynamicQueryable.OrderBy<T>(source, ordering, new object[0]);
				OrderBy(source, ordering);
			}
			return source;
		}
	}
}
