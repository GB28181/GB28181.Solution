using System.Collections.Generic;

namespace Common.Generic
{
	public class AQueue<T> : Queue<T>
	{
		private object _lock = new object();

		public new int Count
		{
			get
			{
				lock (_lock)
				{
					return base.Count;
				}
			}
		}

		public AQueue()
		{
		}

		public AQueue(IEnumerable<T> collection)
			: base(collection)
		{
		}

		public AQueue(int capacity)
			: base(capacity)
		{
		}

		public new T Dequeue()
		{
			lock (_lock)
			{
				return base.Dequeue();
			}
		}

		public new void Enqueue(T item)
		{
			lock (_lock)
			{
				base.Enqueue(item);
			}
		}

		public new T Peek()
		{
			lock (_lock)
			{
				if (base.Count == 0)
				{
					return default(T);
				}
				return base.Peek();
			}
		}

		public new T[] ToArray()
		{
			lock (_lock)
			{
				return base.ToArray();
			}
		}

		public object GetLock()
		{
			return _lock;
		}
	}
}
