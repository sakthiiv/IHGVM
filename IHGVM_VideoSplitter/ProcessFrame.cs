﻿using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace IHGVM_VideoSplitter
{
    public class ProcessFrame
    {

        private FrameReader frameReader;
        private Locator locator;
        private string source = "";
        public List<Activity> activityList = new List<Activity>();

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
                return "";
            }
        }

        public string Source { get { return source; } }

        public ProcessFrame(string fileSource, FrameReader frameReader, Locator locator)
        {
            this.source = fileSource;
            this.frameReader = frameReader;
            this.locator = locator;
        }

        public List<Activity> ActivityFetching()
        {
            bool activity = false;            
            activityList.Clear();
            if (locator != null)
            {
                for (int i = 0; i < locator.pixelsList.Count; i++)
                {
                    if (locator.pixelsList[i] > 500)
                    {
                        if (!activity)
                        {
                            activityList.Add(new Activity() { StartTime = (i + 1) / FrameRate });
                            activityList[activityList.Count - 1].FrameNumber = (i + 1);
                            activity = !activity;
                        }
                    }
                    if (locator.pixelsList[i] < 500 || (locator.pixelsList.Count == (i + 1)))
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
            if (!Directory.Exists(path))
                Directory.CreateDirectory(path);

            Graphics g = Graphics.FromImage(test);
            g.DrawImage(test, 0, 0);
            g.Dispose();

            test.Save(path + this.frameReader.FramePosition + "_Percent_" + 0 + ".bmp", ImageFormat.Bmp);
        }

        public List<Activity> ClearSecondsVariation(List<Activity> activityList) 
        {
            List<Activity> clearedList = new List<Activity>();
            try
            {                
                int row = 0, differenceTh = 3;
                if (activityList.Count == 1) {
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
}
