using System.Collections.Generic;
using System.Reflection;

namespace GLib.Data.Entity
{
	public class EntityInfo
	{
		private EntityAttribute[] columns;

		private IDictionary<string, EntityAttribute> dicColumns = new Dictionary<string, EntityAttribute>();

		private FieldInfo[] fields;

		private IDictionary<string, FieldInfo> dicFields = new Dictionary<string, FieldInfo>();

		private PropertyInfo[] properties;

		private IDictionary<string, PropertyInfo> dicProperties = new Dictionary<string, PropertyInfo>();

		public EntityAttribute[] Columns
		{
			get
			{
				return columns;
			}
			set
			{
				columns = value;
			}
		}

		public FieldInfo[] Fields
		{
			get
			{
				return fields;
			}
			set
			{
				fields = value;
			}
		}

		public PropertyInfo[] Properties
		{
			get
			{
				return properties;
			}
			set
			{
				properties = value;
			}
		}

		public IDictionary<string, EntityAttribute> DicColumns
		{
			get
			{
				return dicColumns;
			}
			set
			{
				dicColumns = value;
			}
		}

		public IDictionary<string, FieldInfo> DicFields
		{
			get
			{
				return dicFields;
			}
			set
			{
				dicFields = value;
			}
		}

		public IDictionary<string, PropertyInfo> DicProperties
		{
			get
			{
				return dicProperties;
			}
			set
			{
				dicProperties = value;
			}
		}

		public EntityInfo()
		{
		}

		public EntityInfo(EntityAttribute[] columns, FieldInfo[] fields, PropertyInfo[] properties)
		{
			this.columns = columns;
			this.fields = fields;
			this.properties = properties;
		}
	}
}
