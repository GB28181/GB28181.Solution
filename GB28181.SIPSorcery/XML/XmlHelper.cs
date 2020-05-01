using GB28181.Logger4Net;
using System;
using System.IO;
using System.Text;
using System.Xml;
using System.Xml.Serialization;

namespace GB28181.Sys.XML
{
    /// <summary>
    /// XML操作访问类
    /// </summary>
    /// <typeparam name="T">泛型</typeparam>
    public abstract class XmlHelper<T> where T : class
    {
        private static ILog logger = AppState.logger;

        /// <summary>
        /// 文档路径
        /// </summary>
        private string m_xml_path;

        private static string m_dir = AppDomain.CurrentDomain.BaseDirectory + "\\Config\\";
        /// <summary>
        /// 存储对象
        /// </summary>
        private T t;

        public XmlHelper() { }
        /// <summary>
        /// 序列化
        /// </summary>
        private void Serialize(T t)
        {
            //XmlSerializer xs = new XmlSerializer(typeof(T));
            //MemoryStream stream = new MemoryStream();
            //XmlWriterSettings settings = new XmlWriterSettings();
            //settings.Indent = true;
            //settings.Encoding = Encoding.GetEncoding("GB2312");
            //settings.Encoding = new UTF8Encoding(false);
            //settings.NewLineOnAttributes = true;
            //settings.OmitXmlDeclaration = false;
            //using (XmlWriter writer = XmlWriter.Create("c:\\catalog.xml", settings))
            //{
            //    var xns = new XmlSerializerNamespaces();

            //    xns.Add(string.Empty, string.Empty);
            //    去除默认命名空间
            //    xs.Serialize(writer, t, xns);
            //}



            XmlWriterSettings settings = new XmlWriterSettings
            {
                OmitXmlDeclaration = false,
                Encoding = Encoding.GetEncoding("utf-8"),

                Indent = true
            };
            XmlSerializer s = new XmlSerializer(t.GetType());
            var xns = new XmlSerializerNamespaces();
            xns.Add("", "");
            XmlWriter w = XmlWriter.Create(m_xml_path, settings);
            s.Serialize(w, t, xns);
            w.Flush();
            w.Close();
            //TextReader r = new StreamReader("c:\\catalog.xml");
            //string xmlBody = r.ReadToEnd();


            //XmlSerializer s = new XmlSerializer(t.GetType());
            //TextWriter w = new StreamWriter("c:\\catalog.xml");
            //s.Serialize(w, t);
            //w.Flush();
            //w.Close();
        }

        public virtual string Serialize<T1>(T1 obj)
        {
            var stream = new MemoryStream();
            var xml = new XmlSerializer(typeof(T1));
            try
            {
                var xns = new XmlSerializerNamespaces();
                xns.Add("", "");
                //序列化对象
                xml.Serialize(stream, obj, xns);
            }
            catch (Exception ex)
            {
                logger.Error("序列化对象为xml字符串出错" + ex);
            }
            //XmlSerializer xs = new XmlSerializer(typeof(T));
            //MemoryStream stream = new MemoryStream();
            //XmlWriterSettings settings = new XmlWriterSettings();
            //settings.Indent = true;
            //settings.Encoding =  Encoding.GetEncoding("GB2312");
            ////settings.Encoding = new UTF8Encoding(false);
            //settings.NewLineOnAttributes = true;
            //settings.OmitXmlDeclaration = false;
            //using (XmlWriter writer = XmlWriter.Create(stream, settings))
            //{
            //    var xns = new XmlSerializerNamespaces();

            //    xns.Add(string.Empty, string.Empty);
            //    //去除默认命名空间
            //    xs.Serialize(writer, obj, xns);
            //}
            return Encoding.UTF8.GetString(stream.ToArray());//.Replace("\r", "");
        }

        ///// <summary>  
        ///// 对象序列化成 XML String  
        ///// </summary>  
        //public string Serialize<T>(T obj)
        //{
        //    string xmlString = string.Empty;
        //    XmlSerializer xmlSerializer = new XmlSerializer(typeof(T));

        //    using (MemoryStream ms = new MemoryStream())
        //    {
        //        xmlSerializer.Serialize(ms, obj);
        //        xmlString = Encoding.UTF8.GetString(ms.ToArray());
        //    }
        //    return xmlString;
        //}  

        //public string Serialize<T>(T entity)
        //{
        //    StringBuilder buffer = new StringBuilder();

        //    XmlSerializer serializer = new XmlSerializer(typeof(T));
        //    using (TextWriter writer = new StringWriter(buffer))
        //    {
        //        serializer.Serialize(writer, entity);
        //    }

        //    return buffer.ToString();

        //}  

