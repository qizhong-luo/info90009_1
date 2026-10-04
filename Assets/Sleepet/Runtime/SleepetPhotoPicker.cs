using System;
using System.Runtime.InteropServices;
using System.Text;
namespace Sleepet
{
    public static class SleepetPhotoPicker
    {
        public static string Pick()
        {
#if UNITY_EDITOR
            return UnityEditor.EditorUtility.OpenFilePanel("Choose a pet photo (JPG / PNG)", "", "png,jpg,jpeg");
#elif UNITY_STANDALONE_WIN
            var dialog = new OpenFile { size = Marshal.SizeOf(typeof(OpenFile)), filter = "Images\0*.png;*.jpg;*.jpeg\0\0", file = new StringBuilder(4096), maxFile = 4096, title = "Choose a pet photo", flags = 0x00001000 | 0x00000800 | 0x00000008 };
            return GetOpenFileName(dialog) ? dialog.file.ToString() : "";
#else
            return "";
#endif
        }
#if UNITY_STANDALONE_WIN && !UNITY_EDITOR
        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        sealed class OpenFile
        {
            public int size; public IntPtr owner, instance; public string filter, customFilter; public int maxCustomFilter, filterIndex;
            public StringBuilder file; public int maxFile; public string fileTitle; public int maxFileTitle; public string initialDirectory, title;
            public int flags; public short fileOffset, extension; public string defaultExtension; public IntPtr customData, hook; public string template;
            public IntPtr reserved; public int reserved2, flagsEx;
        }
        [DllImport("comdlg32.dll", CharSet=CharSet.Unicode, SetLastError=true)] static extern bool GetOpenFileName([In,Out] OpenFile value);
#endif
    }
}
