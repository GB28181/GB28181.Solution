using Microsoft.Win32;
using System;
using System.Diagnostics;

public class RegistryHelper
{
	public static bool ConfigAutoStart(bool isAutoStart, string[] args)
	{
		try
		{
			string processName = Process.GetCurrentProcess().ProcessName;
			RegistryKey registryKey = null;
			using (registryKey = OpenKeyForWrite("SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Run"))
			{
				if (registryKey == null)
				{
					return false;
				}
				string text = ""; // $"\"{Application.ExecutablePath}\"";
				for (int i = 0; i < args.Length; i++)
				{
					text = ((i != 2) ? (text + " " + args[i]) : (text + " " + $"\"{args[i]}\""));
				}
				if (isAutoStart)
				{
					registryKey.SetValue(processName, text);
				}
				else
				{
					registryKey.DeleteValue(processName, throwOnMissingValue: false);
				}
				registryKey.Close();
				return true;
			}
		}
		catch (Exception)
		{
			return false;
		}
	}

	public static RegistryKey OpenKeyForWrite(string subKey)
	{
		return OpenKeyForWrite(Registry.LocalMachine, subKey);
	}

	public static RegistryKey OpenKeyForWrite(RegistryKey key, string subKey)
	{
		RegistryKey registryKey = null;
		registryKey = key.OpenSubKey(subKey, writable: true);
		if (registryKey == null)
		{
			registryKey = key.CreateSubKey(subKey);
		}
		return registryKey;
	}

	public static void WriteAutoLoginWindowRegistryKey(bool isAutoLogin, string defaultUserName, string defaultPassword)
	{
		try
		{
			RegistryKey registryKey = Registry.LocalMachine.OpenSubKey("SOFTWARE\\Microsoft\\Windows NT\\CurrentVersion\\Winlogon", writable: true);
			if (isAutoLogin)
			{
				registryKey.SetValue("AutoAdminLogon", "1");
				registryKey.SetValue("DefaultUserName", defaultUserName);
				registryKey.SetValue("DefaultPassword", defaultPassword);
			}
			else
			{
				registryKey.SetValue("AutoAdminLogon", "0");
				registryKey.SetValue("DefaultPassword", string.Empty);
			}
			registryKey.Close();
		}
		catch (Exception)
		{
		}
	}
}
