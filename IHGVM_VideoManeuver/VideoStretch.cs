using System;
using System.Runtime.InteropServices;
using System.Drawing;
using System.Drawing.Imaging;
using System.Collections.Generic;

namespace IHGVM_VideoManeuver
{
    public abstract class AviStrech
    {
        protected int aviFile;
        protected IntPtr aviStream;

        internal int FilePointer { get { return aviFile; } }
        internal virtual IntPtr StreamPointer { get { return aviStream; } }

        public virtual void Close()
        {
            Avi32.AVIStreamRelease(StreamPointer);
        }
    }

    public class VideoStrech : AviStrech
    {

        #region Variables & Properties

        private int getFrameObject;
        private double frameRate;
        private int frameSize;
        private int width;
        private int height;

        private int start;
        private int length;
        private int firstFrame = 0;
        private int position;        

        private string codec;

        List<Bitmap> bitMaps = new List<Bitmap>();
        public int FrameSize { get { return frameSize; } }
        public double FrameRate { get { return frameRate; } }
        public int Width { get { return width; } }
        public int Height { get { return height; } }

        public int Start { get { return start; } }
        public int Length { get { return length; } }
        public int FirstFrame { get { return firstFrame; } }        

        public string Codec { get { return codec; } }

        public int CurrentPosition
        {
            get { return position; }
            set
            {
                if ((value < start) || (value >= start + length))
                    position = start;
                else
                    position = value;
            }
        }

        #endregion

        public VideoStrech(int aviFile, IntPtr aviStream)
        {
            this.aviFile = aviFile;
            this.aviStream = aviStream;

            Avi32.BITMAPINFOHEADER bih = new Avi32.BITMAPINFOHEADER();
            int size = Marshal.SizeOf(bih);
            Avi32.AVIStreamReadFormat(aviStream, 0, ref bih, ref size);
            Avi32.AVISTREAMINFO streamInfo = GetStreamInfo(aviStream);

            this.bitMaps = new List<Bitmap>();
            this.frameRate = (float)streamInfo.dwRate / (float)streamInfo.dwScale;
            this.width = (int)streamInfo.rcFrame.right;
            this.height = (int)streamInfo.rcFrame.bottom;
            this.frameSize = bih.biSizeImage;
            this.firstFrame = Avi32.AVIStreamStart(aviStream.ToInt32());
            this.position = streamInfo.dwStart;
            this.start = streamInfo.dwStart;
            this.length = streamInfo.dwLength;
            this.codec = Avi32.decode_mmioFOURCC(streamInfo.fccHandler);
        }

        private Avi32.AVISTREAMINFO GetStreamInfo(IntPtr aviStream)
        {
            Avi32.AVISTREAMINFO streamInfo = new Avi32.AVISTREAMINFO();
            int result = Avi32.AVIStreamInfo(StreamPointer, ref streamInfo, Marshal.SizeOf(streamInfo));
            if (result != 0)
            {
                throw new Exception("Exception in VideoStreamInfo: " + result.ToString());
            }
            return streamInfo;
        }

        public void GetFrameOpen()
        {
            Avi32.AVISTREAMINFO streamInfo = GetStreamInfo(StreamPointer);
            Avi32.BITMAPINFOHEADER bitInfoHeader = this.InitializeBitmapHeader(new Avi32.BITMAPINFOHEADER());
            getFrameObject = Avi32.AVIStreamGetFrameOpen(StreamPointer, ref bitInfoHeader);

            if (getFrameObject == 0) { throw new Exception("Exception in VideoStreamGetFrameOpen!"); }
        }

        public Bitmap GetSubsequentPosition(int framePosition)
        {
            IntPtr dib = Avi32.AVIStreamGetFrame(new IntPtr(getFrameObject), framePosition);
            Avi32.BITMAPINFOHEADER bih = new Avi32.BITMAPINFOHEADER();
            bih = (Avi32.BITMAPINFOHEADER)Marshal.PtrToStructure(dib, bih.GetType());

            Bitmap bmp = new Bitmap(this.width, this.height, PixelFormat.Format24bppRgb);
            BitmapData bmData = bmp.LockBits(new Rectangle(0, 0, width, height), ImageLockMode.ReadWrite, PixelFormat.Format24bppRgb);

            int srcStride = bmData.Stride;
            int dstStride = bmData.Stride;

            int dst = bmData.Scan0.ToInt32() + dstStride * (height - 1);
            int src = dib.ToInt32() + Marshal.SizeOf(typeof(Avi32.BITMAPINFOHEADER));

            for (int y = 0; y < height; y++)
            {
                Avi32.memcpy(dst, src, srcStride);
                dst -= dstStride;
                src += srcStride;
            }

            bmp.UnlockBits(bmData);
            return bmp;
        }



        private Avi32.BITMAPINFOHEADER InitializeBitmapHeader(Avi32.BITMAPINFOHEADER tempBih)
        {
            tempBih.biClrImportant = 0;
            tempBih.biClrUsed = 0;
            tempBih.biCompression = 0;
            tempBih.biPlanes = 1;
            tempBih.biSize = Marshal.SizeOf(tempBih);
            tempBih.biXPelsPerMeter = 0;
            tempBih.biYPelsPerMeter = 0;
            tempBih.biHeight = 0;
            tempBih.biWidth = 0;
            tempBih.biBitCount = 24;

            return tempBih;
        }

        public void GetFrameClose()
        {
            if (getFrameObject != 0)
            {
                Avi32.AVIStreamGetFrameClose(getFrameObject);
                getFrameObject = 0;
            }
        }

    }
}
