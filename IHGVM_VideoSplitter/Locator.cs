using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IHGVM_VideoSplitter
{
    public class Locator
    {
        public Bitmap silhouetteFrame;
        private BitmapData bitmapData;
        private int counter = 0;
        
        private int width;
        private int height;

        public List<int> pixelsList = new List<int>();
        private int pixelsChanged;

        OpeningFilter of;
        Cloakers cs;

        public Locator()
        {
            cs = new Cloakers();
            of = new OpeningFilter();
        }

        public double MotionLevel
        {
            get { return (double)pixelsChanged / (width * height); }
        }

        public void ProcessFrame(ref Bitmap image, int framePosition)
        {
            Graphics g;
            string path = @"..\BitMaps\";
            if (!Directory.Exists(path))
                Directory.CreateDirectory(path);

            Bitmap tmpImage;
            tmpImage = of.generateBW.Apply(image);

            if (silhouetteFrame == null)
            {
                silhouetteFrame = (Bitmap)tmpImage.Clone();
                width = image.Width;
                height = image.Height;
                return;
            }

            if (++counter == 2)
            {
                counter = 0;
                cs.ApplyTowardsImage(silhouetteFrame, tmpImage);
            }

            cs.ReferenceImage = silhouetteFrame;
            bitmapData = tmpImage.LockBits(new Rectangle(0, 0, width, height),
            ImageLockMode.ReadWrite, PixelFormat.Format8bppIndexed);
            cs.ApplyDifference(bitmapData);
            cs.ApplyThreshold(bitmapData);

            of.Apply(bitmapData);
            pixelsList.Add(of.pixelCount);

            //Bitmap tmpImage2 = of.Apply(bitmapData);
            //g = Graphics.FromImage(new Bitmap(tmpImage2.Width, tmpImage2.Height));
            //g.DrawImage(tmpImage2, 0, 0);
            //g.Dispose();
            //tmpImage2.Save(path + framePosition + "_" + of.pixelCount + ".bmp", ImageFormat.Bmp);

            tmpImage.UnlockBits(bitmapData);
            tmpImage.Dispose();

        }

    }
}
