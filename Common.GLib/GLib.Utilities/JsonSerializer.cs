using System;
using System.IO;
using System.Runtime.Serialization.Json;
using System.Text;

namespace GLib.Utilities
{
	public class JsonSerializer
	{
		public static T ParseFromJson<T>(string szJson)
		{
			T val = Activator.CreateInstance<T>();
			using (MemoryStream stream = new MemoryStream(Encoding.UTF8.GetBytes(szJson)))
			{
				DataContractJsonSerializer dataContractJsonSerializer = new DataContractJsonSerializer(val.GetType());
				return (T)dataContractJsonSerializer.ReadObject(stream);
			}
		}
	}
}
