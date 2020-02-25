using GLib.GeneralModel;
using System;
using System.Collections.Generic;

namespace GLib.AXLib.Utility
{
	public class ObjectPool<T>
	{
		public class Item
		{
			private int _refCount = 0;

			private object _lockObj = new object();

			private ObjectPool<T> _control = null;

			public T Object
			{
				get;
				private set;
			}

			public Item(ObjectPool<T> control, T @object)
			{
				Object = @object;
				_control = control;
			}

			public void AddRef()
			{
				lock (_lockObj)
				{
					_refCount++;
					if (_refCount > 1)
					{
						throw new Exception();
					}
				}
			}

			public void DelRef()
			{
				lock (_lockObj)
				{
					_refCount--;
					if (_refCount < 0)
					{
						throw new Exception();
					}
					if (_refCount == 0)
					{
						_control.ReleaseItem(this);
					}
				}
			}
		}

		private int _min = 0;

		private int _max = 0;

		private int _count = 0;

		private AQueue<Item> _qIdle = new AQueue<Item>();

		private List<Item> _listUsed = new List<Item>();

		private Func<T> _getObjectItemCallback = null;

		public ObjectPool(int count, Func<T> getObjectItemCallback)
			: this(count, count, getObjectItemCallback)
		{
		}

		public ObjectPool(int min, int max, Func<T> getObjectItemCallback)
		{
			_min = min;
			_max = max;
			_getObjectItemCallback = getObjectItemCallback;
			Init();
		}

		public ObjectPool(T[] arr)
		{
			_min = arr.Length;
			_max = arr.Length;
			lock (_qIdle)
			{
				foreach (T @object in arr)
				{
					Item item = new Item(this, @object);
					_qIdle.Enqueue(item);
					_count++;
				}
			}
		}

		private void Init()
		{
			for (int i = 0; i < _min; i++)
			{
				AddItem();
			}
		}

		private void AddItem()
		{
			lock (_qIdle)
			{
				Item item = new Item(this, _getObjectItemCallback());
				_qIdle.Enqueue(item);
				_count++;
			}
		}

		public Item AllowItem()
		{
			lock (_qIdle)
			{
				if (_qIdle.Count > 0)
				{
					Item item = _qIdle.Dequeue();
					item.AddRef();
					lock (_listUsed)
					{
						if (!_listUsed.Contains(item))
						{
							_listUsed.Add(item);
						}
					}
					return item;
				}
				if (_count < _max)
				{
					AddItem();
					return AllowItem();
				}
				return null;
			}
		}

		public void ReleaseItem(Item item)
		{
			lock (_qIdle)
			{
				lock (_listUsed)
				{
					_listUsed.Remove(item);
				}
				_qIdle.Enqueue(item);
			}
		}
	}
}
