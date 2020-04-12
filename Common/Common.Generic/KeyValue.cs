using System;

namespace Common.Generic
{
	[Serializable]
	public class KeyValue<TKey, UValue>
	{
		public TKey Key
		{
			get;
			set;
		}

		public UValue Value
		{
			get;
			set;
		}

		public KeyValue()
		{
		}

		public KeyValue(TKey key, UValue value)
		{
			Key = key;
			Value = value;
		}

		public override string ToString()
		{
			if (Value != null)
			{
				return Value.ToString();
			}
			if (Key != null)
			{
				return Key.ToString();
			}
			return base.ToString();
		}
	}
}
