using System;
using System.Management;

namespace GLib.Utilities
{
	public class Computer
	{
		public string CpuID;

		public string MacAddress;

		public string DiskID;

		public string LoginUserName;

		public string ComputerName;

		public string SystemType;

		public string TotalPhysicalMemory;

		private static Computer _instance;

		public static Computer Instance()
		{
			if (_instance == null)
			{
				_instance = new Computer();
			}
			return _instance;
		}

		public Computer()
		{
			CpuID = GetCpuID();
			MacAddress = GetMacAddress();
			DiskID = GetDiskID();
			LoginUserName = GetUserName();
			SystemType = GetSystemType();
			TotalPhysicalMemory = GetTotalPhysicalMemory();
			ComputerName = GetComputerName();
		}

		private string GetCpuID()
		{
			try
			{
				string result = "";
				ManagementClass managementClass = new ManagementClass("Win32_Processor");
				ManagementObjectCollection instances = managementClass.GetInstances();
				foreach (ManagementObject item in instances)
				{
					result = item.Properties["ProcessorId"].Value.ToString();
				}
				instances = null;
				managementClass = null;
				return result;
			}
			catch
			{
				return "unknow";
			}
			finally
			{
			}
		}

		private string GetMacAddress()
		{
			try
			{
				string result = "";
				ManagementClass managementClass = new ManagementClass("Win32_NetworkAdapterConfiguration");
				ManagementObjectCollection instances = managementClass.GetInstances();
				foreach (ManagementObject item in instances)
				{
					if ((bool)item["IPEnabled"])
					{
						result = item["MacAddress"].ToString();
						break;
					}
				}
				instances = null;
				managementClass = null;
				return result;
			}
			catch
			{
				return "unknow";
			}
			finally
			{
			}
		}

		private string GetDiskID()
		{
			try
			{
				string result = "";
				ManagementClass managementClass = new ManagementClass("Win32_DiskDrive");
				ManagementObjectCollection instances = managementClass.GetInstances();
				foreach (ManagementObject item in instances)
				{
					result = item.Properties["Model"].ToString();
				}
				instances = null;
				managementClass = null;
				return result;
			}
			catch
			{
				return "unknow";
			}
			finally
			{
			}
		}

		private string GetUserName()
		{
			try
			{
				string result = "";
				ManagementClass managementClass = new ManagementClass("Win32_ComputerSystem");
				ManagementObjectCollection instances = managementClass.GetInstances();
				foreach (ManagementObject item in instances)
				{
					result = item["UserName"].ToString();
				}
				instances = null;
				managementClass = null;
				return result;
			}
			catch
			{
				return "unknow";
			}
			finally
			{
			}
		}

		private string GetSystemType()
		{
			try
			{
				string result = "";
				ManagementClass managementClass = new ManagementClass("Win32_ComputerSystem");
				ManagementObjectCollection instances = managementClass.GetInstances();
				foreach (ManagementObject item in instances)
				{
					result = item["SystemType"].ToString();
				}
				instances = null;
				managementClass = null;
				return result;
			}
			catch
			{
				return "unknow";
			}
			finally
			{
			}
		}

		private string GetTotalPhysicalMemory()
		{
			try
			{
				string result = "";
				ManagementClass managementClass = new ManagementClass("Win32_ComputerSystem");
				ManagementObjectCollection instances = managementClass.GetInstances();
				foreach (ManagementObject item in instances)
				{
					result = item["TotalPhysicalMemory"].ToString();
				}
				instances = null;
				managementClass = null;
				return result;
			}
			catch
			{
				return "unknow";
			}
			finally
			{
			}
		}

		private string GetComputerName()
		{
			try
			{
				return Environment.GetEnvironmentVariable("ComputerName");
			}
			catch
			{
				return "unknow";
			}
			finally
			{
			}
		}
	}
}
