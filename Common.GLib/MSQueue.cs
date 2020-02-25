using System.Threading;

public class MSQueue<T>
{
	private class node_t
	{
		public T value;

		public pointer_t next;
	}

	private struct pointer_t
	{
		public long count;

		public node_t ptr;

		public pointer_t(pointer_t p)
		{
			ptr = p.ptr;
			count = p.count;
		}

		public pointer_t(node_t node, long c)
		{
			ptr = node;
			count = c;
		}
	}

	private pointer_t Head;

	private pointer_t Tail;

	public int Count;

	public MSQueue()
	{
		node_t ptr = new node_t();
		Head.ptr = (Tail.ptr = ptr);
		Count = 0;
	}

	private bool CAS(ref pointer_t destination, pointer_t compared, pointer_t exchange)
	{
		if (compared.ptr == Interlocked.CompareExchange(ref destination.ptr, exchange.ptr, compared.ptr))
		{
			Interlocked.Exchange(ref destination.count, exchange.count);
			return true;
		}
		return false;
	}

	public bool deque(ref T t)
	{
		bool flag = true;
		while (flag)
		{
			pointer_t head = Head;
			pointer_t tail = Tail;
			pointer_t next = head.ptr.next;
			if (head.count != Head.count || head.ptr != Head.ptr)
			{
				continue;
			}
			if (head.ptr == tail.ptr)
			{
				if (null == next.ptr)
				{
					return false;
				}
				CAS(ref Tail, tail, new pointer_t(next.ptr, tail.count + 1));
			}
			else
			{
				t = next.ptr.value;
				if (CAS(ref Head, head, new pointer_t(next.ptr, head.count + 1)))
				{
					flag = false;
				}
			}
		}
		Interlocked.Decrement(ref Count);
		return true;
	}

	public void enqueue(T t)
	{
		node_t node_t = new node_t();
		node_t.value = t;
		bool flag = true;
		while (flag)
		{
			pointer_t tail = Tail;
			pointer_t next = tail.ptr.next;
			if (tail.count != Tail.count || tail.ptr != Tail.ptr)
			{
				continue;
			}
			if (null == next.ptr)
			{
				if (CAS(ref tail.ptr.next, next, new pointer_t(node_t, next.count + 1)))
				{
					Interlocked.Increment(ref Count);
					flag = false;
				}
			}
			else
			{
				CAS(ref Tail, tail, new pointer_t(next.ptr, tail.count + 1));
			}
		}
	}

	public T Dequeue()
	{
		T t = default(T);
		deque(ref t);
		return t;
	}

	public void Enqueue(T item)
	{
		enqueue(item);
	}
}
