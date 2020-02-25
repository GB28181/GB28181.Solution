using System;
using System.Collections.Generic;
using System.Reflection;

namespace GLib
{
	[AttributeUsage(AttributeTargets.All)]
	public class DescriptionAttribute : Attribute
	{
		public string Description
		{
			get;
			set;
		}

		public string Name
		{
			get;
			set;
		}

		public object DefaultValue
		{
			get;
			set;
		}

		public static DescriptionAttribute GetDescription(object value)
		{
			return GetDescription(value.GetType(), DescriptionObjectType.Type, null, inherit: false);
		}

		public static DescriptionAttribute GetDescription(Type attributeType)
		{
			return GetDescription(attributeType, DescriptionObjectType.Type, null, inherit: false);
		}

		public static DescriptionAttribute GetDescription(Type attributeType, bool inherit)
		{
			return GetDescription(attributeType, DescriptionObjectType.Type, null, inherit);
		}

		public static DescriptionAttribute GetDescription(Type attributeType, DescriptionObjectType objectType, string name)
		{
			return GetDescription(attributeType, objectType, name, inherit: false);
		}

		public static DescriptionAttribute GetDescription(Type attributeType, DescriptionObjectType objectType, string name, bool inherit)
		{
			MemberInfo memberInfo = null;
			object[] array = null;
			Type typeFromHandle = typeof(DescriptionAttribute);
			switch (objectType)
			{
			case DescriptionObjectType.Event:
				memberInfo = attributeType.GetEvent(name);
				break;
			case DescriptionObjectType.Field:
				memberInfo = attributeType.GetField(name);
				break;
			case DescriptionObjectType.Method:
				memberInfo = attributeType.GetMethod(name);
				break;
			case DescriptionObjectType.Property:
				memberInfo = attributeType.GetProperty(name);
				break;
			}
			array = ((DescriptionObjectType.Type != objectType) ? memberInfo.GetCustomAttributes(typeFromHandle, inherit) : attributeType.GetCustomAttributes(typeFromHandle, inherit));
			if (array != null && array.Length > 0)
			{
				return (DescriptionAttribute)array[0];
			}
			return null;
		}

		public static DescriptionAttribute GetEnumDescription(object value)
		{
			Type type = value.GetType();
			if (!type.IsEnum)
			{
				throw new ArgumentNullException("参数类型必须为枚举类型");
			}
			string[] array = value.ToString().Split(',');
			IList<DescriptionAttribute> list = new List<DescriptionAttribute>();
			string[] array2 = array;
			foreach (string text in array2)
			{
				if (!(text.Trim() != ""))
				{
					continue;
				}
				FieldInfo field = type.GetField(text.Trim());
				if (field != null && field.GetCustomAttributes(typeof(AuxiliaryFieldAttribute), inherit: true).Length == 0)
				{
					object[] customAttributes = field.GetCustomAttributes(typeof(DescriptionAttribute), inherit: true);
					if (customAttributes.Length > 0)
					{
						list.Add((DescriptionAttribute)customAttributes[0]);
					}
				}
			}
			if (list.Count > 1)
			{
				DescriptionAttribute descriptionAttribute = new DescriptionAttribute();
				int num = 0;
				foreach (DescriptionAttribute item in list)
				{
					if (num == 0)
					{
						descriptionAttribute = item;
					}
					else
					{
						DescriptionAttribute descriptionAttribute2 = descriptionAttribute;
						descriptionAttribute2.Name = descriptionAttribute2.Name + "," + item.Name;
					}
					num++;
				}
				return descriptionAttribute;
			}
			if (list.Count > 0)
			{
				return list[0];
			}
			return null;
		}

		public override string ToString()
		{
			return Name;
		}
	}
}
