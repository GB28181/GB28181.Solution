using System;
using System.Linq;
using System.Reflection;

namespace GLib.Extension
{
	public static class ReflectionEx
	{
		public static void EvaluationByFields(object o1, object o2)
		{
			EvaluationByFields(o1, o2, null);
		}

		public static void EvaluationByFields(object o1, object o2, Type attType)
		{
			if (o1 == null || o2 == null)
			{
				throw new Exception();
			}
			Type type = o1.GetType();
			Type type2 = o2.GetType();
			FieldInfo[] array = type.GetFields();
			if (attType != null)
			{
				array = type.GetAttributeFields(attType).ToArray();
			}
			FieldInfo[] array2 = type2.GetFields();
			if (attType != null)
			{
				array2 = type2.GetAttributeFields(attType).ToArray();
			}
			FieldInfo[] array3 = array;
			foreach (FieldInfo f1 in array3)
			{
				FieldInfo[] source = array2;
				Func<FieldInfo, bool> predicate = (FieldInfo p) => p.Name == f1.Name;
				FieldInfo fieldInfo = source.FirstOrDefault(predicate);
				if (fieldInfo != null)
				{
					f1.SetValue(o1, fieldInfo.GetValue(o2));
				}
			}
		}

		public static void RefSetFieldsByFields(object dts, object src)
		{
			RefSetFieldsByFields(dts, src, null);
		}

		public static void RefSetFieldsByFields(object dts, object src, Type attType)
		{
			if (dts == null || src == null)
			{
				throw new Exception();
			}
			Type type = dts.GetType();
			Type type2 = src.GetType();
			FieldInfo[] array = type.GetFields();
			if (attType != null)
			{
				array = type.GetAttributeFields(attType).ToArray();
			}
			FieldInfo[] array2 = type2.GetFields();
			if (attType != null)
			{
				array2 = type2.GetAttributeFields(attType).ToArray();
			}
			FieldInfo[] array3 = array;
			foreach (FieldInfo f1 in array3)
			{
				FieldInfo[] source = array2;
				Func<FieldInfo, bool> predicate = (FieldInfo p) => p.Name == f1.Name;
				FieldInfo fieldInfo = source.FirstOrDefault(predicate);
				if (fieldInfo != null)
				{
					f1.SetValue(dts, fieldInfo.GetValue(src));
				}
			}
		}

		public static void RefSetPropertiesByProperties(object o1, object o2, Type attType)
		{
			if (o1 == null || o2 == null)
			{
				throw new Exception();
			}
			Type type = o1.GetType();
			Type type2 = o2.GetType();
			PropertyInfo[] array = type.GetProperties();
			if (attType != null)
			{
				array = type.GetAttributeProperties(attType).ToArray();
			}
			PropertyInfo[] array2 = type2.GetProperties();
			if (attType != null)
			{
				array2 = type2.GetAttributeProperties(attType).ToArray();
			}
			PropertyInfo[] array3 = array;
			foreach (PropertyInfo f1 in array3)
			{
				PropertyInfo[] source = array2;
				Func<PropertyInfo, bool> predicate = (PropertyInfo p) => p.Name == f1.Name;
				PropertyInfo propertyInfo = source.FirstOrDefault(predicate);
				if (propertyInfo != null)
				{
					f1.SetValue(o1, propertyInfo.GetValue(o2, null), null);
				}
			}
		}

		public static void RefSetFieldsByProperties(object o1, object o2, Type attType)
		{
			if (o1 == null || o2 == null)
			{
				throw new Exception();
			}
			Type type = o1.GetType();
			Type type2 = o2.GetType();
			FieldInfo[] array = type.GetFields();
			if (attType != null)
			{
				array = type.GetAttributeFields(attType).ToArray();
			}
			PropertyInfo[] array2 = type2.GetProperties();
			if (attType != null)
			{
				array2 = type2.GetAttributeProperties(attType).ToArray();
			}
			FieldInfo[] array3 = array;
			foreach (FieldInfo f1 in array3)
			{
				PropertyInfo[] source = array2;
				Func<PropertyInfo, bool> predicate = (PropertyInfo p) => p.Name == f1.Name;
				PropertyInfo propertyInfo = source.FirstOrDefault(predicate);
				if (propertyInfo != null)
				{
					f1.SetValue(o1, propertyInfo.GetValue(o2, null));
				}
			}
		}

		public static void RefSetPropertiesByFields(object o1, object o2, Type attType)
		{
			if (o1 == null || o2 == null)
			{
				throw new Exception();
			}
			Type type = o1.GetType();
			Type type2 = o2.GetType();
			PropertyInfo[] array = type.GetProperties();
			if (attType != null)
			{
				array = type.GetAttributeProperties(attType).ToArray();
			}
			FieldInfo[] array2 = type2.GetFields();
			if (attType != null)
			{
				array2 = type.GetAttributeFields(attType).ToArray();
			}
			PropertyInfo[] array3 = array;
			foreach (PropertyInfo f1 in array3)
			{
				FieldInfo[] source = array2;
				Func<FieldInfo, bool> predicate = (FieldInfo p) => p.Name == f1.Name;
				FieldInfo fieldInfo = source.FirstOrDefault(predicate);
				if (fieldInfo != null)
				{
					f1.SetValue(o1, fieldInfo.GetValue(o2), null);
				}
			}
		}
	}
}
