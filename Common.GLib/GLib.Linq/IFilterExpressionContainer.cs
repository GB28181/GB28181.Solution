using System.Collections.Generic;

namespace GLib.Linq
{
	public interface IFilterExpressionContainer
	{
		IList<FilterExpression> GetExpressions();

		void AddFilterExpression(string predicate, params object[] values);

		void RemoveFilterExpression(string predicate, params object[] values);

		void RemoveAllFilterExpression(string predicate);

		void RemoveAllFilterExpression();
	}
}
