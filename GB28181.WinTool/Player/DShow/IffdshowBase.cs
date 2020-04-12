using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.InteropServices;
using System.Security;

namespace SS.WPFClient.DShow
{
    #region
    [Guid("FC5BCCF4-FD62-45ee-B022-3840EAEA77B2"), SuppressUnmanagedCodeSecurity,
  InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    public interface IffdshowBase
    {

        [PreserveSig]
        int getVersion2();
        [PreserveSig]
        int getParam(uint paramID, out int value);
        [PreserveSig]
        System.Int32 getParam2(uint paramID);
        [PreserveSig]
        int putParam(uint paramID, int value);
        [PreserveSig]
        int invParam(uint paramID);
        [PreserveSig]
        int getParamStr(uint paramID, out IntPtr str, int buflen);
        //int getParamStr(uint paramID, out string str, int buflen);
        [PreserveSig]
        [return: MarshalAs(UnmanagedType.LPWStr)]
        string getParamStr2(uint paramID); //returns const pointer to string, NULL if fail
        [PreserveSig]
        int putParamStr(uint paramID, [In, MarshalAs(UnmanagedType.LPWStr)] string str);
        [PreserveSig]
        int getParamName(uint i, out string str, int len);
        int notifyParamsChanged();
        int setOnChangeMsg(IntPtr wnd, uint msg);
        int setOnFrameMsg(IntPtr wnd, uint msg);
        int getGlobalSettings(IntPtr globalSettingsPtr);
        int saveGlobalSettings();
        int loadGlobalSettings();
        int saveDialogSettings();
        int loadDialogSettings();
        //int getConfig(const Tconfig* *configPtr);
        //int getInstance(HINSTANCE *hi);
        //STDMETHOD_(HINSTANCE,getInstance2();
        //int getPostproc(Tlibmplayer* *postprocPtr);
        //int getTranslator(Ttranslate* *trans);
        int getConfig(IntPtr configPtr);
        int getInstance(IntPtr hi);
        IntPtr getInstance2();
        int getPostproc(IntPtr postprocPtr);
        int getTranslator(IntPtr trans);

        int initDialog();
        int showCfgDlg(IntPtr owner);
        int getCpuUsage2();
        int getCpuUsageForPP();
        int cpuSupportsMMX();
        int cpuSupportsMMXEXT();
        int cpuSupportsSSE();
        int cpuSupportsSSE2();
        int cpuSupportsSSE3();
        int cpuSupportsSSSE3();
        int cpuSupports3DNOW();
        int cpuSupports3DNOWEXT();
        int dbgInit();
        int dbgError(string fmt, IntPtr args);
        int dbgWrite(string fmt, IntPtr args);
        int dbgDone();
        int showTrayIcon();
        int hideTrayIcon();
        IntPtr getExeflnm();
        int getLibavcodec(IntPtr libavcodecPtr);
        IntPtr getSourceName();
        int getGraph(IntPtr graphPtr);
        int seek(int seconds);
        int tell(out int seconds);
        int stop();
        int run();
        int getState2();
        int getCurTime2();
        [PreserveSig]
        int getParamStr3(uint paramID, out IntPtr bufPtr);
        //int getParamStr3(uint paramID, [Out, MarshalAs(UnmanagedType.LPWStr)] out string bufPtr);

        //int savePresetMem(void *buf,int len); //if len=0, then buf should point to int variable which will be filled with required buffer length
        //int loadPresetMem(const void *buf,int len);
        int savePresetMem(IntPtr buf, int len); //if len=0, then buf should point to int variable which will be filled with required buffer length
        int loadPresetMem(IntPtr buf, int len);
        int getParamName3(uint i, out string namePtr);
        int getInCodecString(out string str, int buflen);
        int getOutCodecString(out string str, int buflen);
        int getMerit(out double merit);
        int setMerit(double merit);
        int lock_(int lockId);
        int unlock(int lockId);
        int getParamInfo(uint i, IntPtr paramPtr);
        int exportRegSettings(int all, string regflnm, int unicode);
        int checkInputConnect(IntPtr pin);
        int getParamListItem(int paramId, int index, IntPtr ptr);
        int abortPlayback(int hr);
        int notifyParam(int id, int val);
        int notifyParamStr(int id, string val);
        int doneDialog();
        int resetParam(uint paramID);
        int getCurrentCodecId2();
        int frameStep(int diff);
        int getInfoItem(uint index, out int id, out string name);
        int getInfoItemValue(int id, out string value, out int wasChange, out int splitline);
        int inExplorer();
        IntPtr getInfoItemName(int id);
        IntPtr getCfgDlgHwnd();
        void setCfgDlgHwnd(IntPtr hwnd);
        IntPtr getTrayHwnd_();
        void setTrayHwnd_(IntPtr hwnd);
        string getInfoItemShortcut(int id);
        int getInfoShortcutItem(string s, out int toklen);
        double CPUcount();
        int get_trayIconType();
        int cpuSupportsSSE41();
        int cpuSupportsSSE42();
        int cpuSupportsSSE4A();
        int cpuSupportsSSE5();
    };

    #endregion

}
