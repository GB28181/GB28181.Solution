using System;

namespace GLib.Web
{
	public abstract class Warn : Exception
	{
		public virtual Warn InnerWarn
		{
			get;
			protected set;
		}

		public virtual WarnDescription Description
		{
			get;
			protected set;
		}

		public Warn(WarnDescription wd)
			: base(wd.Description)
		{
			Description = wd;
		}

		public Warn(WarnDescription wd, Warn inner)
		{
			InnerWarn = inner;
			Description = wd;
		}

		public void Execute()
		{
			if (InnerWarn != null)
			{
				InnerWarn.Execute();
			}
			DoExecute();
		}

		protected abstract void DoExecute();
	}
}
