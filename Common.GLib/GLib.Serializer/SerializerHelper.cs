using System;
using System.IO;
using System.Text;

namespace GLib.Serializer
{
    public class SerializerHelper
    {
        public static string StringSerializer(object value)
        {
            string text = "";
            try
            {
                //		NetDataContractSerializer netDataContractSerializer = new NetDataContractSerializer();
                MemoryStream memoryStream = new MemoryStream();
                //	netDataContractSerializer.WriteObject(memoryStream, value);
                byte[] array = memoryStream.ToArray();
                return Encoding.UTF8.GetString(array, 0, array.Length);
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        public static object StringDeserialize(string value)
        {
            object obj = null;
            try
            {
                //	NetDataContractSerializer netDataContractSerializer = new NetDataContractSerializer();
                byte[] bytes = Encoding.UTF8.GetBytes(value);
                MemoryStream memoryStream = new MemoryStream(bytes);
                memoryStream.Seek(0L, SeekOrigin.Begin);
                //	return netDataContractSerializer.ReadObject(memoryStream);
                return null;
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        public static Stream StreamSerializer(object value)
        {
            Stream stream = null;
            try
            {
                //NetDataContractSerializer netDataContractSerializer = new NetDataContractSerializer();
                MemoryStream memoryStream = new MemoryStream();
              //  netDataContractSerializer.WriteObject(memoryStream, value);
                stream = memoryStream;
                stream.Seek(0L, SeekOrigin.Begin);
            }
            catch (Exception ex)
            {
                throw ex;
            }
            return stream;
        }

        public static object StreamDeserialize(Stream value)
        {
            object obj = null;
            if (!value.CanSeek)
            {
                value = GetNewStream(value);
            }
            try
            {
                //NetDataContractSerializer netDataContractSerializer = new NetDataContractSerializer();
                value.Seek(0L, SeekOrigin.Begin);
                //  return netDataContractSerializer.ReadObject(value);
                return obj;
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        public static T StreamDeserialize<T>(Stream stream)
        {
            object obj = StreamDeserialize(stream);
            if (obj is T)
            {
                return (T)obj;
            }
            return default(T);
        }

        private static Stream GetNewStream(Stream value)
        {
            Stream stream = new MemoryStream();
            byte[] buffer = new byte[4096];
            int num = 0;
            while ((num = value.Read(buffer, 0, 4096)) > 0)
            {
                stream.Write(buffer, 0, num);
            }
            stream.Seek(0L, SeekOrigin.Begin);
            return stream;
        }
    }
}
