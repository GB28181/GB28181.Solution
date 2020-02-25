using System;

namespace GLib.Linq
{
	[Serializable]
	public class FilterExpression
	{
		public string Predicate
		{
			get;
			set;
		}

		public object[] Values
		{
			get;
			set;
		}
	}
}
