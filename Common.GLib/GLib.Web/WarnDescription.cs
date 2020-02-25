using System;

namespace GLib.Web
{
	public class WarnDescription
	{
		public string Title
		{
			get;
			set;
		}

		public string Description
		{
			get;
			set;
		}

		public int HelpCode
		{
			get;
			set;
		}

		public virtual Exception Original
		{
			get;
			set;
		}

		protected WarnDescription()
		{
		}

		public WarnDescription(string description)
		{
			Description = description;
		}

		public WarnDescription(Exception exc)
		{
			Description = exc.Message;
			Original = exc;
		}
	}
}
