using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using IO = System.IO;

namespace IHGVM_VideoSplitter
{
    public class ProcessFrame
    {
        private FrameReader frameReader;
        private Locator locator;
        private InputSettings inSet;
        private string source = string.Empty;
        private string fileName = string.Empty;
        public List<Activity> activityList = new List<Activity>();
        public List<Activity> clearedList;

        public int FrameLength
        {
            get
            {
                if (frameReader != null && frameReader.VideoStrech != null)
                {
                    return frameReader.VideoStrech.Length;
                }
                return 0;
            }
        }

        public double FrameRate
        {
            get
            {
                if (frameReader != null && frameReader.VideoStrech != null)
                {
                    return frameReader.VideoStrech.FrameRate;
                }
                return 0;
            }
        }

        public string Codec
        {
            get
            {
                if (frameReader != null && frameReader.VideoStrech != null)
                {
                    return frameReader.VideoStrech.Codec;
                }
                return string.Empty;
            }
        }

        public string FileName { get { return fileName; } }
        public string Source { get { return source; } }
        public FrameReader FrameReader { get { return frameReader; } }


        public ProcessFrame(string fileSource, FrameReader frameReader, Locator locator, InputSettings inSet)
        {
            this.source = fileSource;
            this.frameReader = frameReader;
            this.locator = locator;
            this.fileName = IO.Path.GetFileNameWithoutExtension(fileSource);
            this.inSet = inSet;
        }

        public List<Activity> ActivityFetching()
        {
            bool activity = false;
            activityList.Clear();
            if (locator != null)
            {
                for (int i = 0; i < locator.pixelsList.Count; i++)
                {
                    if (locator.pixelsList[i] > inSet.Pixel)
                    {
                        if (!activity)
                        {
                            activityList.Add(new Activity() { StartTime = (i + 1) / FrameRate });
                            activityList[activityList.Count - 1].FrameNumber = (i + 1);
                            activity = !activity;
                        }
                    }
                    if (locator.pixelsList[i] < inSet.Pixel || (locator.pixelsList.Count == (i + 1)))
                    {
                        if (activity && activityList.Count > 0)
                        {
                            activityList[activityList.Count - 1].EndTime = (i + 1) / FrameRate;
                            activity = !activity;
                        }
                    }
                }
                locator.pixelsList.Clear();
            }
            return this.ClearSecondsVariation(activityList);
        }

        public void SaveFile(Bitmap test, int Percent = 0)
        {

            string path = @"..\BitMaps\";
            if (!IO.Directory.Exists(path))
                IO.Directory.CreateDirectory(path);

            Graphics g = Graphics.FromImage(test);
            g.DrawImage(test, 0, 0);
            g.Dispose();

            test.Save(path + this.frameReader.FramePosition + "_Percent_" + 0 + ".bmp", ImageFormat.Bmp);
        }

        public List<Activity> ClearSecondsVariation(List<Activity> activityList)
        {
            clearedList = new List<Activity>();
            try
            {
                int row = 0, differenceTh = inSet.Threshold;
                if (activityList.Count == 1)
                {
                    clearedList.Add(new Activity() { StartTime = activityList[0].StartTime, EndTime = activityList[0].EndTime, FrameNumber = activityList[0].FrameNumber });
                }
                for (int i = 1; i < activityList.Count; i++)
                {
                    if (i == 1)
                        clearedList.Add(new Activity() { StartTime = activityList[0].StartTime, EndTime = activityList[0].EndTime, FrameNumber = activityList[0].FrameNumber });
                    if (row < clearedList.Count)
                    {
                        if (((activityList[i].StartTime - clearedList[row].EndTime) <= differenceTh))
                        {
                            clearedList[row].EndTime = activityList[i].EndTime;
                        }
                        else
                        {
                            clearedList.Add(new Activity() { StartTime = activityList[i].StartTime, EndTime = activityList[i].EndTime, FrameNumber = activityList[i].FrameNumber });
                            row++;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                return activityList;
            }

            return clearedList;
        }

    }

    public class Activity
    {
        private double startTime = 0;
        private double endTime = 0;
        private int frameNumber = 0;

        public double StartTime { get { return startTime; } set { startTime = value; } }
        public double EndTime { get { return endTime; } set { endTime = value; } }
        public int FrameNumber { get { return frameNumber; } set { frameNumber = value; } }
    }

    public class InputSettings
    {

        private int threshold = 0;
        private int percentage = 0;
        private int pixel = 0;

        public InputSettings(int t, int per, int p)
        {
            this.threshold = t;
            this.percentage = per;
            this.pixel = p;
        }

        public int Threshold { get { return threshold; } }
        public int Percentage { get { return percentage; } }
        public int Pixel { get { return pixel; } }
    }
}
