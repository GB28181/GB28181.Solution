using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace Security
{
	public sealed class EncryptHelper
	{
		public static string MD5(string source)
		{
			return GetMD5(source + "WEN@#!&*&*(~)_W#@!!@!^WEN!");
		}

		private static string GetMD5(string source)
		{
			MD5CryptoServiceProvider mD5CryptoServiceProvider = new MD5CryptoServiceProvider();
			mD5CryptoServiceProvider.ComputeHash(Encoding.ASCII.GetBytes(source));
			StringBuilder stringBuilder = new StringBuilder();
			byte[] hash = mD5CryptoServiceProvider.Hash;
			foreach (byte b in hash)
			{
				stringBuilder.AppendFormat("{0:X2}", b);
			}
			return stringBuilder.ToString();
		}

		public static string DES(string source, string key)
		{
			DESCryptoServiceProvider dESCryptoServiceProvider = new DESCryptoServiceProvider();
			byte[] bytes = Encoding.Default.GetBytes(source);
			dESCryptoServiceProvider.Key = Encoding.ASCII.GetBytes(key);
			dESCryptoServiceProvider.IV = Encoding.ASCII.GetBytes(key);
			MemoryStream memoryStream = new MemoryStream();
			CryptoStream cryptoStream = new CryptoStream(memoryStream, dESCryptoServiceProvider.CreateEncryptor(), CryptoStreamMode.Write);
			cryptoStream.Write(bytes, 0, bytes.Length);
			cryptoStream.FlushFinalBlock();
			StringBuilder stringBuilder = new StringBuilder();
			byte[] array = memoryStream.ToArray();
			foreach (byte b in array)
			{
				stringBuilder.AppendFormat("{0:X2}", b);
			}
			return stringBuilder.ToString();
		}

		public static string Base64(string source)
		{
			if (!string.IsNullOrEmpty(source))
			{
				return Convert.ToBase64String(Encoding.Default.GetBytes(source));
			}
			return string.Empty;
		}
	}
}
