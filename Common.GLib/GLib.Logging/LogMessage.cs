using System;

namespace GLib.Logging
{
	public class LogMessage
	{
		private DateTime _time;

		private string _content;

		private MessageType _type;

		public DateTime Time
		{
			get
			{
				return _time;
			}
			set
			{
				_time = value;
			}
		}

		public string Content
		{
			get
			{
				return _content;
			}
			set
			{
				_content = value;
			}
		}

		public MessageType Type
		{
			get
			{
				return _type;
			}
			set
			{
				_type = value;
			}
		}

		public LogMessage()
			: this("", MessageType.Unkown)
		{
		}

		public LogMessage(string content, MessageType type)
			: this(DateTime.Now, content, type)
		{
		}

		public LogMessage(DateTime time, string content, MessageType type)
		{
			_time = time;
			_content = content;
			_type = type;
		}

		public override string ToString()
		{
			return _time.ToString() + "\t" + _content + "\t";
		}
	}
}
