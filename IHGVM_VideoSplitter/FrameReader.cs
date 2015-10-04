using IHGVM_VideoManeuver;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IHGVM_VideoSplitter
{
    public class FrameReader
    {

        VideoManeuver videoManeuver;
        public VideoStrech VideoStrech
        {
            get
            {
                if (videoManeuver != null)
                    return videoManeuver.videoStretch;
                return null;
            }
        }

        private int framePosition;
        public int FramePosition
        {
            get { return framePosition; }
            set
            {
                if ((value < VideoStrech.Start) || (value >= VideoStrech.Start + VideoStrech.Length))
                    framePosition = VideoStrech.Start;
                else
                    framePosition = value;
            }
        }


        public FrameReader()
        {

        }

        public void Read(string fileSource)
        {
            videoManeuver = new VideoManeuver(fileSource);
            videoManeuver.Open();
            FramePosition = VideoStrech.Start;
        }

        public Bitmap GetNextFrame()
        {

            Bitmap bitmap = videoManeuver.videoStretch.GetSubsequentPosition(FramePosition);
            FramePosition++;
            return bitmap;
        }

        public void Dispose()
        {
            videoManeuver.Close();
        }
    }
}
