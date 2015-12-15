using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;

namespace IHGVM_VideoSplitter
{
    public class Cloakers
    {
        private Bitmap referenceImage;
        public Bitmap ReferenceImage
        {
            get { return referenceImage; }
            set { referenceImage = value; }
        }
        int threshold = 15;

        public Cloakers()
        {
 
        }

        public Cloakers(Bitmap referenceImage)
        {
            this.ReferenceImage = referenceImage;
        }

        public void ApplyDifference(BitmapData imageData)
        {
            int width = imageData.Width;
            int height = imageData.Height;

            Bitmap dstImage = CreateGrayscaleImage(width, height);
            BitmapData dstData = dstImage.LockBits(new Rectangle(0, 0, width, height), ImageLockMode.ReadWrite, PixelFormat.Format8bppIndexed);

            try
            {
                ProcessFilter(new BitmapCustomImage(imageData));
            }
            finally
            {
                dstImage.UnlockBits(dstData);
            }
        }

        public void ApplyThreshold(BitmapData imageData)
        {
            ProcessFilter(new BitmapCustomImage(imageData), new Rectangle(0, 0, imageData.Width, imageData.Height));            
        }

        public void ApplyTowardsImage(Bitmap image, Bitmap tmpImage)
        {
            BitmapData data = image.LockBits(new Rectangle(0, 0, image.Width, image.Height), ImageLockMode.ReadWrite, image.PixelFormat);
            try
            {
                ProcessFilterTowards(new BitmapCustomImage(data), tmpImage);
            }
            finally
            {
                image.UnlockBits(data);
            }
        }


        public static Bitmap CreateGrayscaleImage(int width, int height)
        {
            Bitmap image = new Bitmap(width, height, PixelFormat.Format8bppIndexed);
            SetGSPalette(image);
            return image;
        }

        public static void SetGSPalette(Bitmap image)
        {
            ColorPalette cp = image.Palette;
            for (int i = 0; i < 256; i++)
            {
                cp.Entries[i] = Color.FromArgb(i, i, i);
            }
            image.Palette = cp;
        }

        protected unsafe void ProcessFilter(BitmapCustomImage image)
        {            
            if (referenceImage != null)
            {
                BitmapData ovrData = referenceImage.LockBits(new Rectangle(0, 0, image.Width, image.Height), ImageLockMode.ReadOnly, image.PixelFormat);
                try
                {
                    ProcessFilter(image, new BitmapCustomImage(ovrData));
                }
                finally
                {
                    referenceImage.UnlockBits(ovrData);
                }
            }
        }

        protected unsafe void ProcessFilter(BitmapCustomImage image, BitmapCustomImage overlay)
        {
            int width = image.Width;
            int height = image.Height;
            int v;

            if ((image.PixelFormat == PixelFormat.Format8bppIndexed))
            {
                int srcOffset = image.Stride - width;
                int ovrOffset = overlay.Stride - width;

                byte* ptr = (byte*)image.ImageData.ToPointer();
                byte* ovr = (byte*)overlay.ImageData.ToPointer();

                for (int y = 0; y < height; y++)
                {
                    for (int x = 0; x < width; x++, ptr++, ovr++)
                    {
                        v = (int)*ptr - (int)*ovr;
                        *ptr = (v < 0) ? (byte)-v : (byte)v;
                    }
                    ptr += srcOffset;
                    ovr += ovrOffset;
                }
            }
        }

        protected unsafe void ProcessFilter(BitmapCustomImage image, Rectangle rect)
        {
            int startX = rect.Left;
            int startY = rect.Top;
            int stopX = startX + rect.Width;
            int stopY = startY + rect.Height;

            if (image.PixelFormat == PixelFormat.Format8bppIndexed)
            {
                int offset = image.Stride - rect.Width;
                byte* ptr = (byte*)image.ImageData.ToPointer();
                ptr += (startY * image.Stride + startX);

                for (int y = startY; y < stopY; y++)
                {
                    for (int x = startX; x < stopX; x++, ptr++)
                        *ptr = (byte)((*ptr >= threshold) ? 255 : 0);
                    ptr += offset;
                }
            }
            else
            {
                byte* basePtr = (byte*)image.ImageData.ToPointer() + startX * 2;
                int stride = image.Stride;

                for (int y = startY; y < stopY; y++)
                {
                    ushort* ptr = (ushort*)(basePtr + stride * y);
                    for (int x = startX; x < stopX; x++, ptr++)
                        *ptr = (ushort)((*ptr >= threshold) ? 65535 : 0);
                }
            }
        }

