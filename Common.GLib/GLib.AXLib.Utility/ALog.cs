using System;

namespace GLib.AXLib.Utility
{
	public class ALog
	{
		public bool Enabled;

		public bool EnabledT;

		public bool EnabledI;

		public bool EnabledD;

		public bool EnabledE;

		public string Tag;

		private ALog(string tag)
		{
			Tag = tag;
			Enabled = (EnabledT = (EnabledI = (EnabledD = (EnabledE = true))));
		}

		public static ALog Create(string tag)
		{
			return new ALog(tag);
		}

		public static ALog Create(Type type)
		{
			return new ALog(type.Name);
		}

		private void Print(string str)
		{
			Console.WriteLine(str);
		}

		private void Print(Exception e)
		{
			Console.WriteLine(e.ToString());
		}

		public void I(string format, params string[] args)
		{
			if (Enabled && EnabledI)
			{
				Print(string.Format(format, args));
			}
		}

		public void I(Exception e)
		{
			if (Enabled && EnabledI)
			{
				Print(e);
			}
		}

		public void T(string format, params string[] args)
		{
			if (Enabled && EnabledT)
			{
				Print(string.Format(format, args));
			}
		}

		public void T(Exception e)
		{
			if (Enabled && EnabledT)
			{
				Print(e);
			}
		}

		public void D(string format, params string[] args)
		{
			if (Enabled && EnabledD)
			{
				Print(string.Format(format, args));
			}
		}

		public void D(Exception e)
		{
			if (Enabled && EnabledD)
			{
				Print(e);
			}
		}

		public void E(string format, params string[] args)
		{
			if (Enabled && EnabledE)
			{
				Print(string.Format(format, args));
			}
		}

		public void E(Exception e)
		{
			if (Enabled && EnabledE)
			{
				Print(e);
			}
		}
	}
}
