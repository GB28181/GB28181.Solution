using System;
using System.Collections.Generic;
using System.Reflection;

namespace GLib.Data.Entity
{
	public static class EntityTypeCache
	{
		private static IDictionary<Type, EntityInfo> cache;

		static EntityTypeCache()
		{
			cache = null;
			cache = new Dictionary<Type, EntityInfo>();
		}

		public static void InsertEntityInfo(Type type, EntityInfo entityInfo)
		{
			if (!cache.ContainsKey(type))
			{
				cache.Add(type, entityInfo);
			}
		}

		public static void InsertEntityInfo(ILogicEntity entity, EntityInfo entityInfo)
		{
			Type type = entity.GetType();
			InsertEntityInfo(type, entityInfo);
		}

		public static EntityInfo GetEntityInfo(Type type)
		{
			if (cache.ContainsKey(type))
			{
				return cache[type];
			}
			EntityInfo entityInfo = new EntityInfo(null, null, type.GetProperties(BindingFlags.DeclaredOnly | BindingFlags.Instance | BindingFlags.Public));
			InsertEntityInfo(type, entityInfo);
			return entityInfo;
		}

		public static EntityInfo GetEntityInfo(ILogicEntity entity)
		{
			Type type = entity.GetType();
			return GetEntityInfo(type);
		}

		public static EntityInfo GetEntityInfo<T>() where T : ILogicEntity
		{
			Type typeFromHandle = typeof(T);
			return GetEntityInfo(typeFromHandle);
		}
	}
}