        protected unsafe void ProcessFilterTowards(BitmapCustomImage image, Bitmap tmpImage)
        {
            if (tmpImage != null)
            {
                int stepSize = 1;
                BitmapData tmpData = tmpImage.LockBits(new Rectangle(0, 0, image.Width, image.Height), ImageLockMode.ReadOnly, image.PixelFormat);
                BitmapCustomImage tmplay = new BitmapCustomImage(tmpData);

                try
                {
                    PixelFormat pixelFormat = image.PixelFormat;
                    int width = image.Width;
                    int height = image.Height;
                    int v;

                    if ((pixelFormat == PixelFormat.Format8bppIndexed))
                    {
                        int srcOffset = image.Stride - width;
                        int ovrOffset = tmplay.Stride - width;

                        byte* ptr = (byte*)image.ImageData.ToPointer();
                        byte* ovr = (byte*)tmplay.ImageData.ToPointer();

                        for (int y = 0; y < height; y++)
                        {
                            for (int x = 0; x < width; x++, ptr++, ovr++)
                            {
                                v = (int)*ovr - *ptr;
                                if (v > 0)
                                    *ptr += (byte)((stepSize < v) ? stepSize : v);
                                else if (v < 0)
                                {
                                    v = -v; 
                                    *ptr -= (byte)((stepSize < v) ? stepSize : v);
                                }
                            }
                            ptr += srcOffset;
                            ovr += ovrOffset;
                        }
                    }
                }
                finally
                {
                    tmpImage.UnlockBits(tmpData);
                }
            }
        }

    }

    public class BitmapCustomImage : IDisposable
    {
        private IntPtr imageData;
        private int width, height, stride;
        private PixelFormat pixelFormat;

        public IntPtr ImageData { get { return imageData; } }
        public int Width { get { return width; } }
        public int Height { get { return height; } }
        public int Stride { get { return stride; } }
        public PixelFormat PixelFormat { get { return pixelFormat; } }

        public BitmapCustomImage(BitmapData bitmapData)
        {
            this.imageData = bitmapData.Scan0;
            this.width = bitmapData.Width;
            this.height = bitmapData.Height;
            this.stride = bitmapData.Stride;
            this.pixelFormat = bitmapData.PixelFormat;
        }

        public void Dispose()
        {
            if ((imageData != IntPtr.Zero))
            {
                System.Runtime.InteropServices.Marshal.FreeHGlobal(imageData);
                System.GC.RemoveMemoryPressure(stride * height);
                imageData = IntPtr.Zero;
            }
            GC.SuppressFinalize(this);
        }
    }


    #region Opening - Erosion and Dilation

    public class OpeningFilter
    {
        private OpeningEroded openingEroded = new OpeningEroded();
        private OpeningDilute dilatation = new OpeningDilute();
        public Generate60s generateBW = new Generate60s(); 
        public int pixelCount = 0;
        public static int Size = 3;

        public OpeningFilter()
        {
 
        }

        public Bitmap ApplyOpeningEroded(BitmapData imageData)
        {
            int width = imageData.Width;
            int height = imageData.Height;

            PixelFormat dstPixelFormat = PixelFormat.Format8bppIndexed;
            Bitmap dstImage = (dstPixelFormat == PixelFormat.Format8bppIndexed) ? Cloakers.CreateGrayscaleImage(width, height) : new Bitmap(width, height, PixelFormat.Format8bppIndexed);
            BitmapData dstData = dstImage.LockBits(new Rectangle(0, 0, width, height), ImageLockMode.ReadWrite, dstPixelFormat);

            try
            {
                openingEroded.OpeningErodedProcessing(new BitmapCustomImage(imageData), new BitmapCustomImage(dstData), new Rectangle(0, 0, width, height));
            }
            finally
            {
                dstImage.UnlockBits(dstData);
            }

            return dstImage;
        }

