using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using IHGVM_VideoManeuver;
using IHGVM_VideoSplitter;
using System.Collections.ObjectModel;
using System.Threading;
using System.Drawing;

namespace IHGVM
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class IndexWindow : Window
    {
        public IndexWindow()
        {
            InitializeComponent();

            // Control Settings Initialization
            txtFileName.IsReadOnly = true;
        }

        OpenFileDialog openFileDialog;
        ProcessFrame processFrame;
        FrameWriter frameWriter;
        FrameReader frameReader = new FrameReader();
        Locator locator = new Locator();

        private Thread frameThread = null;
        private Thread frameSavingThread = null;
        public bool Running
        {
            get
            {
                if (frameThread != null)
                {
                    if (frameThread.Join(0) == false)
                        return true;
                    Free();
                }
                return false;
            }
        }

        public bool SaveRunning
        {
            get
            {
                if (frameSavingThread != null)
                {
                    if (frameSavingThread.Join(0) == false)
                        return true;
                    SaveFree();
                }
                return false;
            }
        }

        private void Free()
        {
            frameThread = null;
        }

        private void SaveFree()
        {
            frameSavingThread = null;
        }

        public void Start()
        {
            if (frameThread == null)
            {
                ProgresserToggle();
                ProgressSwitcher(true);

                frameThread = new Thread(() => WorkerThread());
                frameThread.Name = processFrame.Source;
                frameThread.Start();
            }
        }

        public void SaveStart()
        {
            if (frameSavingThread == null)
            {
                ProgresserToggle();
                ProgressSwitcher(false);

                frameSavingThread = new Thread(() => SaveWorkerThread());
                frameSavingThread.Name = processFrame.Source + INDEX_CONSTANTS.STATE_SAVE_THREAD;
                frameSavingThread.Start();
            }
        }

        public void WorkerThread()
        {
            List<Activity> tempActivities;
            try
            {
                frameReader.Read(processFrame.Source);
                Dispatcher.BeginInvoke(new Action(delegate() 
                {
                    pbFrameStatus.Maximum = processFrame.FrameLength;
                    lblFrameLength.Content = INDEX_CONSTANTS.LABLE_FRAME_LENGTH + (processFrame.FrameLength);
                    lblFrameRate.Content = INDEX_CONSTANTS.LABLE_FRAME_RATE + processFrame.FrameRate;
                    lblCodec.Content = INDEX_CONSTANTS.LABLE_CODEC + processFrame.Codec;
                }));

                locator.silhouetteFrame = null;
                while (true)
                {
                    Bitmap bmp = frameReader.GetNextFrame();
                    locator.LocateEachFrame(ref bmp, frameReader.FramePosition);
                    //this.SaveFile(bmp);
                    bmp.Dispose();

                    FrameCountDispatcher();

                    if ((frameReader.FramePosition + 1) >= frameReader.VideoStrech.Length)
                        break;
                }

                tempActivities = processFrame.ActivityFetching();                
                Dispatcher.BeginInvoke(new Action(delegate() 
                {
                    lblProcessedFrames.Content = "Processed Frames: " + processFrame.FrameLength;
                    pbFrameStatus.Value = 0;
                    ActivityTimeList.Items.Clear();
                    for (int i = 0; i < tempActivities.Count; i++)
                    {
                        ActivityTimeList.Items.Add(tempActivities[i]);
                    }                    
                    ProgresserToggle();
                }));

                //frameReader.Dispose();

            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("exception : " + ex.Message);
            }

            this.Free();
        }

        public void SaveWorkerThread()
        {
            try
            {
                int activityIndex = -1;
                Dispatcher.BeginInvoke(new Action(delegate()
                {
                    activityIndex = ActivityTimeList.SelectedIndex;
                }));

                Thread.Sleep(100);
                if (activityIndex >= 0)
                    frameWriter.SaveActivity(activityIndex);
                else
                    frameWriter.SaveActivity();

                Dispatcher.BeginInvoke(new Action(delegate()
                {
                    ActivityTimeList.SelectedIndex = -1;
                    ProgresserToggle();
                }));
            }
            catch (Exception ex)
            {
 
            }
            this.SaveFree();
        }

        public void FrameCountDispatcher()
        {
            int forEvery = 100;
            //if (frameReader.FramePosition % forEvery == 0) { 
                Dispatcher.BeginInvoke(new Action(delegate() 
                {
                    lblProcessedFrames.Content = "Processed Frames: " + frameReader.FramePosition;
                    pbFrameStatus.Value = frameReader.FramePosition;
                }));
            //}
        }

        public void ProgresserToggle()
        {
            switch (grdProgresser.Visibility)
            {
                case Visibility.Hidden:                   
                    grdProgresser.Visibility = Visibility.Visible;
                    break;
                case Visibility.Visible:
                    lblProcessedFrames.Visibility = Visibility.Hidden;
                    grdProgresser.Visibility = Visibility.Hidden;
                    break;
            }
            ToggleDisabled(grdProgresser.Visibility == Visibility.Hidden);

        }

        public void ProgressSwitcher(bool isFrame)
        {
            pbFrameStatus.IsIndeterminate = !isFrame;
            if (isFrame)
            {
                lblProgress.Content = INDEX_CONSTANTS.FRAME_EXTRACTION;
                lblProcessedFrames.Visibility = Visibility.Visible;
            }
            else
            {
                lblProgress.Content = INDEX_CONSTANTS.ACTIVITY_SAVING;
                lblProcessedFrames.Visibility = Visibility.Hidden;
            }
        }

        public void Stop()
        {
            if (this.Running)
            {
                frameThread.Abort();
            }
        }

        public void SaveStop()
        {
            if (this.SaveRunning)
            {
                frameSavingThread.Abort();
            }
        }

        #region Events & Common Functions

        private void ToggleDisabled(bool enabled)
        {
            btnOpenFile.IsEnabled = enabled;
            btnProcess.IsEnabled = enabled;
            btnReset.IsEnabled = enabled;
            btnSaveActivity.IsEnabled = enabled;
            btnSaveActivities.IsEnabled = enabled;
            txtThreshold.IsEnabled = enabled;
            txtPercentage.IsEnabled = enabled;
            txtPixels.IsEnabled = enabled;
        }

        private void Reset()
        {
            openFileDialog = null;
            txtFileName.Text = "";
            lblFrameLength.Content = "";
            lblFrameRate.Content = "";
            lblCodec.Content = "";
            ActivityTimeList.Items.Clear();
        }

        private void btnOpenFile_Click(object sender, RoutedEventArgs e)
        {
            this.Reset();
            openFileDialog = new OpenFileDialog();
            openFileDialog.Filter = INDEX_CONSTANTS.FILE_FILTERS;

            //String path = INDEX_CONSTANTS.STRING_PATH;
            if (openFileDialog.ShowDialog() == true && openFileDialog.FileName != null)
            {
                txtFileName.Text = openFileDialog.FileName;
            }
        }

        private void btnProcess_Click(object sender, RoutedEventArgs e)
        {
            if (openFileDialog != null)
            {
                processFrame = new ProcessFrame(openFileDialog.FileName, frameReader, locator);
                frameWriter = new FrameWriter(processFrame);
                this.Start();
            }
        }

        private void btnReset_Click(object sender, RoutedEventArgs e)
        {
            this.Reset();
        }

        private void btnSaveActivity_Click(object sender, RoutedEventArgs e)
        {
            if (ActivityTimeList.SelectedIndex < 0)
                MessageBox.Show(INDEX_CONSTANTS.SAVE_ERROR, INDEX_CONSTANTS.APPLICATION_CAPTION, MessageBoxButton.OK);
            else
                this.SaveStart();
        }

        #endregion


    }
}
