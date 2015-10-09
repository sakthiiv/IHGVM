using System;
using System.Runtime.InteropServices;

namespace IHGVM_VideoManeuver
{
    public class Avi32
    {
        #region Constants

        public static readonly int streamtype_video = mmioFOURCC('v', 'i', 'd', 's');

        #endregion

        public static Int32 mmioFOURCC(char ch0, char ch1, char ch2, char ch3)
        {
            return ((Int32)(byte)(ch0) | ((byte)(ch1) << 8) |
                ((byte)(ch2) << 16) | ((byte)(ch3) << 24));
        }

        public static string decode_mmioFOURCC(int code)
        {
            char[] chs = new char[4];
            for (int i = 0; i < 4; i++)
            {
                chs[i] = (char)(byte)((code >> (i << 3)) & 0xFF);
                if (!char.IsLetterOrDigit(chs[i]))
                    chs[i] = ' ';
            }
            return new string(chs);
        }

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        public struct RECT
        {
            public UInt32 left;
            public UInt32 top;
            public UInt32 right;
            public UInt32 bottom;
        } 

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        public struct BITMAPINFOHEADER
        {
            public Int32 biSize;
            public Int32 biWidth;
            public Int32 biHeight;
            public Int16 biPlanes;
            public Int16 biBitCount;
            public Int32 biCompression;
            public Int32 biSizeImage;
            public Int32 biXPelsPerMeter;
            public Int32 biYPelsPerMeter;
            public Int32 biClrUsed;
            public Int32 biClrImportant;
        }

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        public struct AVISTREAMINFO
        {
            public Int32 fccType;
            public Int32 fccHandler;
            public Int32 dwFlags;
            public Int32 dwCaps;
            public Int16 wPriority;
            public Int16 wLanguage;
            public Int32 dwScale;
            public Int32 dwRate;
            public Int32 dwStart;
            public Int32 dwLength;
            public Int32 dwInitialFrames;
            public Int32 dwSuggestedBufferSize;
            public Int32 dwQuality;
            public Int32 dwSampleSize;
            public RECT rcFrame;
            public Int32 dwEditCount;
            public Int32 dwFormatChangeCount;
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 64)]
            public UInt16[] szName;
        }

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        public struct BITMAPFILEHEADER
        {
            public Int16 bfType; //"magic cookie" - must be "BM"
            public Int32 bfSize;
            public Int16 bfReserved1;
            public Int16 bfReserved2;
            public Int32 bfOffBits;
        }

        #region Avi dll Methods

        [DllImport("avifil32.dll")]
        public static extern void AVIFileInit();

        [DllImport("avifil32.dll", PreserveSig = true)]
        public static extern int AVIFileOpen(ref int ppfile, String szFile, int uMode, int pclsidHandler);

        [DllImport("avifil32.dll")]
        public static extern int AVIFileGetStream(int pfile, out IntPtr ppavi, int fccType, int lParam);

        [DllImport("avifil32.dll")]
        public static extern int AVIStreamReadFormat(IntPtr aviStream, Int32 lPos, ref BITMAPINFOHEADER lpFormat, ref Int32 cbFormat);

        [DllImport("avifil32.dll", PreserveSig = true)]
        public static extern int AVIStreamStart(int pavi);

        [DllImport("avifil32.dll", PreserveSig = true)]
        public static extern int AVIStreamLength(int pavi);

        [DllImport("avifil32.dll")]
        public static extern int AVIStreamInfo(IntPtr pAVIStream, ref AVISTREAMINFO psi, int lSize);

        [DllImport("avifil32.dll")]
        public static extern int AVIStreamGetFrameOpen(IntPtr pAVIStream, ref BITMAPINFOHEADER bih);

        [DllImport("avifil32.dll")]
        public static extern int AVIStreamGetFrameClose(int pGetFrameObj);

        [DllImport("avifil32.dll")]
        public static extern int AVIStreamGetFrame(int pGetFrameObj, int lPos);

        [DllImport("avifil32.dll")]
        public static extern IntPtr AVIStreamGetFrame(IntPtr pGet, int lPos);

        [DllImport("avifil32.dll")]
        public static extern int AVIFileRelease(int pfile);

        [DllImport("avifil32.dll")]
        public static extern void AVIFileExit();

        [DllImport("avifil32.dll")]
        public static extern int AVIStreamRelease(IntPtr aviStream);

        [DllImport("ntdll.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern int memcpy(int dst, int src, int count);

        #endregion

    }
}