        ///// <summary>
        ///// 序列化
        ///// </summary>
        ///// <typeparam name="T">类型</typeparam>
        ///// <param name="entity">实体类型</param>
        ///// <returns>XML格式字符串</returns>
        //public string Serialize<T>(T entity)
        //{
        //    //StringBuilder 
        //    MemoryStream stream = new MemoryStream();
        //    XmlSerializer serializer = new XmlSerializer(typeof(T));
        //    XmlWriterSettings settings = new XmlWriterSettings();
        //    settings.Indent = true;
        //    settings.Encoding = new UTF8Encoding(false);
        //    settings.NewLineOnAttributes = true;
        //    settings.OmitXmlDeclaration = false;
        //    try
        //    {
        //        using (XmlWriter write = XmlWriter.Create(stream, settings))
        //        {
        //            XmlSerializerNamespaces ns = new XmlSerializerNamespaces();
        //            //去除默认命名空间
        //            ns.Add("", "");
        //            serializer.Serialize(stream, entity, ns);
        //        }
        //    }
        //    catch (Exception ex)
        //    {
        //        logger.Error("对象序列化到XML格式字符串错误" + ex.Message+ex.StackTrace.ToString());
        //    }
        //    stream.Close();
        //    return Encoding.UTF8.GetString(stream.ToArray()).Replace("\r", "");
        //} 

        /// <summary>
        /// 反序列
        /// </summary>
        /// <returns></returns>
        private T Deserialize()
        {
            //MemoryStream stream = new MemoryStream(Encoding.GetEncoding("utf-8").GetBytes(m_xml_path));
            //StreamReader sr = new StreamReader(stream, Encoding.GetEncoding("utf-8"));

            if (File.Exists(m_xml_path))
            {
                TextReader r = new StreamReader(m_xml_path);
                XmlSerializer s = new XmlSerializer(typeof(T));
                object obj;
                try
                {
                    obj = (T)s.Deserialize(r);
                }
                catch (Exception)
                {
                    r.Close();
                    return null;
                }
                if (obj is T)
                    t = obj as T;
                r.Close();
            }
            return t;
        }

        public string ConvertUtf8ToDefault(string message)
        {
            System.Text.Encoding utf8;
            utf8 = System.Text.Encoding.GetEncoding("utf-8");
            byte[] array = Encoding.Unicode.GetBytes(message);
            byte[] s4 = System.Text.Encoding.Convert(System.Text.Encoding.UTF8, System.Text.Encoding.GetEncoding("gb2312"), array);
            string str = Encoding.Default.GetString(s4);
            return str;
        }


        /// <summary>
        /// 反序列
        /// </summary>
        /// <returns></returns>
        private T Deserialize(string xmlBody)
        {
            MemoryStream stream = new MemoryStream(Encoding.GetEncoding("utf-8").GetBytes(xmlBody));
            StreamReader sr = new StreamReader(stream, Encoding.GetEncoding("utf-8"));

            //TextReader sr = new StringReader(xmlBody);
            XmlSerializer s = new XmlSerializer(typeof(T));
            object obj;
            try
            {
                obj = (T)s.Deserialize(sr);
            }
            catch (Exception)
            {
                //logger.Error("反序列化错误" + ex.Message + ex.StackTrace.ToString());
                sr.Close();
                return null;
            }
            if (obj is T)
                t = obj as T;
            sr.Close();
            return t;
        }

        /// <summary>
        /// 读取文件并返回并构建成类
        /// </summary>
        /// <param name="type">类型</param>
        /// <returns>需要返回的类型格式</returns>
        public virtual T Read(Type type)
        {
            CheckConstructPath(type);
            return this.Deserialize();
        }
        /// <summary>
        /// 读取文件并返回并构建成类
        /// </summary>
        /// <param name="xmlBody">XML文档</param>
        /// <returns>需要返回的类型格式</returns>
        public virtual T Read(string xmlBody)
        {
            return this.Deserialize(xmlBody);
        }

        /// <summary>
        /// //检查并构造路径
        /// </summary>
        /// <param name="type"></param>
        private void CheckConstructPath(System.Type type)
        {
            //构造路径
            string temppath = m_dir + type.Name + ".xml";

            //如果路径相等则返回
            if (this.m_xml_path == temppath)
                return;

            //是否存在Config目录，不存在则返回
            if (!Directory.Exists(m_dir))
                Directory.CreateDirectory(m_dir);

            this.m_xml_path = temppath;

        }

        /// <summary>
        /// 保存文件
        /// </summary>
        /// <param name="t">类型</param>
        public virtual void Save(T t)
        {
            CheckConstructPath(t.GetType());
            Serialize(t);
        }

        /// <summary>
        /// 保存文件
        /// </summary>
        /// <typeparam name="T1">类型1</typeparam>
        /// <param name="t">类型实例</param>
        /// <returns></returns>
        public virtual string Save<T1>(T1 t)
        {
            CheckConstructPath(t.GetType());
            return Serialize(t);
        }
    }
}
