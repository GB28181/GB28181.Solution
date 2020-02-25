using GLib.Data.Entity;
using System;

namespace GLib.Data.Core
{
	public interface ISqlCreator<TL> : IDisposable where TL : ILogicEntity, new()
	{
		string CreateMaxSql(string selector, string col);

		string CreateCountSql(string selector, string col);

		string CreateSumSql(string selector, string col);

		string CreateInsertSql(TL model, out DBParam[] outparam);

		string CreateUpdateSql(TL model, string colList, string selector, out DBParam[] outparam, params DBParam[] values);

		string CreateDeleteSql(TL model, out DBParam[] outparam);

		string CreateDeleteSql(string selector);

		string CreateModelSql(TL model, out DBParam[] outparam);

		string CreateModelSql(string selector, string colList);

		string CreatePageSql(TL model, out DBParam[] outparam);

		string CreatePageSql(string selector, int pageIndex, int pageSize, string ordering, string colList, params DBParam[] values);
	}
}
