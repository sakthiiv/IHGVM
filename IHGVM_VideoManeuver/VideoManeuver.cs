using System;
using System.Collections;

namespace IHGVM_VideoManeuver
{
    public class VideoManeuver
    {
        private int aviFile = 0;
        private ArrayList branches = new ArrayList();
        public VideoStrech videoStretch = null;

        public VideoManeuver(string fileName)
        {
            int fileResult;
            fileResult = Avi32.AVIFileOpen(ref aviFile, fileName, AVI32_CONSTANTS.CONS_READWRITE, 0);

            if (fileResult != 0)
                throw new Exception("Unable to open file: " + fileResult.ToString());
        }

        public VideoStrech GetVideoStream()
        {
            IntPtr aviStream;

            int result = Avi32.AVIFileGetStream(aviFile, out aviStream, Avi32.streamtype_video, 0);

            if (result != 0)
            {
                throw new Exception("Exception in AVIFileGetStream: " + result.ToString());
            }

            VideoStrech stream = new VideoStrech(aviFile, aviStream);
            branches.Add(stream);
            return stream;
        }

        public void Open()
        {
            videoStretch = this.GetVideoStream();
            videoStretch.GetFrameOpen();
        }

        public void Close()
        {
            foreach (AviStrech branch in branches)
            {
                branch.Close();
            }

            Avi32.AVIFileRelease(aviFile);
            Avi32.AVIFileExit();
            videoStretch.GetFrameClose();
        }

    }
}
