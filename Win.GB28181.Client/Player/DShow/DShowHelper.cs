using DirectShowLib;
using Helpers;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;

namespace SS.WPFClient.DShow
{
    public class DShowHelper
    {
        public static string PrintMediaType(AMMediaType amm)
        {
            var vih = DShowHelper.QueryVideoInfoHeader(amm);
            var compressionName = DShowHelper.ForCompression(vih.BmiHeader.Compression);

            var str = string.Format("{0} width={1};height={2}", DsToString.MediaSubTypeToString(amm.subType).PadRight(8), vih.BmiHeader.Width, vih.BmiHeader.Height);
            Console.WriteLine("DShow:{0}", str);
            return str;
        }
        public static int GetCompression(string str)
        {
            var cs = str.ToCharArray();
            var bs = cs.Select(p => (int)p).ToArray();
            int result = 0;
            for (int i = 0; i < cs.Length; i++)
            {
                result |= (bs[i] << i * 8);
            }
            return result;
        }
        public static string ForCompression(int comperssion)
        {
            return Encoding.ASCII.GetString(BitConverter.GetBytes(comperssion));
        }
        public static VideoInfoHeader QueryVideoInfoHeader(AMMediaType amm)
        {
            if (amm == null)
            {
            }
            if (amm.formatSize == 0)
                return null;
            var ptr = amm.formatPtr;
            var buff = new byte[amm.formatSize];
            Marshal.Copy(ptr, buff, 0, amm.formatSize);
            return FunctionEx.BytesToStruct<VideoInfoHeader>(buff);
        }

        public static AMMediaType FindAMMediaType(PinEx pinex, VSizeMode mode) {
            var size = GetVSize(mode);
            return FindAMMediaType(pinex, size.Width, size.Height);
        }
        public static AMMediaType FindAMMediaType(PinEx pinex, int width,int height) {
            //e436eb7d-524f-11ce-9f53-0020af0ba770  RGB24
            //32595559-0000-0010-8000-00aa00389b71  YUY2

            return FindAMMediaType(pinex, width, height, new Guid[] { Guid.Parse("e436eb7d-524f-11ce-9f53-0020af0ba770"), Guid.Parse("32595559-0000-0010-8000-00aa00389b71") });
        }
        public static AMMediaType FindAMMediaType(PinEx pinex, int width, int height,Guid[] subTypes) {
     
            foreach (var mt in pinex.MediaTypes) {
                var vi = QueryVideoInfoHeader(mt);
              
                if (vi.BmiHeader.Width == width && vi.BmiHeader.Height == height && (subTypes == null || (subTypes != null && subTypes.Contains(mt.subType)))) {
                    
                    PrintMediaType(mt);
                    return mt;
                }
            }
            return null;
        }

        public static IBaseFilter GetSystemFilter(Guid filterCategory, string filterName)
        {
            var device = DsDevice.GetDevicesOfCat(filterCategory)
                  .FirstOrDefault(p => p.Name.EqIgnoreCase(filterName));
            if (device != null)
            {
                Guid guidBaseFilter = typeof(IBaseFilter).GUID;
                object resultObj = null;
                device.Mon.BindToObject(null, null, ref guidBaseFilter, out resultObj);
                device.Dispose();
                return resultObj as IBaseFilter;
            }
            return null;
        }
        public static IBaseFilter GetSystemFilter(Guid filterCategory, Guid classId)
        {
            var device = DsDevice.GetDevicesOfCat(filterCategory)
                  .FirstOrDefault(p => p.ClassID == classId);
            if (device != null)
            {
                Guid guidBaseFilter = typeof(IBaseFilter).GUID;
                object resultObj = null;
                device.Mon.BindToObject(null, null, ref guidBaseFilter, out resultObj);
                device.Dispose();
                return resultObj as IBaseFilter;
            }
            return null;
        }
        public static IBaseFilter GetDirectShowFilter(string filterName)
        {
            var filterCategory = new Guid("083863F1-70DE-11D0-BD40-00A0C911CE86");
            return GetSystemFilter(filterCategory, filterName);
        }
        public static IBaseFilter GetDirectShowFilter(Guid classId)
        {
            var filterCategory = new Guid("083863F1-70DE-11D0-BD40-00A0C911CE86");
            return GetSystemFilter(filterCategory, classId);
        }
        public static List<BaseFilterEx> GetFilter_CaptureList()
        {
            var list=new List<BaseFilterEx>();
            var devices = DsDevice.GetDevicesOfCat(FilterCategory.VideoInputDevice);
            for (int i = 0; i < devices.Count(); i++)
            {
                list.Add(GetFilter_Capture(i));
            }
            return list;
        }
        public static BaseFilterEx GetFilter_Capture(int index)
        {
            var devices = DsDevice.GetDevicesOfCat(FilterCategory.VideoInputDevice);
            if (devices.Count() > index)
            {


                DsDevice device = devices[index];
                Guid guidBaseFilter = typeof(IBaseFilter).GUID;
                object resultObj = null;
                device.Mon.BindToObject(null, null, ref guidBaseFilter, out resultObj);
                device.Dispose();
                var baseFilter = resultObj as IBaseFilter;
                if (baseFilter != null)
                    return new BaseFilterEx((IBaseFilter)resultObj, "Capture:" + (index + 1).ToString());
                else
                    return null;
            }

            return null;
        }
        public static BaseFilterEx GetFilter_Capture(string name)
        {
            var filter = DShowHelper.GetSystemFilter(FilterCategory.VideoInputDevice, name);
            if (filter != null)
                return new BaseFilterEx(filter);
            else
                return null;
        }

