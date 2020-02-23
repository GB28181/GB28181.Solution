using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Security;
using System.Runtime.InteropServices;
using DirectShowLib;
using GLib.Extension;
 
using System.Diagnostics;
namespace Win.WPFClient.DShow
{
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public delegate long JSSourceFilter_FillBufferCallBack(ref IntPtr pData);
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public delegate void JSSourceFilter_ErrorCallBack(int pData);
    /// <summary>
    /// 该接口为标准DShow接口，用于跟DShow进行数据交互,
    /// 在接收到数据后通过回调将数据传给过滤器
    /// </summary>
    [Guid("1CB42CC8-D32C-4f73-9267-C114DA470322")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    [SuppressUnmanagedCodeSecurity]
    public interface IDSMutualFilter
    {
        
        /// <summary>
        /// 设置数据格式
        /// </summary>
        /// <param name="mt"></param>
        /// <returns></returns>
        [DispId(1)]
        [PreserveSig]
        int SetMediaType([In]  AMMediaType mt);

        /// <summary>
        /// 设置读取侦数据回调
        /// </summary>
        /// <param name="cb"></param>
        /// <returns></returns>
        [DispId(2)]
        [PreserveSig]
        int SetFillBuffCallBack([MarshalAs(UnmanagedType.FunctionPtr)] JSSourceFilter_FillBufferCallBack cb);
        /// <summary>
        /// 设置异常回调
        /// </summary>
        /// <param name="cb"></param>
        /// <returns></returns>
        [DispId(3)]
        [PreserveSig]
        int SetErrorCallBack([MarshalAs(UnmanagedType.FunctionPtr)] JSSourceFilter_ErrorCallBack cb);


    }


 
    /// <summary>
    /// 视频尺寸模式
    /// </summary>
    public enum VSizeMode
    {
        QCIF = 1,//160X120
        QVGA = 2,//320X240
        CIF = 3,//352X288
        VGA = 4,//640X480
        D1 = 5//704X576
    }
}
