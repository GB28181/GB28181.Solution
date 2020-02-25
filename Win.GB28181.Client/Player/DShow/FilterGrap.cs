using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using DirectShowLib;
 
using System.Runtime.InteropServices;
using GLib.Extension;
using System.Diagnostics;
namespace SS.WPFClient.DShow
{
    /// <summary>
    /// 过滤器--图表基类
    /// </summary>
    public class FilterGraphBase
    {
        public FilterGraph FilterGraph = null;
        public IGraphBuilder GraphBuilder { get; private set; }
        public IMediaControl MediaControl { get; private set; }
        public IVideoWindow VideoWindow { get; private set; }
        public IMediaEventEx MediaEventEx { get; private set; }
         
        protected List<BaseFilterEx> FilterExList = new List<BaseFilterEx>();
        public FilterGraphBase()
        {
            InitGraph();
        }
        private void InitGraph()
        {
         
            FilterGraph = new FilterGraph();

            this.GraphBuilder = (IGraphBuilder)FilterGraph;
            this.MediaControl = (IMediaControl)this.GraphBuilder;
            this.VideoWindow = (IVideoWindow)this.GraphBuilder;
            this.MediaEventEx = (IMediaEventEx)this.GraphBuilder;
        }
        public virtual void AddFilterEx(BaseFilterEx filterEx)
        {
            FilterExList.Add(filterEx);
        }
        public virtual bool ConnencAll(AMMediaType mediaType)
        {
            foreach (var filterEx in this.FilterExList)
            {
                this.GraphBuilder.RemoveFilter(filterEx.BaseFilter);
                if (filterEx.DefInPin != null)
                {
                    filterEx.DefInPin.Pin.Disconnect();
                    this.GraphBuilder.Disconnect(filterEx.DefInPin.Pin);
                }
                if (filterEx.DefOutPin != null)
                {
                    filterEx.DefOutPin.Pin.Disconnect();
                    this.GraphBuilder.Disconnect(filterEx.DefOutPin.Pin);
                }
            }

            foreach (var filterEx in this.FilterExList)
                this.GraphBuilder.AddFilter(filterEx.BaseFilter, filterEx.Name);
            var firstFilterEx = this.FilterExList[0];
            var lastConnFilterEx = this.FilterExList[1];
            var bFlag = true;
            //var vi = DSHelper.QueryVideoInfoHeader(mediaType);
            if (mediaType != null)
            {
                //var hr = ((IAMStreamConfig)firstFilterEx.DefOutPin.Pin).SetFormat(firstFilterEx.DefOutPin.MediaTypes[0]);
                //DsError.ThrowExceptionForHR(hr);
                //hr = ((IAMStreamConfig)firstFilterEx.DefOutPin.Pin).SetFormat(firstFilterEx.DefOutPin.MediaTypes[2]);
                //DsError.ThrowExceptionForHR(hr);
                var hr = ((IAMStreamConfig)firstFilterEx.DefOutPin.Pin).SetFormat(mediaType);
                DsError.ThrowExceptionForHR(hr);
            }
            if (mediaType == null)
            {
                bFlag &= firstFilterEx.ConnectFilter(lastConnFilterEx);
                GLib.DebugEx.Trace("DShow", string.Format("图表{0}连接{1} {2}", firstFilterEx.Name, lastConnFilterEx.Name, bFlag ? "成功" : "失败"));
            }
            else
            {
                bFlag &= firstFilterEx.ConnectFilter(lastConnFilterEx, mediaType);
                GLib.DebugEx.Trace("DShow", string.Format("图表{0}连接{1} {2}", firstFilterEx.Name, lastConnFilterEx.Name, bFlag ? "成功" : "失败"));
            }
            foreach (var filterEx in this.FilterExList.Skip(2))
            {
                bFlag &= lastConnFilterEx.ConnectFilter(filterEx);
                GLib.DebugEx.Trace("DShow", string.Format("图表{0}连接{1} {2}", lastConnFilterEx.Name, filterEx.Name, bFlag ? "成功" : "失败"));
                lastConnFilterEx = filterEx;
            }
            if (!bFlag)
                foreach (var filterEx in this.FilterExList)
                    this.GraphBuilder.RemoveFilter(filterEx.BaseFilter);
            return bFlag;
        }
        public virtual bool ConnencAll()
        {
            return ConnencAll(null);
        }
        public int Run()
        {
            return this.MediaControl.Run();
        }
        public int Stop()
        {
            return this.MediaControl.Stop();
        }

