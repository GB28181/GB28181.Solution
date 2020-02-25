using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Net;
using System.Net.Mail;
using System.Text;

namespace GLib.Utilities
{
	public class EmailSender
	{
		private IList<string> mailAttachmentList = new List<string>();

		private string message = "";

		private SendState sendState = SendState.Sending;

		private StringCollection bcc = new StringCollection();

		private StringCollection cc = new StringCollection();

		private StringCollection to = new StringCollection();

		public string Server
		{
			get;
			set;
		}

		public int ServerPort
		{
			get;
			set;
		}

		public string UserName
		{
			get;
			set;
		}

		public string Password
		{
			get;
			set;
		}

		public string From
		{
			get;
			set;
		}

		public string FromName
		{
			get;
			set;
		}

		public string Body
		{
			get;
			set;
		}

		public Encoding BodyEncoding
		{
			get;
			set;
		}

		public bool IsBodyHtml
		{
			get;
			set;
		}

		public MailPriority Priority
		{
			get;
			set;
		}

		public string ReplyTo
		{
			get;
			set;
		}

		public StringCollection Bcc => bcc;

		public StringCollection CC => cc;

		public MailAddress Sender
		{
			get;
			set;
		}

		public string Subject
		{
			get;
			set;
		}

		public Encoding SubjectEncoding
		{
			get;
			set;
		}

		public StringCollection To => to;

		public IList<string> MailAttachmentList => mailAttachmentList;

		public char Delimiter
		{
			get;
			set;
		}

		public string Message => message;

		public SendState SendState => sendState;

		public EmailSender()
		{
			IsBodyHtml = true;
			Priority = MailPriority.High;
			BodyEncoding = Encoding.UTF8;
			SubjectEncoding = Encoding.UTF8;
			ServerPort = 25;
			Delimiter = '=';
		}

		public virtual bool Send()
		{
			if (string.IsNullOrEmpty(From))
			{
				sendState = SendState.Error;
				message = "没有发件人！";
				return false;
			}
			if (string.IsNullOrEmpty(Subject))
			{
				sendState = SendState.Error;
				message = "没有邮件标题！";
				return false;
			}
			if (string.IsNullOrEmpty(Body))
			{
				sendState = SendState.Error;
				message = "没有邮件正文！";
				return false;
			}
			if (To.Count < 1)
			{
				sendState = SendState.Error;
				message = "没有收件人！";
				return false;
			}
			if (string.IsNullOrEmpty(Server))
			{
				sendState = SendState.Error;
				message = "没有设置邮件服务器地址！";
				return false;
			}
			if (string.IsNullOrEmpty(Password))
			{
				sendState = SendState.Error;
				message = "没有设置邮件服务器账号密码！";
				return false;
			}
			if (string.IsNullOrEmpty(UserName))
			{
				UserName = From;
			}
			MailMessage mailMessage = new MailMessage();
			mailMessage.From = new MailAddress(From, FromName);
			mailMessage.Subject = Subject.Trim();
			mailMessage.SubjectEncoding = SubjectEncoding;
			mailMessage.Body = Body.Trim();
			mailMessage.BodyEncoding = BodyEncoding;
			mailMessage.IsBodyHtml = IsBodyHtml;
			foreach (string mailAttachment in mailAttachmentList)
			{
				mailMessage.Attachments.Add(new Attachment(mailAttachment));
			}
			StringEnumerator enumerator2 = To.GetEnumerator();
			try
			{
				while (enumerator2.MoveNext())
				{
					string current2 = enumerator2.Current;
					string[] array = current2.Split(Delimiter);
					MailAddress mailAddress = null;
					mailAddress = ((array.Length <= 1) ? new MailAddress(array[0].Trim()) : new MailAddress(array[0].Trim(), array[1].Trim()));
					mailMessage.To.Add(mailAddress);
				}
			}
			finally
			{
				(enumerator2 as IDisposable)?.Dispose();
			}
			if (Bcc.Count > 0)
			{
				enumerator2 = Bcc.GetEnumerator();
				try
				{
					while (enumerator2.MoveNext())
					{
						string current3 = enumerator2.Current;
						string[] array2 = current3.Split(Delimiter);
						MailAddress mailAddress = null;
						mailAddress = ((array2.Length <= 1) ? new MailAddress(array2[0].Trim()) : new MailAddress(array2[0].Trim(), array2[1].Trim()));
						mailMessage.Bcc.Add(mailAddress);
					}
				}
				finally
				{
					(enumerator2 as IDisposable)?.Dispose();
				}
			}
			if (CC.Count > 0)
			{
				enumerator2 = CC.GetEnumerator();
				try
				{
					while (enumerator2.MoveNext())
					{
						string current3 = enumerator2.Current;
						string[] array3 = current3.Split(Delimiter);
						MailAddress mailAddress = null;
						mailAddress = ((array3.Length <= 1) ? new MailAddress(array3[0].Trim()) : new MailAddress(array3[0].Trim(), array3[1].Trim()));
						mailMessage.CC.Add(mailAddress);
					}
				}
				finally
				{
					(enumerator2 as IDisposable)?.Dispose();
				}
			}
			try
			{
				SmtpClient smtpClient = new SmtpClient(Server, ServerPort);
				smtpClient.Credentials = new NetworkCredential(UserName, Password);
				smtpClient.SendCompleted += SendCompletedCallback;
				string subject = Subject;
				smtpClient.Send(mailMessage);
			}
			catch (Exception ex)
			{
				sendState = SendState.Error;
				message = ex.Message;
				return false;
			}
			mailMessage.Dispose();
			return true;
		}

		private void SendCompletedCallback(object sender, AsyncCompletedEventArgs e)
		{
			string arg = (string)e.UserState;
			if (e.Cancelled)
			{
				sendState = SendState.Canceled;
				message = $"[{arg}] 发送取消";
			}
			if (e.Error != null)
			{
				sendState = SendState.Error;
				message = $"[{arg}] {e.Error.ToString()}";
			}
			else
			{
				sendState = SendState.Succeed;
				message = "发送成功";
			}
		}
	}
}
