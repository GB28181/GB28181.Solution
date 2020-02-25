using GLib.Data.Entity;
using GLib.Logging;
using System;
using System.Collections.Generic;
using System.Data;
using System.Reflection;

namespace GLib.Data.Core
{
	public class Repository<TL> : IRepository<TL>, IDisposable where TL : ILogicEntity, new()
	{
		private ISqlCreator<TL> creator = null;

		private string _dbname = "";

		private string _PrimaryKey = "";

		private TL _model = default(TL);

		private int _PageSize = 100;

		private TL Model
		{
			get
			{
				if (_model == null)
				{
					_model = new TL();
				}
				return _model;
			}
		}

		public string DbName
		{
			get
			{
				if (string.IsNullOrEmpty(_dbname))
				{
					if (Model == null)
					{
						throw new ArgumentNullException("model is null!");
					}
					_dbname = Model.DbName;
				}
				return _dbname;
			}
			set
			{
				_dbname = value;
			}
		}

		public string PrimaryKey
		{
			get
			{
				if (_PrimaryKey == "")
				{
					if (Model == null)
					{
						throw new ArgumentNullException("model is null !");
					}
					_PrimaryKey = Model.PrimaryKey;
				}
				return _PrimaryKey;
			}
			set
			{
				_PrimaryKey = value;
			}
		}

		public int MaxID
		{
			get
			{
				string strCommand = creator.CreateMaxSql(string.Empty, PrimaryKey);
				object obj = SqlHelper.ExecuteScalar(DbName, strCommand, CommandType.Text, null);
				if (obj == null)
				{
					return 0;
				}
				if (obj.ToString().Trim() == "")
				{
					return 0;
				}
				return int.Parse(obj.ToString());
			}
		}

		public int PageSize
		{
			get
			{
				return _PageSize;
			}
			set
			{
				_PageSize = value;
			}
		}

		public Repository()
		{
			creator = SqlCreatorFactory.GetSqlCreator<TL>();
		}

		protected Repository(string dbName)
			: this()
		{
			_dbname = dbName;
			creator = SqlCreatorFactory.GetSqlCreator<TL>();
		}

		public virtual bool Exists(TL model)
		{
			throw new ArgumentNullException("Exists is not implement!");
		}

		public virtual bool Exists(int id)
		{
			return Exists($"{PrimaryKey}=@0", new DBParam
			{
				ParamName = PrimaryKey,
				ParamDbType = DbType.Int32,
				ParamValue = id
			});
		}

		public virtual bool Exists(long id)
		{
			return Exists($"{PrimaryKey}=@0", new DBParam
			{
				ParamName = PrimaryKey,
				ParamDbType = DbType.Int64,
				ParamValue = id
			});
		}

		public virtual bool Exists(string selector)
		{
			return Exists(selector, (DBParam[])null);
		}

		public virtual bool Exists(string selector, params DBParam[] values)
		{
			string strCommand = creator.CreateCountSql(selector, string.Empty);
			object obj = SqlHelper.ExecuteScalar(DbName, strCommand, CommandType.Text, values);
			if (object.Equals(obj, null) || object.Equals(obj, DBNull.Value))
			{
				return false;
			}
			if (Convert.ToInt32(obj) == 0)
			{
				return false;
			}
			return true;
		}

		public virtual int Sum(string selector, string column)
		{
			return Sum(selector, column, (DBParam[])null);
		}

		public virtual int Sum(string selector, string column, params DBParam[] values)
		{
			string strCommand = creator.CreateSumSql(selector, column);
			object obj = SqlHelper.ExecuteScalar(DbName, strCommand, CommandType.Text, values);
			if (object.Equals(obj, null) || object.Equals(obj, DBNull.Value))
			{
				return 0;
			}
			return Convert.ToInt32(obj);
		}