        public BaseFilterEx this[string name]
        {
            get
            {
                return FilterExList.FirstOrDefault(p => p.Name.EqIgnoreCase(name));
            }
        }

        public int Remove(BaseFilterEx filterEx)
        {
            FilterExList.Remove(filterEx);
            return _Remove(filterEx.BaseFilter);
        }
        public int Remove(IBaseFilter filter)
        {
            var fex = FilterExList.FirstOrDefault(p => p.BaseFilter == filter);
            if (fex == null)
                throw new ApplicationException("未存在对应的filter");
            else
                return Remove(fex);
        }
        private int _Remove(IBaseFilter filter)
        {
            var hr = GraphBuilder.RemoveFilter(filter);
            return hr;
        }

        
    }
 
    
    /// <summary>
    /// 通用过滤器图片
    /// </summary>
    public class GenFilterGraphEx : FilterGraphBase
    {
        //添摄像头
        public BaseFilterEx AddCapture(int index)
        {
            var filterEx = DShowHelper.GetFilter_Capture(index);
            FilterExList.Add(filterEx);
            return filterEx;

        }
        //添加H264播放主动获取流Filter
        public BaseFilterEx AddJSSourceFilter()
        {
            var filterEx = DShowHelper.GetFilter_JSSourceFilterEx();
            FilterExList.Add(filterEx);
            return filterEx;
        }
        //添加ffdshow编码器
        public BaseFilterEx AddFfdshowEncode()
        {
            var filterEx = DShowHelper.GetFilter_FfdshowEncode();
            FilterExList.Add(filterEx);
            return filterEx;
        }
        //添加ffdshow解码器
        public BaseFilterEx AddFfdshowDecode()
        {
            var filterEx = DShowHelper.GetFilter_FfdshowDecode();
            FilterExList.Add(filterEx);
            return filterEx;
        }
        //添加流回调
        public BaseFilterEx AddSampleGrabber()
        {
            var filterEx = DShowHelper.GetFilter_SampleGrabber();
            FilterExList.Add(filterEx);
            return filterEx;

        }
        //缩放
        public BaseFilterEx AddMoonlightScalar()
        {
            var filterEx = DShowHelper.GetFilter_MoonlightScalar();
            FilterExList.Add(filterEx);
            return filterEx;
        }
        //流分支
        public BaseFilterEx AddSmartTee()
        {
            var filterEx = DShowHelper.GetFilter_SmartTee();
            FilterExList.Add(filterEx);
            return filterEx;
           
        }
        //添加颜色转换
        public BaseFilterEx AddColorSpaceConverter()
        {
            var filterEx = DShowHelper.GetFilter_ColorSpaceConverter();
            FilterExList.Add(filterEx);
            return filterEx;
        }
        //添加视频呈现
        public BaseFilterEx AddVideoRenderer()
        {
            var filterEx = DShowHelper.GetFilter_VideoRenderer();
            FilterExList.Add(filterEx);
            return filterEx;
        }
        //添加avi输出
        public BaseFilterEx AddAVIDecompressor()
        {
            var filterEx = DShowHelper.GetFilter_AVIDecompressor();
            FilterExList.Add(filterEx);
            return filterEx;
        }

        //添加NullRenderer输出
        public BaseFilterEx AddNullRenderer() {
            var filterEx = DShowHelper.GetFilter_NullRenderer();
            FilterExList.Add(filterEx);
            return filterEx;
        }




        public bool NullRender()
        {
            var bFlag = ConnencAll();
            if (!bFlag)
            {
                var filterEx = this.FilterExList.Last();
                var hr = this.GraphBuilder.Render(filterEx.DefOutPin.Pin);
                bFlag &= hr == 0;
            }
            return bFlag;
        }


        public bool VideoRender()
        {
          
            var bFlag = ConnencAll();
            if (bFlag)
            {
                var filterEx = this.FilterExList.Last();

                var hr = this.GraphBuilder.Render(filterEx.DefOutPin.Pin);
                DsError.ThrowExceptionForHR(hr);

                bFlag &= hr == 0;
            }
            return bFlag;
        }

       
    }

    

}
