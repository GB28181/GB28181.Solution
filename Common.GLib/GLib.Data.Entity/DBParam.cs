using System.Data;

namespace GLib.Data.Entity
{
	public class DBParam
	{
		private string _ParamName = "";

		private DbType _ParamDbType = DbType.String;

		private object _ParamValue = null;

		public string ParamName
		{
			get
			{
				return _ParamName;
			}
			set
			{
				_ParamName = value;
			}
		}

		public DbType ParamDbType
		{
			get
			{
				return _ParamDbType;
			}
			set
			{
				_ParamDbType = value;
			}
		}

		public object ParamValue
		{
			get
			{
				return _ParamValue;
			}
			set
			{
				_ParamValue = value;
			}
		}
	}
}
