using System;

namespace GLib.Data.Entity
{
	[AttributeUsage(AttributeTargets.All, AllowMultiple = true, Inherited = true)]
	public class EntityAttribute : Attribute
	{
		private bool _customMember;

		private bool _IsDbGenerated;

		public bool CustomMember
		{
			get
			{
				return _customMember;
			}
			set
			{
				_customMember = value;
			}
		}

		public bool IsDbGenerated
		{
			get
			{
				return _IsDbGenerated;
			}
			set
			{
				_IsDbGenerated = value;
			}
		}
	}
}
