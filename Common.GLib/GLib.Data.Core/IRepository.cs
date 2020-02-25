using GLib.Data.Entity;
using System;

namespace GLib.Data.Core
{
	public interface IRepository<TL> : IDisposable
	{
		bool Exists(TL model);

		bool Exists(int id);

		bool Exists(long id);

		bool Exists(string selector);

		bool Exists(string selector, params DBParam[] values);

		int Sum(string selector, string column);

		int Sum(string selector, string column, params DBParam[] values);

		int InsertModel(TL model);

		int[] InsertModelList(TL[] modelList);

		TL GetModel(int id);

		TL GetModel(long id);

		TL GetModel(TL model);

		TL GetModel(string selector, params DBParam[] values);

		TL GetModel(string selector, string colList, params DBParam[] values);

		TL[] GetModelList();

		TL[] GetModelList(TL model);

		TL[] GetModelList(string selector, params DBParam[] values);

		TL[] GetModelList(string selector, string ordering, params DBParam[] values);

		TL[] GetModelList(string selector, string ordering, string colList, params DBParam[] values);

		TL[] GetModelList(string selector, string ordering, string colList, int pageIndex, int pageSize, params DBParam[] values);

		int GetModelListCount(string selector, params DBParam[] values);

		bool UpdateModel(TL model);

		bool UpdateModel(TL model, string colList);

		bool UpdateModel(TL model, string selector, params DBParam[] values);

		bool UpdateModel(TL model, string colList, string selector, params DBParam[] values);

		bool DeleteModel(int id);

		bool DeleteModel(long id);

		bool DeleteModel(TL model);

		bool Delete(string selector, params DBParam[] values);
	}
}
