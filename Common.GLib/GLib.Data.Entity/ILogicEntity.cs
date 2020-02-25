namespace GLib.Data.Entity
{
	public interface ILogicEntity
	{
		[Entity(CustomMember = true)]
		string PrimaryKey
		{
			get;
			set;
		}

		[Entity(CustomMember = true)]
		bool IsAutoID
		{
			get;
			set;
		}

		[Entity(CustomMember = true)]
		string DbName
		{
			get;
			set;
		}
	}
}
