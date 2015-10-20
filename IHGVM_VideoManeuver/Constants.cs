using System;

namespace IHGVM_VideoManeuver
{

    public static class INDEX_CONSTANTS
    {
        public const string LABLE_FRAME_LENGTH = "Total No. of Frames in this Video: ";
        public const string LABLE_FRAME_RATE = "Frame Rate of this Video: ";
        public const string LABLE_CODEC = "Codec Type of this Video: ";
        public const string STRING_PATH = @"..\BitMaps\";
        public const string FILE_FILTERS = "Avi files (*.avi)|*.avi|All files (*.*)|*.*";
        public const string APPLICATION_CAPTION = "IHGVM";
        public const string SAVEALL_ERROR = "No new videos processed. Please choose a video to process.";
        public const string SAVE_ERROR = "Please choose an activity to save.";
    }

    public class AVI32_CONSTANTS
    {
        public static int PALETTE_SIZE = 4 * 256;
        public const int CONS_WRITE = 1;
        public const int CONS_READWRITE = 2;
        public const int CONS_CREATE = 4096;
        public const int BMP_MAGIC_COOKIE = 19778;
    }
}
