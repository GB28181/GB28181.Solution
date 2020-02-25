namespace AXLib.Utility
{
	public interface IAction<T>
	{
		void invoke(T t);
	}
}
