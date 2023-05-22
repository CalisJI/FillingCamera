using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using System.Runtime.InteropServices;
using MVSDK;
using CameraHandle = System.Int32;
using MvApi = MVSDK.MvApi;
using System.IO;
using System.Threading;
using System.Reflection;
using System.Windows.Threading;

namespace FirstStepMulti
{
    public partial class Form1 : Form
    {
        #region variable
        protected IntPtr[] m_Grabber = new IntPtr[6];
        protected CameraHandle[] m_hCamera = new CameraHandle[6];
        protected tSdkCameraDevInfo[] m_DevInfo;
        protected pfnCameraGrabberFrameCallback m_FrameCallback;
        protected pfnCameraGrabberSaveImageComplete[] m_SaveImageComplete = new pfnCameraGrabberSaveImageComplete[6];
        BackgroundWorker m_BackgroundWorker;
        BackgroundWorker m_BackgroundWorker1;
        #endregion
        int[] countimage = new int[6];
        public Form1()
        {
            InitializeComponent();
            textBox2.Text = "C:\\Users\\admin\\Desktop\\saveimage";
            m_BackgroundWorker = new BackgroundWorker();
            m_BackgroundWorker1 = new BackgroundWorker();
            m_BackgroundWorker1.DoWork += M_BackgroundWorker1_DoWork;
            m_BackgroundWorker1.RunWorkerCompleted += M_BackgroundWorker1_RunWorkerCompleted;
            m_BackgroundWorker1.WorkerSupportsCancellation = true;
            m_BackgroundWorker.DoWork += M_BackgroundWorker_DoWork;
            m_BackgroundWorker.RunWorkerCompleted += M_BackgroundWorker_RunWorkerCompleted;
            m_BackgroundWorker.WorkerSupportsCancellation = true;
            m_FrameCallback = new pfnCameraGrabberFrameCallback(CameraGrabberFrameCallback);
            m_SaveImageComplete[0] = new pfnCameraGrabberSaveImageComplete(CameraGrabberSaveImageComplete);
            m_SaveImageComplete[1] = new pfnCameraGrabberSaveImageComplete(CameraGrabberSaveImageComplete1);
            m_SaveImageComplete[2] = new pfnCameraGrabberSaveImageComplete(CameraGrabberSaveImageComplete2);
            m_SaveImageComplete[3] = new pfnCameraGrabberSaveImageComplete(CameraGrabberSaveImageComplete3);
            m_SaveImageComplete[4] = new pfnCameraGrabberSaveImageComplete(CameraGrabberSaveImageComplete4);
            m_SaveImageComplete[5] = new pfnCameraGrabberSaveImageComplete(CameraGrabberSaveImageComplete5);
            MvApi.CameraEnumerateDevice(out m_DevInfo);
            int NumDev = (m_DevInfo != null ? Math.Min(m_DevInfo.Length, 6) : 0);

            IntPtr[] hDispWnds = { this.DispWnd1.Handle, this.DispWnd2.Handle, this.DispWnd3.Handle, this.DispWnd4.Handle, this.DispWnd5.Handle, this.DispWnd6.Handle };
            for (int i = 0; i < NumDev; ++i)
            {
                if (MvApi.CameraGrabber_Create(out m_Grabber[i], ref m_DevInfo[i]) == CameraSdkStatus.CAMERA_STATUS_SUCCESS)
                {
                    MvApi.CameraGrabber_GetCameraHandle(m_Grabber[i], out m_hCamera[i]);
                    MvApi.CameraCreateSettingPage(m_hCamera[i], this.Handle, m_DevInfo[i].acFriendlyName, null, (IntPtr)0, 0);

                    MvApi.CameraGrabber_SetRGBCallback(m_Grabber[i], m_FrameCallback, IntPtr.Zero);
                    MvApi.CameraGrabber_SetSaveImageCompleteCallback(m_Grabber[i], m_SaveImageComplete[i], IntPtr.Zero);


                    tSdkCameraCapbility cap;
                    MvApi.CameraGetCapability(m_hCamera[i], out cap);
                    if (cap.sIspCapacity.bMonoSensor != 0)
                        MvApi.CameraSetIspOutFormat(m_hCamera[i], (uint)MVSDK.emImageFormat.CAMERA_MEDIA_TYPE_MONO8);

                    MvApi.CameraGrabber_SetHWnd(m_Grabber[i], hDispWnds[i]);
                }
            }
            for (int i = 0; i < NumDev; ++i)
            {
                if (m_Grabber[i] != IntPtr.Zero)
                    MvApi.CameraGrabber_StartLive(m_Grabber[i]);
            }

        }

