namespace Common.Networks
{
	public class ByteObjSocket<T> : SocketEx where T : IByteObj, new()
	{
		public virtual T ReadObj()
		{
			int len = ReadInt32();
			byte[] bytes = ReadBytes(len);
			T result = new T();
			result.SetBytes(bytes);
			return result;
		}

		public virtual void Write(T t)
		{
			byte[] bytes = t.GetBytes();
			Write(bytes.Length);
			Write(bytes);
		}
	}
}