        public static BaseFilterEx GetFilter_JSSourceFilterEx()
        {
            string name = "JSSourceFilter";
            var filter = DShowHelper.GetDirectShowFilter(name);
            if (filter != null)
                return new BaseFilterEx(filter, name);
            else
                return null;
        }
        public static BaseFilterEx GetFilter_FfdshowEncode()
        {
            string name = "ffdshow video encoder";
            var filter = DShowHelper.GetSystemFilter(new Guid("33D9A760-90C8-11D0-BD43-00A0C911CE86"), name);
            if (filter != null)
                return new BaseFilterEx(filter, name);
            else
                return null;
        }
        public static BaseFilterEx GetFilter_FfdshowDecode()
        {
            string name = "ffdshow video decoder";
            var filter = DShowHelper.GetDirectShowFilter("ffdshow video decoder");
            if (filter != null)
                return new BaseFilterEx(filter, name);
            else
                return null;
        }
        public static BaseFilterEx GetFilter_SampleGrabber()
        {

            return new BaseFilterEx(((IBaseFilter)new SampleGrabber()), "SampleGrabber");
        }
        public static BaseFilterEx GetFilter_SmartTee()
        {
            var filter = DShowHelper.GetDirectShowFilter("smart tee");
            if (filter != null)
                return new BaseFilterEx(filter);
            else
                return null;

        }
        public static BaseFilterEx GetFilter_ColorSpaceConverter()
        {
            var filter = DShowHelper.GetDirectShowFilter("Color Space Converter");
            if (filter != null)
                return new BaseFilterEx(filter);
            else
                return null;

        }
        public static BaseFilterEx GetFilter_MoonlightScalar()
        {
            var filter = DShowHelper.GetDirectShowFilter("moonlight");
            if (filter != null)
                return new BaseFilterEx(filter);
            else
                return null;
        }
        public static BaseFilterEx GetFilter_VideoRenderer()
        {

            return new BaseFilterEx(((IBaseFilter)new VideoRendererDefault()), "VideoRenderer");

            //var filter = DSHelper.GetDirectShowFilter("video renderer");
            //if (filter != null)
            //    return new BaseFilterEx(filter);
            //else
            //    return null;
        }
        public static BaseFilterEx GetFilter_NullRenderer()
        {
            return new BaseFilterEx(((IBaseFilter)new NullRenderer()), "NullRenderer");
        }
        public static BaseFilterEx GetFilter_AVIDecompressor()
        {
            return new BaseFilterEx(((IBaseFilter)new AVIDec()), "AVIDecompressor");

        }



        public static Size GetVSize(VSizeMode mode)
        {
            switch (mode)
            {
                case VSizeMode.QCIF: return new Size(160, 120);
                case VSizeMode.QVGA: return new Size(320, 240);
                case VSizeMode.CIF: return new Size(352, 288);
                case VSizeMode.VGA: return new Size(640, 480);
                case VSizeMode.D1: return new Size(704, 576);
                default: return new Size(352, 288);
            }
        }

    }

}
