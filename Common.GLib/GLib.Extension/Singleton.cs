namespace GLib.Extension
{
	public class Singleton<T> where T : new()
	{
		private static T instance__ = default(T);

		private static object syncRoot__ = new object();

		public static T Instance
		{
			get
			{
				if (instance__ == null)
				{
					lock (syncRoot__)
					{
						if (instance__ == null)
						{
							instance__ = new T();
						}
					}
				}
				return instance__;
			}
		}
	}
}