		public virtual int InsertModel(TL model)
		{
			_model = model;
			try
			{
				DBParam[] outparam = null;
				string strCommand = creator.CreateInsertSql(model, out outparam);
				if (model.IsAutoID)
				{
					object obj = SqlHelper.ExecuteScalar(DbName, strCommand, CommandType.Text, outparam);
					if (SqlHelper.GetDBType() == DataBaseType.Access)
					{
						return MaxID;
					}
					if (obj == null || obj == DBNull.Value)
					{
						return 0;
					}
					return Convert.ToInt32(obj);
				}
				return SqlHelper.ExecuteNonQuery(DbName, strCommand, CommandType.Text, outparam);
			}
			catch (Exception ex)
			{
				Log log = new Log();
				log.Write(new LogMessage
				{
					Content = "GLib.Data.Core.Repository. InsertModel(TL model)：\r\n " + ex.Message,
					Type = MessageType.Error
				});
				return 0;
			}
		}

		public virtual int[] InsertModelList(TL[] modelList)
		{
			List<int> list = new List<int>();
			foreach (TL model in modelList)
			{
				int item;
				if ((item = InsertModel(model)) > 0)
				{
					list.Add(item);
				}
			}
			return list.ToArray();
		}

		public virtual TL GetModel(int id)
		{
			return GetModel($"{PrimaryKey}={id}", string.Empty, (DBParam[])null);
		}

		public virtual TL GetModel(long id)
		{
			return GetModel($"{PrimaryKey}={id}", string.Empty, (DBParam[])null);
		}

		public virtual TL GetModel(TL model)
		{
			try
			{
				DBParam[] outparam = null;
				string strCommand = creator.CreateModelSql(model, out outparam);
				model = default(TL);
				using (IDataReader dataReader = SqlHelper.ExecuteDataReader(DbName, strCommand, CommandType.Text, outparam))
				{
					if (dataReader.Read())
					{
						model = GetModel(dataReader);
					}
					dataReader.Close();
				}
				return model;
			}
			catch (Exception ex)
			{
				Log log = new Log();
				log.Write(new LogMessage
				{
					Content = "GLib.Data.Core.Repository. GetModel(String selector, String colList, params DBParam[] values)：\r\n " + ex.Message,
					Type = MessageType.Error
				});
				return default(TL);
			}
		}

		public virtual TL GetModel(string selector, params DBParam[] values)
		{
			return GetModel(selector, string.Empty, values);
		}

		public virtual TL GetModel(string selector, string colList, params DBParam[] values)
		{
			try
			{
				string strCommand = creator.CreateModelSql(selector, colList);
				TL result = default(TL);
				using (IDataReader dataReader = SqlHelper.ExecuteDataReader(DbName, strCommand, CommandType.Text, values))
				{
					if (dataReader.Read())
					{
						result = GetModel(dataReader);
					}
					dataReader.Close();
				}
				return result;
			}
			catch (Exception ex)
			{
				Log log = new Log();
				log.Write(new LogMessage
				{
					Content = "GLib.Data.Core.Repository. GetModel(String selector, String colList, params DBParam[] values)：\r\n " + ex.Message,
					Type = MessageType.Error
				});
				return default(TL);
			}
		}

		public virtual TL GetModel(IDataReader dr)
		{
			TL val = new TL();
			PropertyInfo[] properties = EntityTypeCache.GetEntityInfo(typeof(TL)).Properties;
			PropertyInfo[] array = properties;
			foreach (PropertyInfo propertyInfo in array)
			{
				object[] customAttributes = propertyInfo.GetCustomAttributes(typeof(EntityAttribute), inherit: true);
				EntityAttribute entityAttribute = null;
				if (customAttributes.Length > 0)
				{
					entityAttribute = (EntityAttribute)customAttributes[0];
				}
				if (entityAttribute != null && entityAttribute.CustomMember)
				{
					continue;
				}
				int ordinal;
				try
				{
					ordinal = dr.GetOrdinal(propertyInfo.Name);
				}
				catch (IndexOutOfRangeException)
				{
					continue;
				}
				object value;
				if (dr.IsDBNull(ordinal) && EntityHelper.GetDbType(propertyInfo.PropertyType) == DbType.String)
				{
					value = string.Empty;
				}
				else
				{
					if (dr.IsDBNull(ordinal))
					{
						continue;
					}
					value = dr.GetValue(ordinal);
				}
				propertyInfo.SetValue(val, value, null);
			}
			return val;
		}

