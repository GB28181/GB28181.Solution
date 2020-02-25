using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace GLib.Utilities.Security
{
	public sealed class DecryptHelper
	{
		public static string DES(string source, string key)
		{
			DESCryptoServiceProvider dESCryptoServiceProvider = new DESCryptoServiceProvider();
			byte[] array = new byte[source.Length / 2];
			for (int i = 0; i < source.Length / 2; i++)
			{
				int num = Convert.ToInt32(source.Substring(i * 2, 2), 16);
				array[i] = (byte)num;
			}
			dESCryptoServiceProvider.Key = Encoding.ASCII.GetBytes(key);
			dESCryptoServiceProvider.IV = Encoding.ASCII.GetBytes(key);
			MemoryStream memoryStream = new MemoryStream();
			CryptoStream cryptoStream = new CryptoStream(memoryStream, dESCryptoServiceProvider.CreateDecryptor(), CryptoStreamMode.Write);
			cryptoStream.Write(array, 0, array.Length);
			cryptoStream.FlushFinalBlock();
			return Encoding.Default.GetString(memoryStream.ToArray());
		}

		public static string Base64(string source)
		{
			if (!string.IsNullOrEmpty(source))
			{
				return Encoding.ASCII.GetString(Convert.FromBase64String(source));
			}
			return string.Empty;
		}
	}
}
