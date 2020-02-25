using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using DirectShowLib;
using System.Runtime.InteropServices;

namespace SS.WPFClient.DShow
{ 
    /// <summary>
    /// DShow过滤器扩展
    /// </summary>
    public sealed class BaseFilterEx : IDisposable
    {
        public BaseFilterEx(IBaseFilter baseFilter, string name)
        {
            this.BaseFilter = baseFilter;
            Init();
            Name = name;
        }
        public BaseFilterEx(IBaseFilter baseFilter)
        {
            this.BaseFilter = baseFilter;
            Init();
        }
        /// <summary>
        /// 过滤器接口
        /// </summary>
        public IBaseFilter BaseFilter { get; private set; }
        /// <summary>
        /// 引脚集
        /// </summary>
        public List<PinEx> PinExs { get; private set; }
        /// <summary>
        /// 过滤器的guid
        /// </summary>
        public Guid ClassID { get; private set; }
        /// <summary>
        /// 过滤器名称
        /// </summary>
        public string Name { get; private set; }
        private PinEx _DefOutPin = null;
        private PinEx _DefInPin = null;
        /// <summary>
        /// 默认输出引脚
        /// </summary>
        public PinEx DefOutPin
        {
            get
            {
                if (_DefOutPin == null)
                {
                    _DefOutPin = FindPinEx(PinDirection.Output);
                }
                return _DefOutPin;
            }
            set
            {
                if (OutPins.Contains(value))
                    _DefOutPin = value;
                else
                    throw new ApplicationException("设置的引脚未存在于该filter中");
            }
        }
        /// <summary>
        /// 默认输入引脚
        /// </summary>
        public PinEx DefInPin
        {
            get
            {
                if (_DefInPin == null)
                {
                    _DefInPin = FindPinEx(PinDirection.Input);
                }
                return _DefInPin;
            }
            set
            {
                if (InPins.Contains(value))
                    _DefInPin = value;
                else
                    throw new ApplicationException("设置的引脚未存在于该filter中");
            }
        }
        /// <summary>
        /// 获取当前过滤器的输入引脚
        /// </summary>
        public PinEx[] OutPins { get { return PinExs.Where(p => p.PinDirection == PinDirection.Output).ToArray(); } }
        /// <summary>
        /// 获取当前过滤器的输入引脚
        /// </summary>
        public PinEx[] InPins { get { return PinExs.Where(p => p.PinDirection == PinDirection.Input).ToArray(); } }

        //初始化
        private void Init()
        {
            InitGuid();
            InitName();
            InitPins();
        }
        //初始化当前guid
        private void InitGuid()
        {
            var hr = 0;
            Guid classid;
            hr = BaseFilter.GetClassID(out classid);
            DsError.ThrowExceptionForHR(hr);
            this.ClassID = classid;
        }
        //初始化当前过滤器名称
        private void InitName()
        {
            var hr = 0;
            FilterInfo filterInfo;
            hr = BaseFilter.QueryFilterInfo(out filterInfo);
            DsError.ThrowExceptionForHR(hr);
            this.Name = filterInfo.achName;
        }
        //初始化当前引脚
        private void InitPins()
        {
            var hr = 0;
            IEnumPins enumPins = null;
            BaseFilter.EnumPins(out enumPins);
            DsError.ThrowExceptionForHR(hr);

            short count = 128;
            IntPtr pCount = Marshal.AllocHGlobal(sizeof(short));
            Marshal.WriteInt16(pCount, count);

            IPin[] pinArr = new IPin[1];
            List<PinEx> pinExs = new List<PinEx>();
            while (enumPins.Next(1, pinArr, pCount) == 0)
            {
                pinExs.Add(new PinEx(pinArr[0]));
            }
            this.PinExs = pinExs;
            Marshal.ReleaseComObject(enumPins);

        }
        
      
        /// <summary>
        /// 查找引脚
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public PinEx FindPinEx(string name)
        {
            return PinExs.FirstOrDefault(p => p.Name == name);
        }
        /// <summary>
        /// 查找引脚
        /// </summary>
        /// <param name="dir">输入引脚或输出引脚</param>
        /// <returns></returns>
        public PinEx FindPinEx(PinDirection dir)
        {
            return PinExs.FirstOrDefault(p => p.PinDirection == dir);
        }
        /// <summary>
        /// 使用默认输出引脚连接过滤器默认输入引脚
        /// </summary>
        /// <param name="filterEx"></param>
        /// <returns></returns>
        public bool ConnectFilter(BaseFilterEx filterEx)
        {
            var op = this.DefOutPin;
            var ip = filterEx.DefInPin;
            return op.ConnectEx(ip);
        }
        /// <summary>
        /// 根据媒体类型连接过滤器合适的引脚
        /// </summary>
        /// <param name="filterEx"></param>
        /// <param name="pmt"></param>
        /// <returns></returns>
        public bool ConnectFilter(BaseFilterEx filterEx, AMMediaType pmt)
        {
            return this.DefOutPin.ConnectEx(filterEx.DefInPin, pmt) == 0;
        }



        public void Dispose()
        {

            Marshal.ReleaseComObject(BaseFilter);
        }
    }
    
}