		public virtual TL[] GetModelList()
		{
			return GetModelList(string.Empty, string.Empty, string.Empty, 0, PageSize, (DBParam[])null);
		}

		public virtual TL[] GetModelList(TL model)
		{
			try
			{
				DBParam[] outparam = null;
				string strCommand = creator.CreatePageSql(model, out outparam);
				TL[] array = null;
				using (IDataReader dr = SqlHelper.ExecuteDataReader(DbName, strCommand, CommandType.Text, outparam))
				{
					array = GetModelList(dr);
				}
				if (array == null)
				{
					array = new TL[0];
				}
				return array;
			}
			catch (Exception ex)
			{
				Log log = new Log();
				log.Write(new LogMessage
				{
					Content = "GLib.Data.Core.Repository.GetModelList(TL model)" + ex.Message,
					Type = MessageType.Error
				});
				return null;
			}
		}

		public virtual TL[] GetModelList(string selector, params DBParam[] values)
		{
			return GetModelList(selector, string.Empty, values);
		}

		public virtual TL[] GetModelList(string selector, string ordering, params DBParam[] values)
		{
			return GetModelList(selector, ordering, string.Empty, values);
		}

		public virtual TL[] GetModelList(string selector, string ordering, string colList, params DBParam[] values)
		{
			return GetModelList(selector, ordering, colList, 0, PageSize, values);
		}

		public virtual TL[] GetModelList(string selector, string ordering, string colList, int pageIndex, int pageSize, params DBParam[] values)
		{
			if (pageIndex <= 0)
			{
				pageIndex = 1;
			}
			if (pageSize <= 0)
			{
				pageSize = PageSize;
			}
			try
			{
				string strCommand = creator.CreatePageSql(selector, pageIndex, pageSize, ordering, colList, values);
				TL[] array = null;
				using (IDataReader dr = SqlHelper.ExecuteDataReader(DbName, strCommand, CommandType.Text, values))
				{
					array = GetModelList(dr);
				}
				if (array == null)
				{
					array = new TL[0];
				}
				return array;
			}
			catch (Exception ex)
			{
				Log log = new Log();
				log.Write(new LogMessage
				{
					Content = "GLib.Data.Core.Repository. GetModelList(String selector, String ordering, String colList, Int32 pageIndex, Int32 pageSize, params DBParam[] values)：\r\n " + ex.Message,
					Type = MessageType.Error
				});
				return null;
			}
		}

		public virtual PageList<TL> GetPageList(string selector, string ordering, string colList, int pageIndex, int pageSize, params DBParam[] values)
		{
			if (pageIndex <= 0)
			{
				pageIndex = 1;
			}
			if (pageSize <= 0)
			{
				pageSize = PageSize;
			}
			PageList<TL> pageList = new PageList<TL>(pageIndex, pageSize, 0);
			string str = creator.CreatePageSql(selector, pageIndex, pageSize, ordering, colList, values);
			string str2 = creator.CreateCountSql(selector, string.Empty);
			try
			{
				List<TL> list = new List<TL>();
				using (IDataReader dataReader = SqlHelper.ExecuteDataReader(DbName, str + "  " + str2, CommandType.Text, values))
				{
					while (dataReader.Read())
					{
						list.Add(GetModel(dataReader));
					}
					if (!dataReader.IsClosed)
					{
						dataReader.Close();
					}
				}
				pageList.TotalCount = GetModelListCount(selector, values);
				pageList.AddRange(list);
				return pageList;
			}
			catch (Exception ex)
			{
				Log log = new Log();
				log.Write(new LogMessage
				{
					Content = "GLib.Data.Core.Repository.GetPageList(String selector, String ordering, String colList, int pageIndex, int pageSize, params DBParam[] values)：\r\n " + ex.Message,
					Type = MessageType.Error
				});
				return pageList;
			}
		}