        private void M_BackgroundWorker1_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            if (!m_BackgroundWorker1.IsBusy)
            {
                m_BackgroundWorker1.RunWorkerAsync();
            }
        }

        private void M_BackgroundWorker1_DoWork(object sender, DoWorkEventArgs e)
        {
            if (m_BackgroundWorker1.CancellationPending)
            {
                e.Cancel = true;
            }
            else
            {

            }
        }

        private void M_BackgroundWorker_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            if (!m_BackgroundWorker.IsBusy)
            {
                m_BackgroundWorker.RunWorkerAsync();
            }
        }

        private void M_BackgroundWorker_DoWork(object sender, DoWorkEventArgs e)
        {
            if (m_BackgroundWorker.CancellationPending)
            {
                e.Cancel = true;
            }
            else
            {


            }
        }


        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            for (int i = 0; i < 4; ++i)
            {
                if (m_Grabber[i] != IntPtr.Zero)
                    MvApi.CameraGrabber_Destroy(m_Grabber[i]);
            }
        }

        private void CameraGrabberFrameCallback(
            IntPtr Grabber,
            IntPtr pFrameBuffer,
            ref tSdkFrameHead pFrameHead,
            IntPtr Context)
        {

        }

        private void CameraGrabberSaveImageComplete(
            IntPtr Grabber,
            IntPtr Image,
            CameraSdkStatus Status,
            IntPtr Context)
        {
            if (Image != IntPtr.Zero)
            {
                countimage[0]++;
                string filename = System.IO.Path.Combine(
                        textBox2.Text.ToString(),
                        string.Format("{0}.bmp", "Camera1_image_" + countimage[0] + "_" + counttimer[0]));

                MvApi.CameraImage_SaveAsBmp(Image, filename);
                //  MessageBox.Show(filename + counttimer[0]);

            }

            MvApi.CameraImage_Destroy(Image);
            counttimer[1]++;
            if (m_Grabber[1] != IntPtr.Zero)
            {
                SoftTrigger(1);
            }
            else
            {
                enable = false;
            }

        }

        private void CameraGrabberSaveImageComplete1(
           IntPtr Grabber,
           IntPtr Image,
           CameraSdkStatus Status,
           IntPtr Context)
        {
            if (Image != IntPtr.Zero)
            {
                countimage[1]++;
                string filename = System.IO.Path.Combine(
                        textBox2.Text.ToString(),
                        string.Format("{0}.bmp", "Camera2_image_" + countimage[1] + "_" + counttimer[1]));

                MvApi.CameraImage_SaveAsBmp(Image, filename);
                //MessageBox.Show(filename + counttimer[1]);
            }
            MvApi.CameraImage_Destroy(Image);
            if (m_Grabber[2] != IntPtr.Zero)
            {
                counttimer[2]++;

                SoftTrigger(2);
            }
            else
            {
                enable = false;
            }

        }

        private void CameraGrabberSaveImageComplete2(
           IntPtr Grabber,
           IntPtr Image,
           CameraSdkStatus Status,
           IntPtr Context)
        {
            if (Image != IntPtr.Zero)
            {
                countimage[2]++;
                string filename = System.IO.Path.Combine(
                        textBox2.Text.ToString(),
                        string.Format("{0}.bmp", "Camera3_image_" + countimage[2] + "_" + counttimer[2]));

                MvApi.CameraImage_SaveAsBmp(Image, filename);
                // MessageBox.Show(filename);
            }

            MvApi.CameraImage_Destroy(Image);
            if (m_Grabber[3] != IntPtr.Zero)
            {
                counttimer[3]++;
                SoftTrigger(3);

            }
            else
            {
                enable = false;
            }

        }

        private void CameraGrabberSaveImageComplete3(
           IntPtr Grabber,
           IntPtr Image,
           CameraSdkStatus Status,
           IntPtr Context)
        {
            if (Image != IntPtr.Zero)
            {
                countimage[3]++;
                string filename = System.IO.Path.Combine(
                        textBox2.Text.ToString(),
                        string.Format("{0}.bmp", "Camera4_image_" + countimage[3] + "_" + counttimer[3]));

                MvApi.CameraImage_SaveAsBmp(Image, filename);
                //  MessageBox.Show(filename);
            }

            MvApi.CameraImage_Destroy(Image);
            if (m_Grabber[4] != IntPtr.Zero)
            {
                counttimer[4]++;

                SoftTrigger(4);
            }
            else
            {
                enable = false;
            }

        }

        private void CameraGrabberSaveImageComplete4(
           IntPtr Grabber,
           IntPtr Image,
           CameraSdkStatus Status,
           IntPtr Context)
        {
            if (Image != IntPtr.Zero)
            {
                countimage[4]++;
                string filename = System.IO.Path.Combine(
                       textBox2.Text.ToString(),
                       string.Format("{0}.bmp", "Camera5_image_" + countimage[4] + "_" + counttimer[4]));

                MvApi.CameraImage_SaveAsBmp(Image, filename);
                //  MessageBox.Show(filename);
            }

            MvApi.CameraImage_Destroy(Image);
            if (m_Grabber[5] != IntPtr.Zero)
            {
                counttimer[5]++;

                SoftTrigger(5);
            }
            else
            {
                enable = false;
            }

        }
        private void CameraGrabberSaveImageComplete5(
           IntPtr Grabber,
           IntPtr Image,
           CameraSdkStatus Status,
           IntPtr Context)
        {
            if (Image != IntPtr.Zero)
            {

                countimage[5]++;
                string filename = System.IO.Path.Combine(
                         textBox2.Text.ToString(),
                         string.Format("{0}.bmp", "Camera6_image_" + countimage[5] + "_" + counttimer[5]));

                MvApi.CameraImage_SaveAsBmp(Image, filename);
                //  MessageBox.Show(filename);
            }

            MvApi.CameraImage_Destroy(Image);
            enable = false;
        }

        private void buttonSettings1_Click(object sender, EventArgs e)
        {
            if (m_Grabber[0] != IntPtr.Zero)
                MvApi.CameraShowSettingPage(m_hCamera[0], 1);
        }

        private void buttonPlay1_Click(object sender, EventArgs e)
        {
            if (m_Grabber[0] != IntPtr.Zero)
                MvApi.CameraSetTriggerMode(m_hCamera[0], 0);
            MvApi.CameraGrabber_StartLive(m_Grabber[0]);
        }

        private void buttonStop1_Click(object sender, EventArgs e)
        {
            if (m_Grabber[0] != IntPtr.Zero)
                MvApi.CameraGrabber_StopLive(m_Grabber[0]);
        }

        private void buttonSnap1_Click(object sender, EventArgs e)
        {
            if (m_Grabber[0] != IntPtr.Zero)
                MvApi.CameraGrabber_SaveImageAsync(m_Grabber[0]);
        }

        private void buttonSettings2_Click(object sender, EventArgs e)
        {
            if (m_Grabber[1] != IntPtr.Zero)
                MvApi.CameraShowSettingPage(m_hCamera[1], 1);
        }

        private void buttonPlay2_Click(object sender, EventArgs e)
        {
            if (m_Grabber[1] != IntPtr.Zero)
                MvApi.CameraSetTriggerMode(m_hCamera[1], 0);
            MvApi.CameraGrabber_StartLive(m_Grabber[1]);
        }

        private void buttonStop2_Click(object sender, EventArgs e)
        {
            if (m_Grabber[1] != IntPtr.Zero)
                MvApi.CameraGrabber_StopLive(m_Grabber[1]);
        }

        private void buttonSnap2_Click(object sender, EventArgs e)
        {
            if (m_Grabber[1] != IntPtr.Zero)
                MvApi.CameraGrabber_SaveImageAsync(m_Grabber[1]);
        }

        private void buttonSettings3_Click(object sender, EventArgs e)
        {
            if (m_Grabber[2] != IntPtr.Zero)
                MvApi.CameraShowSettingPage(m_hCamera[2], 1);
        }

        private void buttonPlay3_Click(object sender, EventArgs e)
        {
            if (m_Grabber[2] != IntPtr.Zero)
                MvApi.CameraSetTriggerMode(m_hCamera[2], 0);
            MvApi.CameraGrabber_StartLive(m_Grabber[2]);
        }

        private void buttonStop3_Click(object sender, EventArgs e)
        {
            if (m_Grabber[2] != IntPtr.Zero)
                MvApi.CameraGrabber_StopLive(m_Grabber[2]);
        }

        private void buttonSnap3_Click(object sender, EventArgs e)
        {
            if (m_Grabber[2] != IntPtr.Zero)
                MvApi.CameraGrabber_SaveImageAsync(m_Grabber[2]);
        }

        private void buttonSettings4_Click(object sender, EventArgs e)
        {
            if (m_Grabber[3] != IntPtr.Zero)
                MvApi.CameraShowSettingPage(m_hCamera[3], 1);
        }

        private void buttonPlay4_Click(object sender, EventArgs e)
        {
            if (m_Grabber[3] != IntPtr.Zero)
                MvApi.CameraSetTriggerMode(m_hCamera[3], 0);
            MvApi.CameraGrabber_StartLive(m_Grabber[3]);
        }

        private void buttonStop4_Click(object sender, EventArgs e)
        {
            if (m_Grabber[3] != IntPtr.Zero)
                MvApi.CameraGrabber_StopLive(m_Grabber[3]);
        }

        private void buttonSnap4_Click(object sender, EventArgs e)
        {
            if (m_Grabber[3] != IntPtr.Zero)
                MvApi.CameraGrabber_SaveImageAsync(m_Grabber[3]);
        }

        private void timer1_Tick(object sender, EventArgs e)
        {
            Label[] Labels = { label1, label2, label3, label4, label5, label6 };
            for (int i = 0; i < 6; ++i)
            {
                if (m_Grabber[i] != IntPtr.Zero)
                {
                    tSdkGrabberStat stat;
                    MvApi.CameraGrabber_GetStat(m_Grabber[i], out stat);
                    string info = String.Format("| Size:{0}*{1} | DispFPS:{2} | CapFPS:{3} |",
                        stat.Width, stat.Height, stat.DispFps, stat.CapFps);
                    Labels[i].Text = info;
                }
            }
        }
        private void SoftTrigger(int a)
        {
            if (m_Grabber[a] != IntPtr.Zero)
            {
                MvApi.CameraSoftTrigger(m_hCamera[a]);
                //if (MvApi.CameraGrabber_SaveImage(m_Grabber[a], out Image, 2000) == CameraSdkStatus.CAMERA_STATUS_SUCCESS)
                //{
                //    countimage[0]++;
                //    string filename = System.IO.Path.Combine(
                //            textBox2.Text.ToString(),
                //            string.Format("{0}.bmp", "Camera1_image_" + countimage[0] + counttimer[0]));

                //    MvApi.CameraImage_SaveAsBmp(Image, filename);
                //    MvApi.CameraImage_Destroy(Image);
                //    timer2.Enabled = true;
                //}
                //else
                //{
                //    MessageBox.Show("llll");
                //}
                //Bitmap bitmap = new Bitmap(DispWnd1.Image);
                MvApi.CameraGrabber_SaveImageAsync(m_Grabber[a]);
            }
        }
        int[] counttimer = new int[6];
        bool enable;
        private void timer2_Tick(object sender, EventArgs e)
        {
            if (!enable)
            {
                enable = true;
                counttimer[0]++;
                SoftTrigger(0);
            }

        }

        private void button3_Click(object sender, EventArgs e)
        {
            for (int i = 0; i < 6; i++)
            {
                MvApi.CameraSetTriggerMode(m_hCamera[i], 1);
            }
            if (timer2.Enabled == true)
            {
                timer2.Enabled = false;
            }
            else
            {
                timer2.Enabled = true;
            }
        }
        private void Trigger1_Click(object sender, EventArgs e)
        {
            MvApi.CameraSetTriggerMode(m_hCamera[0], 1);
            SoftTrigger(0);
        }

        private void Trigger2_Click(object sender, EventArgs e)
        {
            MvApi.CameraSetTriggerMode(m_hCamera[1], 1);
            SoftTrigger(1);
        }

        private void Trigger5_Click(object sender, EventArgs e)
        {
            MvApi.CameraSetTriggerMode(m_hCamera[4], 1);
            SoftTrigger(5);
        }

        private void Trigger3_Click(object sender, EventArgs e)
        {
            MvApi.CameraSetTriggerMode(m_hCamera[2], 1);
            SoftTrigger(3);
        }

        private void Trigger4_Click(object sender, EventArgs e)
        {
            MvApi.CameraSetTriggerMode(m_hCamera[3], 1);
            SoftTrigger(4);
        }

        private void Trigger6_Click(object sender, EventArgs e)
        {
            MvApi.CameraSetTriggerMode(m_hCamera[5], 1);
            SoftTrigger(6);
        }

        private void buttonSettings5_Click(object sender, EventArgs e)
        {
            if (m_Grabber[4] != IntPtr.Zero)
                MvApi.CameraShowSettingPage(m_hCamera[4], 1);
        }

        private void buttonPlay5_Click(object sender, EventArgs e)
        {
            if (m_Grabber[4] != IntPtr.Zero)
                MvApi.CameraSetTriggerMode(m_hCamera[4], 0);
            MvApi.CameraGrabber_StartLive(m_Grabber[4]);
        }

        private void buttonStop5_Click(object sender, EventArgs e)
        {
            if (m_Grabber[4] != IntPtr.Zero)
                MvApi.CameraGrabber_StopLive(m_Grabber[4]);
        }

        private void buttonSnap5_Click(object sender, EventArgs e)
        {
            if (m_Grabber[4] != IntPtr.Zero)
                MvApi.CameraGrabber_SaveImageAsync(m_Grabber[4]);
        }

        private void buttonSettings6_Click(object sender, EventArgs e)
        {
            if (m_Grabber[5] != IntPtr.Zero)
                MvApi.CameraShowSettingPage(m_hCamera[5], 1);
        }

        private void buttonPlay6_Click(object sender, EventArgs e)
        {
            if (m_Grabber[5] != IntPtr.Zero)
                MvApi.CameraSetTriggerMode(m_hCamera[5], 0);
            MvApi.CameraGrabber_StartLive(m_Grabber[5]);
        }

        private void buttonStop6_Click(object sender, EventArgs e)
        {
            if (m_Grabber[5] != IntPtr.Zero)
                MvApi.CameraGrabber_StopLive(m_Grabber[5]);
        }
        private void buttonSnap6_Click(object sender, EventArgs e)
        {
            if (m_Grabber[5] != IntPtr.Zero)
                MvApi.CameraGrabber_StopLive(m_Grabber[5]);
        }

        private void timertrigger_Click(object sender, EventArgs e)
        {
            if (textBox1 != null)
            {
                if (Convert.ToInt32(textBox1.Text) > 0)
                {
                    timer2.Interval = Convert.ToInt32(textBox1.Text);

                }
                else
                {
                    MessageBox.Show("eror");
                }
            }

        }

        int[] countshowimage = new int[6];

        string[] filename = new string[6];
        string[] filenamedelete = new string[6];
        FileInfo[] file = new FileInfo[6];
        FileInfo[] filedelete = new FileInfo[6];
        string[] namecamera = new string[6] { "Camera1_image_", "Camera2_image_", "Camera3_image_", "Camera4_image_", "Camera5_image_", "Camera6_image_" };
        private void button1_Click(object sender, EventArgs e)
        {
            PictureBox[] pictureBoxes = { DispWnd1, DispWnd2, DispWnd3, DispWnd4, DispWnd5, DispWnd6 };

            for (int i = 0; i < countshowimage.Length; i++)
            {
                pictureBoxes[i].Image = null;
                countshowimage[i]++;

                // xóa ảnh trước đó

                filenamedelete[i] = System.IO.Path.Combine(
                 textBox2.Text.ToString(),
                 string.Format("{0}.bmp", namecamera[i] + (countshowimage[i] - 1) + "_" + (countshowimage[i] - 1)));
                filedelete[i] = new FileInfo(filenamedelete[i]);
                if (filedelete[i].Exists.Equals(true)) // kiểm tra sự tồn tại của ảnh
                {

                    File.Delete(filenamedelete[i]);
                }

                // hiển thị ảnh trước đó
                filename[i] = System.IO.Path.Combine(
                       textBox2.Text.ToString(),
                       string.Format("{0}.bmp", namecamera[i] + countshowimage[i] + "_" + countshowimage[i]));
                file[i] = new FileInfo(filename[i]);
                if (file[i].Exists.Equals(true)) // kiểm tra sự tồn tại của ảnh
                {
                    pictureBoxes[i].Image = new Bitmap(filename[i]);

                }
                else
                {
                    pictureBoxes[i].Image = null;
                }


            }



        }
    }
}
