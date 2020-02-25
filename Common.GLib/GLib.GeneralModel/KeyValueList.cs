using System;
using System.Collections.Generic;
using System.Linq;

namespace GLib.GeneralModel
{
	[Serializable]
	public class KeyValueList<TKey, UValue> : List<KeyValue<TKey, UValue>>
	{
		public UValue this[TKey key]
		{
			get
			{
				KeyValue<TKey, UValue> keyValue = Enumerable.FirstOrDefault(this, (KeyValue<TKey, UValue> p) => p.Key.Equals(key));
				if (keyValue != null)
				{
					return keyValue.Value;
				}
				return default(UValue);
			}
			set
			{
				KeyValue<TKey, UValue> keyValue = Enumerable.FirstOrDefault(this, (KeyValue<TKey, UValue> p) => p.Key.Equals(key));
				if (keyValue != null)
				{
					keyValue.Value = value;
				}
				else
				{
					Add(new KeyValue<TKey, UValue>(key, value));
				}
			}
		}

		public KeyValueList()
		{
		}

		public KeyValueList(IEnumerable<KeyValue<TKey, UValue>> collection)
			: base(collection)
		{
		}
	}
}
