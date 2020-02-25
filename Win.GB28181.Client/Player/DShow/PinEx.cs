using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using DirectShowLib;
using System.Runtime.InteropServices;
using System.Diagnostics;
using GLib.GeneralModel;
namespace SS.WPFClient.DShow
{
    public class PinConnectEventArg : EventArgs
    {
        public AMMediaType Set;
        public AMMediaType Reset;
        public PinConnectEventArg(AMMediaType @set)
        {
            Set = @set;
        }
        
    }
    /// <summary>
    /// DShow引脚扩展
    /// </summary>
    public sealed class PinEx : IDisposable
    {
        private bool _alearErr = false;
        public PinEx(IPin pin)
        {
            this.Pin = pin;
            Init();
        }
        /// <summary>
        /// 引脚接口
        /// </summary>
        public IPin Pin { get; private set; }
        /// <summary>
        /// 引起Id
        /// </summary>
        public string Id { get; private set; }
        /// <summary>
        /// 引脚名称
        /// </summary>
        public string Name { get; private set; }
        
        public PinDirection PinDirection { get; private set; }
        public IBaseFilter Filter { get; private set; }
        public List<AMMediaType> MediaTypes { get; private set; }

        public event EventHandler<PinConnectEventArg> PinConnecting;
        public event EventHandler<EventArgsEx<AMMediaType>> PinConnectFailure;
        public event EventHandler<EventArgsEx<AMMediaType>> PinConnectSuccess;
         
        public bool ConnectEx(PinEx pReceivePinEx)
        {
            Disconnect();
            if (this.MediaTypes.Count == 0)
                InitAMMediaType();
            Console.WriteLine(string.Format("InitAMMediaType():{0}", this.MediaTypes.Count));
            foreach (var mt in this.MediaTypes)
            {

                var hr = pReceivePinEx.Pin.QueryAccept(mt);
                Console.WriteLine(string.Format("pReceivePinEx.Pin.QueryAccept(mt):{0}  {1}", hr, DsError.GetErrorText(hr)));
                if (hr != 0)
                    continue;
 
                if (hr == 0 && ConnectEx(pReceivePinEx, mt) == 0)
                    return true;
            }
            return false;
        }
        public int ConnectEx(PinEx pReceivePinEx, AMMediaType pmt)
        {
            var eventArg = new PinConnectEventArg(pmt);
            if (PinConnecting != null)
                PinConnecting(this, eventArg);

            var result = Pin.Connect(pReceivePinEx.Pin, eventArg.Reset ?? pmt);
            if (result == 0)
            {
                pReceivePinEx.InitAMMediaType();
                if (PinConnectSuccess != null) PinConnectSuccess(this, new EventArgsEx<AMMediaType>(pmt));
            }
            else
            {
                Console.WriteLine(string.Format("DSError:{0}  {1}", result, DsError.GetErrorText(result)));
                if (PinConnectFailure != null) PinConnectFailure(this, new EventArgsEx<AMMediaType>(pmt));
                
            }
            return result;
        }
        public bool Disconnect()
        {
            var hr = Pin.Disconnect();
            ThrowExceptionForHR(hr);
            InitAMMediaType();
            return hr == 0;
        }

        private void Init()
        {
            InitId();
            InitDirection();
            InitAMMediaType();
        }
        private void InitId()
        {
            var hr = 0;
            string id = null;
            hr = Pin.QueryId(out id);
            this.Id = id;
            ThrowExceptionForHR(hr);

        }
        private void InitDirection()
        {
            var hr = 0;

            PinInfo pinInfo;
            hr = Pin.QueryPinInfo(out pinInfo);
            DsError.ThrowExceptionForHR(hr);
            this.Name = pinInfo.name;
            this.PinDirection = pinInfo.dir;
            this.Filter = pinInfo.filter;
        }
        /// <summary>
        /// 初始化该引脚的媒体类型
        /// </summary>
        public void InitAMMediaType()
        {
            
            this.MediaTypes = new List<AMMediaType>();

            var hr = 0;
            IEnumMediaTypes enumMediaTypes = null;
            hr = Pin.EnumMediaTypes(out enumMediaTypes);
            if (hr != 0)
            {
                if (hr != DsResults.E_NotConnected)
                    DsError.ThrowExceptionForHR(hr);
                return;
            }
            short count = 128;
            IntPtr pCount = Marshal.AllocHGlobal(sizeof(short));
            Marshal.WriteInt16(pCount, count);
            AMMediaType[] mediaTypeArr = new AMMediaType[1];

            while (enumMediaTypes.Next(1, mediaTypeArr, pCount) == 0)
            {
                if(mediaTypeArr[0]!=null)
                    MediaTypes.Add(mediaTypeArr[0]);
            }
            Marshal.ReleaseComObject(enumMediaTypes);

        }
 
        public void Dispose()
        {
            Marshal.ReleaseComObject(this.Pin);
        }
        private void ThrowExceptionForHR(int hr)
        {
            if (_alearErr)
                try
                {
                    DsError.ThrowExceptionForHR(hr);
                }
                catch (Exception ex)
                {
                    throw;
                }
        }


    }
}