        public Bitmap ApplyOpeningDilute(Bitmap image)
        {
            BitmapData srcData = image.LockBits(new Rectangle(0, 0, image.Width, image.Height), ImageLockMode.ReadOnly, image.PixelFormat);
            Bitmap dstImage = null;

            try
            {
                dstImage = ApplyDilation2(srcData);
                if ((image.HorizontalResolution > 0) && (image.VerticalResolution > 0))
                {
                    dstImage.SetResolution(image.HorizontalResolution, image.VerticalResolution);
                }
                this.pixelCount = dilatation.PixelCount;
            }
            finally
            {
                image.UnlockBits(srcData);
            }

            return dstImage;
        }

        public Bitmap ApplyDilation2(BitmapData imageData)
        {
            int width = imageData.Width;
            int height = imageData.Height;
            PixelFormat dstPixelFormat = PixelFormat.Format8bppIndexed;

            Bitmap dstImage = Cloakers.CreateGrayscaleImage(width, height);
            BitmapData dstData = dstImage.LockBits(new Rectangle(0, 0, width, height),ImageLockMode.ReadWrite, dstPixelFormat);
            try
            {
                dilatation.OpeningDiluteProcessing(new BitmapCustomImage(imageData), new BitmapCustomImage(dstData), new Rectangle(0, 0, width, height));
            }
            finally
            {
                dstImage.UnlockBits(dstData);
            }

            return dstImage;
        }

        public Bitmap Apply(BitmapData imageData)
        {
            Bitmap tempImage = this.ApplyOpeningEroded(imageData);
            Bitmap destImage = this.ApplyOpeningDilute(tempImage);
            tempImage.Dispose();
            return destImage;
        }


    }

    public class OpeningEroded
    {
        public OpeningEroded() { }

        public unsafe void OpeningErodedProcessing(BitmapCustomImage sourceData, BitmapCustomImage destinationData, Rectangle rect)
        {
            PixelFormat pixelFormat = sourceData.PixelFormat;

            int startX = rect.Left;
            int startY = rect.Top;
            int stopX = startX + rect.Width;
            int stopY = startY + rect.Height;
            int r = OpeningFilter.Size >> 1;
            bool isFound;

            if (pixelFormat == PixelFormat.Format8bppIndexed)
            {
                int dstStride = destinationData.Stride;
                int srcStride = sourceData.Stride;

                byte* baseSrc = (byte*)sourceData.ImageData.ToPointer();
                byte* baseDst = (byte*)destinationData.ImageData.ToPointer();


                for (int y = startY; y < stopY; y++)
                {
                    byte* src = baseSrc + y * srcStride;
                    byte* dst = baseDst + y * dstStride;
                    byte min, v;
                    int t, ir, jr, i, j;

                    for (int x = startX; x < stopX; x++, src++, dst++)
                    {
                        min = 255; isFound = false;
                        for (i = 0; i < OpeningFilter.Size; i++)
                        {
                            ir = i - r; t = y + ir;

                            if (t < startY) continue;
                            if (t >= stopY) break;
                            for (j = 0; j < OpeningFilter.Size; j++)
                            {
                                jr = j - r; t = x + jr;
                                if (t < startX) continue;
                                if (t < stopX)
                                {
                                    isFound = true;
                                    v = src[ir * srcStride + jr];
                                    if (v < min) min = v;
                                }
                            }
                        }
                        *dst = (isFound) ? min : *src;
                    }
                }
            }
        }
    }

    public class OpeningDilute
    {
        private int pixelCount = 0;

        public int PixelCount { get { return pixelCount; } }

        public OpeningDilute() { }