		public virtual TL[] GetModelList(IDataReader dr)
		{
			List<TL> list = new List<TL>();
			if (dr != null)
			{
				while (dr.Read())
				{
					list.Add(GetModel(dr));
				}
				dr?.Close();
			}
			return list.ToArray();
		}

		public virtual int GetModelListCount(string selector, params DBParam[] values)
		{
			int num = 0;
			try
			{
				string strCommand = creator.CreateCountSql(selector, string.Empty);
				return int.Parse(SqlHelper.ExecuteScalar(DbName, strCommand, CommandType.Text, values).ToString());
			}
			catch (Exception ex)
			{
				Log log = new Log();
				log.Write(new LogMessage
				{
					Content = "GLib.Data.Core.Repository. GetModelListCount(String selector, params DBParam[] values)：\r\n " + ex.Message,
					Type = MessageType.Error
				});
				return 0;
			}
		}

		public virtual bool UpdateModel(TL model)
		{
			return UpdateModel(model, string.Empty, string.Empty, (DBParam[])null);
		}

		public virtual bool UpdateModel(TL model, string colList)
		{
			return UpdateModel(model, colList, string.Empty, (DBParam[])null);
		}

		public virtual bool UpdateModel(TL model, string selector, params DBParam[] values)
		{
			return UpdateModel(model, string.Empty, selector, values);
		}

		public virtual bool UpdateModel(TL model, string colList, string selector, params DBParam[] values)
		{
			_model = model;
			try
			{
				DBParam[] outparam = null;
				string strCommand = creator.CreateUpdateSql(model, colList, selector, out outparam, values);
				return SqlHelper.ExecuteNonQuery(DbName, strCommand, CommandType.Text, outparam) > 0;
			}
			catch (Exception ex)
			{
				Log log = new Log();
				log.Write(new LogMessage
				{
					Content = "GLib.Data.Core.Repository. UpdateModel(TL model, string colList, string selector, params DBParam[] values)：\r\n " + ex.Message,
					Type = MessageType.Error
				});
				return false;
			}
		}

		public virtual bool DeleteModel(int id)
		{
			return Delete($"{PrimaryKey}={id}", (DBParam[])null);
		}

		public virtual bool DeleteModel(long id)
		{
			return Delete($"{PrimaryKey}={id}", (DBParam[])null);
		}

		public virtual bool DeleteModel(TL model)
		{
			_model = model;
			try
			{
				DBParam[] outparam = null;
				string strCommand = creator.CreateDeleteSql(model, out outparam);
				return SqlHelper.ExecuteNonQuery(DbName, strCommand, CommandType.Text, outparam) > 0;
			}
			catch (Exception ex)
			{
				Log log = new Log();
				log.Write(new LogMessage
				{
					Content = "GLib.Data.Core.Repository.Delete(TL model)：\r\n " + ex.Message,
					Type = MessageType.Error
				});
				return false;
			}
		}

		public virtual bool Delete(string selector, params DBParam[] values)
		{
			try
			{
				string strCommand = creator.CreateDeleteSql(selector);
				return SqlHelper.ExecuteNonQuery(DbName, strCommand, CommandType.Text, values) > 0;
			}
			catch (Exception ex)
			{
				Log log = new Log();
				log.Write(new LogMessage
				{
					Content = "GLib.Data.Core.Repository.Delete(String selector, params DBParam[] values)：\r\n " + ex.Message,
					Type = MessageType.Error
				});
				return false;
			}
		}

		public void Dispose()
		{
			GC.SuppressFinalize(true);
		}
	}
}
