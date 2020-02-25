using GLib.Data.Entity;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;

namespace GLib.Data.Core
{
	public class SqlCreator<TL> : ISqlCreator<TL>, IDisposable where TL : ILogicEntity, new()
	{
		private string _PrimaryKey = "";

		private string _TableName = "";

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

		public virtual string TableName
		{
			get
			{
				if (_TableName == "")
				{
					bool flag = false;
					PropertyInfo[] properties = EntityTypeCache.GetEntityInfo(typeof(TL)).Properties;
					for (int i = 0; i < properties.Length; i++)
					{
						if (properties[i].Name.Trim().ToLower() == "TableName".ToLower())
						{
							object value = properties[i].GetValue(Model, null);
							if (value != null && !string.IsNullOrEmpty(value.ToString()))
							{
								_TableName = value.ToString();
								flag = true;
								break;
							}
						}
					}
					if (!flag)
					{
						_TableName = "[" + typeof(TL).Name + "]";
					}
				}
				return _TableName;
			}
			set
			{
				_TableName = value;
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

		public string CreateMaxSql(string selector, string col)
		{
			col = (string.IsNullOrEmpty(col) ? PrimaryKey : col);
			StringBuilder stringBuilder = new StringBuilder();
			stringBuilder.Append($"select max({GetSQLFildList(col)}) from {TableName}");
			if (!string.IsNullOrEmpty(selector))
			{
				stringBuilder.Append(" where " + selector);
			}
			return stringBuilder.ToString();
		}

		public string CreateCountSql(string selector, string col)
		{
			col = (string.IsNullOrEmpty(col) ? "1" : col);
			StringBuilder stringBuilder = new StringBuilder();
			stringBuilder.Append($"select count({col}) as TotalCount from {TableName} ");
			if (!string.IsNullOrEmpty(selector))
			{
				stringBuilder.Append(" where " + selector);
			}
			return stringBuilder.ToString();
		}

		public string CreateSumSql(string selector, string col)
		{
			StringBuilder stringBuilder = new StringBuilder();
			stringBuilder.Append($"select sum({GetSQLFildList(col)}) from {TableName} ");
			if (!string.IsNullOrEmpty(selector))
			{
				stringBuilder.Append(" where " + selector);
			}
			return stringBuilder.ToString();
		}

		public string CreateInsertSql(TL model, out DBParam[] outparam)
		{
			StringBuilder stringBuilder = new StringBuilder();
			StringBuilder stringBuilder2 = new StringBuilder();
			stringBuilder.Append($"insert into {TableName}(");
			PropertyInfo[] properties = EntityTypeCache.GetEntityInfo(typeof(TL)).Properties;
			List<DBParam> list = new List<DBParam>();
			for (int i = 0; i < properties.Length; i++)
			{
				object[] customAttributes = properties[i].GetCustomAttributes(typeof(EntityAttribute), inherit: true);
				EntityAttribute entityAttribute = null;
				if (customAttributes.Length > 0)
				{
					entityAttribute = (EntityAttribute)customAttributes[0];
				}
				if (!model.IsAutoID || !(model.PrimaryKey == properties[i].Name) || entityAttribute == null || !entityAttribute.IsDbGenerated)
				{
					if (properties[i].Name == PrimaryKey && entityAttribute != null && !entityAttribute.IsDbGenerated)
					{
						stringBuilder.Append("[" + properties[i].Name + "],");
						stringBuilder2.Append("@" + properties[i].Name + ",");
						list.Add(new DBParam
						{
							ParamName = properties[i].Name,
							ParamDbType = EntityHelper.GetDbType(properties[i].PropertyType),
							ParamValue = EntityHelper.GetTypeDefaultValue(properties[i].GetValue(model, null), EntityHelper.GetDbType(properties[i].PropertyType))
						});
					}
					else if (properties[i].Name != PrimaryKey && (entityAttribute == null || (!entityAttribute.IsDbGenerated && !entityAttribute.CustomMember)))
					{
						stringBuilder.Append("[" + properties[i].Name + "],");
						stringBuilder2.Append("@" + properties[i].Name + ",");
						list.Add(new DBParam
						{
							ParamName = properties[i].Name,
							ParamDbType = EntityHelper.GetDbType(properties[i].PropertyType),
							ParamValue = EntityHelper.GetTypeDefaultValue(properties[i].GetValue(model, null), EntityHelper.GetDbType(properties[i].PropertyType))
						});
					}
				}
			}
			stringBuilder = stringBuilder.Replace(",", ")", stringBuilder.Length - 1, 1);
			stringBuilder2 = stringBuilder2.Replace(",", ")", stringBuilder2.Length - 1, 1);
			stringBuilder.Append(" values (");
			stringBuilder.Append(stringBuilder2.ToString() + ";");
			if (model.IsAutoID)
			{
				stringBuilder.Append($" select ident_current('{TableName}') ");
			}
			outparam = list.ToArray();
			return stringBuilder.ToString();
		}

		public string CreateUpdateSql(TL model, string colList, string selector, out DBParam[] outparam, params DBParam[] values)
		{
			StringBuilder stringBuilder = new StringBuilder();
			stringBuilder.Append("update  " + TableName + " set ");
			PropertyInfo[] properties = EntityTypeCache.GetEntityInfo(typeof(TL)).Properties;
			PropertyInfo propertyInfo = null;
			List<DBParam> list = new List<DBParam>();
			for (int i = 0; i < properties.Length; i++)
			{
				object[] customAttributes = properties[i].GetCustomAttributes(typeof(EntityAttribute), inherit: true);
				EntityAttribute entityAttribute = null;
				if (customAttributes.Length > 0)
				{
					entityAttribute = (EntityAttribute)customAttributes[0];
				}
				if (properties[i].Name == PrimaryKey)
				{
					propertyInfo = properties[i];
				}
				else if (properties[i].Name != PrimaryKey && !(entityAttribute?.CustomMember ?? false) && (string.IsNullOrEmpty(colList) || colList.IndexOf(properties[i].Name.Trim()) >= 0) && !EntityHelper.IsTypeMinValue(properties[i].GetValue(model, null), EntityHelper.GetDbType(properties[i].PropertyType)))
				{
					stringBuilder.Append("[" + properties[i].Name + "]=@" + properties[i].Name + ",");
					list.Add(new DBParam
					{
						ParamDbType = EntityHelper.GetDbType(properties[i].PropertyType),
						ParamName = properties[i].Name,
						ParamValue = properties[i].GetValue(model, null)
					});
				}
			}
			stringBuilder = stringBuilder.Replace(",", " ", stringBuilder.Length - 1, 1);
			if (!string.IsNullOrEmpty(selector))
			{
				stringBuilder.Append(" where 1=1  and " + selector);
				if (values != null)
				{
					for (int i = 0; i < values.Length; i++)
					{
						list.Insert(i, values[i]);
					}
				}
			}
			else
			{
				stringBuilder.Append(" where [" + PrimaryKey + "]=@" + PrimaryKey);
				list.Add(new DBParam
				{
					ParamDbType = EntityHelper.GetDbType(propertyInfo.PropertyType),
					ParamName = propertyInfo.Name,
					ParamValue = propertyInfo.GetValue(model, null)
				});
			}
			outparam = list.ToArray();
			return stringBuilder.ToString();
		}

		public string CreateDeleteSql(TL model, out DBParam[] outparam)
		{
			StringBuilder stringBuilder = new StringBuilder();
			PropertyInfo[] properties = EntityTypeCache.GetEntityInfo(typeof(TL)).Properties;
			PropertyInfo propertyInfo = null;
			List<DBParam> list = new List<DBParam>();
			for (int i = 0; i < properties.Length; i++)
			{
				object[] customAttributes = properties[i].GetCustomAttributes(typeof(EntityAttribute), inherit: true);
				EntityAttribute entityAttribute = null;
				if (customAttributes.Length > 0)
				{
					entityAttribute = (EntityAttribute)customAttributes[0];
				}
				if (properties[i].Name == PrimaryKey)
				{
					propertyInfo = properties[i];
				}
			}
			stringBuilder.Append("  " + PrimaryKey + "=@" + PrimaryKey);
			list.Add(new DBParam
			{
				ParamDbType = EntityHelper.GetDbType(propertyInfo.PropertyType),
				ParamName = propertyInfo.Name,
				ParamValue = propertyInfo.GetValue(model, null)
			});
			outparam = list.ToArray();
			return CreateDeleteSql(stringBuilder.ToString());
		}

		public string CreateDeleteSql(string selector)
		{
			StringBuilder stringBuilder = new StringBuilder();
			stringBuilder.Append("delete from " + TableName);
			if (!string.IsNullOrEmpty(selector))
			{
				stringBuilder.Append(" where " + selector);
			}
			return stringBuilder.ToString();
		}

		public string CreateModelSql(TL model, out DBParam[] outparam)
		{
			PropertyInfo[] properties = EntityTypeCache.GetEntityInfo(typeof(TL)).Properties;
			List<DBParam> list = new List<DBParam>();
			for (int i = 0; i < properties.Length; i++)
			{
				if (properties[i].Name == PrimaryKey)
				{
					list.Add(new DBParam
					{
						ParamName = properties[i].Name,
						ParamDbType = EntityHelper.GetDbType(properties[i].PropertyType),
						ParamValue = properties[i].GetValue(model, null)
					});
					break;
				}
			}
			outparam = list.ToArray();
			return CreateModelSql(string.Format("{0}=@{0}", PrimaryKey), string.Empty);
		}

		public string CreateModelSql(string selector, string colList)
		{
			StringBuilder stringBuilder = new StringBuilder();
			if (string.IsNullOrEmpty(colList))
			{
				stringBuilder.Append(string.Format("select top 1 {0} from {1}", "*", TableName));
			}
			else
			{
				stringBuilder.Append(string.Format("select top 1 {0} from {1}", colList.Replace("new", ""), TableName));
			}
			if (!string.IsNullOrEmpty(selector))
			{
				stringBuilder.Append("  where " + selector);
			}
			return stringBuilder.ToString();
		}

		public string CreatePageSql(TL model, out DBParam[] outparam)
		{
			PropertyInfo[] properties = EntityTypeCache.GetEntityInfo(typeof(TL)).Properties;
			List<DBParam> list = new List<DBParam>();
			for (int i = 0; i < properties.Length; i++)
			{
				if (properties[i].Name == PrimaryKey)
				{
					list.Add(new DBParam
					{
						ParamName = properties[i].Name,
						ParamDbType = EntityHelper.GetDbType(properties[i].PropertyType),
						ParamValue = properties[i].GetValue(model, null)
					});
					break;
				}
			}
			outparam = list.ToArray();
			return CreatePageSql(string.Format("{0}=@{0}", PrimaryKey), 0, PageSize, string.Empty, string.Empty, outparam);
		}

		public string CreatePageSql(string selector, int pageIndex, int pageSize, string ordering, string colList, params DBParam[] values)
		{
			pageIndex = ((pageIndex <= 0) ? 1 : pageIndex);
			if (pageSize == 0)
			{
				pageSize = PageSize;
			}
			string text = "";
			if (string.IsNullOrEmpty(colList) || colList == "*")
			{
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
					if (!(entityAttribute?.CustomMember ?? false))
					{
						text = text + "[" + propertyInfo.Name + "],";
					}
				}
				text = text.Substring(0, text.Length - 1);
			}
			else
			{
				text = GetSQLFildList(colList);
			}
			StringBuilder stringBuilder = new StringBuilder();
			string text2;
			if (string.IsNullOrEmpty(ordering))
			{
				text2 = ((!string.IsNullOrEmpty(PrimaryKey)) ? $" order by {PrimaryKey} desc" : string.Format(" order by {0} desc", "newid()"));
			}
			else
			{
				ordering = ordering.Replace("descending", "desc").Replace("ascending", "asc");
				text2 = $" order by {ordering}";
			}
			if (string.IsNullOrEmpty(selector))
			{
				stringBuilder.Append($"select {text} from(select {text}, row_number() over({text2}) as row from {TableName}");
				stringBuilder.Append($") a where row between {(pageIndex - 1) * pageSize + 1} and {pageIndex * pageSize}");
			}
			else
			{
				stringBuilder.Append($"select {text} from(select {text}, row_number() over({text2}) as row from {TableName}");
				stringBuilder.Append($" where {SqlHelper.ParseSelector(selector, values)}");
				stringBuilder.Append($") a where row between {(pageIndex - 1) * pageSize + 1} and {pageIndex * pageSize}");
			}
			return stringBuilder.ToString();
		}

		public void Dispose()
		{
			GC.SuppressFinalize(this);
		}

		private string GetSQLFildList(string fldList)
		{
			if (string.IsNullOrEmpty(fldList))
			{
				return "*";
			}
			if (fldList.Trim() == "*")
			{
				return "*";
			}
			fldList = "[" + fldList + "]";
			fldList = fldList.Replace('，', ',');
			fldList = fldList.Replace(",", "],[");
			return fldList;
		}
	}
}