        public unsafe void OpeningDiluteProcessing(BitmapCustomImage sourceData, BitmapCustomImage destinationData, Rectangle rect)
        {
            PixelFormat pixelFormat = sourceData.PixelFormat;
            int startX = rect.Left;
            int startY = rect.Top;
            int stopX = startX + rect.Width;
            int stopY = startY + rect.Height;
            pixelCount = 0;

            int r = OpeningFilter.Size >> 1;
            bool isFound;

            if (pixelFormat == PixelFormat.Format8bppIndexed)
            {
                int pixelSize = (pixelFormat == PixelFormat.Format8bppIndexed) ? 1 : 3;
                int srcStride = sourceData.Stride;

                byte* baseSrc = (byte*)sourceData.ImageData.ToPointer();
                byte* baseDst = (byte*)destinationData.ImageData.ToPointer();


                for (int y = startY; y < stopY; y++)
                {
                    byte* src = baseSrc + y * srcStride;
                    byte* dst = baseDst + y * destinationData.Stride;
                    byte max, v;
                    int t, ir, jr, i, j;

                    for (int x = startX; x < stopX; x++, src++, dst++)
                    {
                        max = 0; isFound = false;
                        for (i = 0; i < OpeningFilter.Size; i++)
                        {
                            ir = i - r; t = y + ir;

                            if (t < startY) continue;
                            if (t >= stopY) break;

                            for (j = 0; j < OpeningFilter.Size; j++)
                            {
                                jr = j - r; t = x + jr;

                                if (t < startX) continue;
                                if (t < stopX)
                                {
                                    isFound = true;
                                    v = src[ir * srcStride + jr];
                                    if (v > max) max = v;
                                    if (v == 255) pixelCount++;
                                }
                            }
                        }
                        *dst = (isFound) ? max : *src;
                    }
                }
            }

        }
    }

    #endregion

    #region GrayScale - Generating GrayScale Image

    public class Generate60s
    {
        public double coeffr = 0.2125;
        public double coeffg = 0.7154;
        public double coeffb = 0.0721;

        public Bitmap Apply(Bitmap image)
        {
            BitmapData srcData = image.LockBits(new Rectangle(0, 0, image.Width, image.Height), ImageLockMode.ReadOnly, image.PixelFormat);
            Bitmap dstImage = null;
            try
            {
                dstImage = Apply(srcData);
                if ((image.HorizontalResolution > 0) && (image.VerticalResolution > 0))
                    dstImage.SetResolution(image.HorizontalResolution, image.VerticalResolution);
            }
            finally
            {
                image.UnlockBits(srcData);
            }

            return dstImage;
        }

        public Bitmap Apply(BitmapData imageData)
        {
            PixelFormat dstPixelFormat = PixelFormat.Format8bppIndexed;
            Bitmap dstImage = Cloakers.CreateGrayscaleImage(imageData.Width, imageData.Height);
            BitmapData dstData = dstImage.LockBits(new Rectangle(0, 0, imageData.Width, imageData.Height),ImageLockMode.ReadWrite, dstPixelFormat);
            try
            {
                Convert60sImage(new BitmapCustomImage(imageData), new BitmapCustomImage(dstData));
            }
            finally
            {
                dstImage.UnlockBits(dstData);
            }

            return dstImage;
        }

        protected unsafe void Convert60sImage(BitmapCustomImage sourceData, BitmapCustomImage destinationData)
        {
            int width = sourceData.Width;
            int height = sourceData.Height;
            PixelFormat srcPixelFormat = sourceData.PixelFormat;

            if ((srcPixelFormat == PixelFormat.Format24bppRgb))
            {
                int pixelSize = (srcPixelFormat == PixelFormat.Format24bppRgb) ? 3 : 4;
                int srcOffset = sourceData.Stride - width * pixelSize;
                int dstOffset = destinationData.Stride - width;

                int rc = (int)(0x10000 * coeffr);
                int gc = (int)(0x10000 * coeffg);
                int bc = (int)(0x10000 * coeffb);

                while (rc + gc + bc < 0x10000)
                {
                    bc++;
                }

                byte* src = (byte*)sourceData.ImageData.ToPointer();
                byte* dst = (byte*)destinationData.ImageData.ToPointer();

                for (int y = 0; y < height; y++)
                {
                    for (int x = 0; x < width; x++, src += pixelSize, dst++)
                    {
                        *dst = (byte)((rc * src[2] + gc * src[1] + bc * src[0]) >> 16);
                    }
                    src += srcOffset;
                    dst += dstOffset;
                }
            }
        }

    }

    #endregion

    
}
