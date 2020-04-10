using System;
using System.Collections.Generic;
using System.Reflection;

namespace Common.Generic
{
	public static class TypeEx
	{
		public static List<PropertyInfo> GetAttributeProperties(this Type type, Type attType)
		{
			PropertyInfo[] properties = type.GetProperties();
			List<PropertyInfo> list = new List<PropertyInfo>();
			PropertyInfo[] array = properties;
			foreach (PropertyInfo propertyInfo in array)
			{
				object[] customAttributes = propertyInfo.GetCustomAttributes(attType, inherit: true);
				if (customAttributes.Length > 0)
				{
					list.Add(propertyInfo);
				}
			}
			return list;
		}

		public static List<FieldInfo> GetAttributeFields(this Type type, Type attType)
		{
			FieldInfo[] fields = type.GetFields();
			List<FieldInfo> list = new List<FieldInfo>();
			FieldInfo[] array = fields;
			foreach (FieldInfo fieldInfo in array)
			{
				object[] customAttributes = fieldInfo.GetCustomAttributes(attType, inherit: true);
				if (customAttributes.Length > 0)
				{
					list.Add(fieldInfo);
				}
			}
			return list;
		}

		public static List<MemberInfo> GetAttributeMembers(this Type type, Type attType)
		{
			MemberInfo[] members = type.GetMembers();
			List<MemberInfo> list = new List<MemberInfo>();
			MemberInfo[] array = members;
			foreach (MemberInfo memberInfo in array)
			{
				object[] customAttributes = memberInfo.GetCustomAttributes(attType, inherit: true);
				if (customAttributes.Length > 0)
				{
					list.Add(memberInfo);
				}
			}
			return list;
		}
	}
}
