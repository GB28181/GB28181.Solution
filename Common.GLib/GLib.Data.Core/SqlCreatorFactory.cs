using GLib.Data.Entity;

namespace GLib.Data.Core
{
	public class SqlCreatorFactory
	{
		public static ISqlCreator<TL> GetSqlCreator<TL>() where TL : ILogicEntity, new()
		{
			DataBaseType dBType = SqlHelper.GetDBType();
			if (dBType == DataBaseType.SqlServer)
			{
				return new SqlCreator<TL>();
			}
			return new SqlCreator<TL>();
		}
	}
}
