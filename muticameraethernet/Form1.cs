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
using Emgu.CV.Structure;
using Emgu.CV;
using static System.Windows.Forms.VisualStyles.VisualStyleElement;
using System.Drawing.Imaging;
using Emgu.CV.Util;
using OpenTK;
using System.Linq;
using static System.Net.Mime.MediaTypeNames;
using EasyModbus;
using Emgu.CV.Features2D;
using PID_Fill;
using Emgu.CV.CvEnum;
using System.Diagnostics;
using System.Windows.Forms.DataVisualization.Charting;

namespace FirstStepMulti
{
    public partial class Form1 : Form
    {
        #region variable
        Image<Bgr, byte> ImgInput;
        Image<Bgr, byte> ImgInput1;
        Image<Bgr, byte> ImgInput2;
        Image<Bgr, byte> ImgRoi;
        Image<Gray, byte> _imgCanny;
        //Image<Bgr, byte> _imgCanny;

        double perimeter;
        VectorOfVectorOfPoint contours = new VectorOfVectorOfPoint();
        Rectangle roi;

        Mat m = new Mat();
        int dem, dem1;
        int max, max1, max2, max3, max5, max6, max7;
        int min;
        int minx;
        int[] mintest = new int[500];
        bool find_line = false;
        int countfild_line;
        bool testmin;
        bool Error_PLC = false;
        protected tSdkImageResolution tResolution;
        int[] toadox = new int[1000];
        int[] toadoy = new int[1000];
        int[] toadox1 = new int[1000];
        int[] toadoy1 = new int[1000];
        bool[] M20 = new bool[1];
        bool Write_D260;
        int D260;
        int lastD260;
        Bitmap image;
        protected IntPtr[] m_Grabber = new IntPtr[6];
        protected CameraHandle[] m_hCamera = new CameraHandle[6];
        protected tSdkCameraDevInfo[] m_DevInfo;
        tSdkFrameHead pFrameHead;
        protected pfnCameraGrabberFrameCallback m_FrameCallback;
        protected pfnCameraGrabberSaveImageComplete[] m_SaveImageComplete = new pfnCameraGrabberSaveImageComplete[6];
        BackgroundWorker m_BackgroundWorker;
        BackgroundWorker m_BackgroundWorker1;
        #endregion
        int[] countimage = new int[6];
        ModbusClient modbusClient = new ModbusClient();
        PIDCaculater iDCaculater = new PIDCaculater(50);
        DataTable dt = new DataTable();
        DataTable dt2 = new DataTable();
        public Form1()
        {
            InitializeComponent();
            chart1.Series.Add("Water Level");
            dt.Columns.Add("T", typeof(int));
            dt.Columns.Add("H", typeof(int));
            chart1.Series["Water Level"].XValueMember = "T";
            chart1.Series["Water Level"].YValueMembers = "H";
            chart1.Series["Water Level"].IsValueShownAsLabel = false;
            chart1.Series["Water Level"].BorderWidth = 3;
            chart1.Series["Water Level"].ChartType = SeriesChartType.Line;
            chart1.Series["Water Level"].Points.DataBind(dt.DefaultView, "T", "H", "");

            ChartArea chartArea = new ChartArea("ChartArea");
            chart2.Series.Add("Servo");

            dt2.Columns.Add("T", typeof(int));
            dt2.Columns.Add("P", typeof(int));
            chart2.Series["Servo"].XValueMember = "T";
            chart2.Series["Servo"].YValueMembers = "P";
            chart2.Series["Servo"].IsValueShownAsLabel = false;
            chart2.Series["Servo"].BorderWidth = 3;
            chart2.Series["Servo"].ChartType = SeriesChartType.Line;
            chart2.Series["Servo"].Points.DataBind(dt2.DefaultView, "T", "P", "");
            iDCaculater.Kp = 0.3;
            iDCaculater.Ki = 3;
            iDCaculater.Kd = 0;
            iDCaculater.Water_Level_Max = 300;
            iDCaculater.Water_Level_Min = 23;
            iDCaculater.Possition_Max = Convert.ToDouble(textBox1.Text);
            iDCaculater.Position_Min = Convert.ToDouble(textBox2.Text);
            iDCaculater.Water_Level_Target = Convert.ToDouble(textBox3.Text);
            iDCaculater.Dt = Convert.ToInt32(textBox4.Text);
            iDCaculater.Limit_Position = 10000;
            modbusClient.IPAddress = "192.168.1.200";
            modbusClient.Parity = System.IO.Ports.Parity.None;
            modbusClient.StopBits = System.IO.Ports.StopBits.One;
            modbusClient.Port = 502;
            //if (modbusClient.Connected) { modbusClient.Disconnect(); }
            modbusClient.Connect();
            timer1.Start();
            label2.Text = "0";
            label10.Text = "Initialize...";
            label12.Text = "0.000 sec";
            TimeSpan time = new TimeSpan(0, 0, 0, 2, 567);
            label12.Text = time.ToString("ss\\.fff") + " sec";
            trackBar1.Value = 117;
            trackBar2.Value = 244;
            Range_max = Convert.ToInt32(textBox1.Text);
            Range_min = Convert.ToInt32(textBox2.Text);
            m_BackgroundWorker = new BackgroundWorker();
            //m_BackgroundWorker1 = new BackgroundWorker();
            //m_BackgroundWorker1.DoWork += M_BackgroundWorker1_DoWork;
            //m_BackgroundWorker1.RunWorkerCompleted += M_BackgroundWorker1_RunWorkerCompleted;
            //m_BackgroundWorker1.WorkerSupportsCancellation = true;
            m_BackgroundWorker.DoWork += M_BackgroundWorker_DoWork;
            m_BackgroundWorker.RunWorkerCompleted += M_BackgroundWorker_RunWorkerCompleted;
            m_BackgroundWorker.WorkerSupportsCancellation = true;
            if (modbusClient.Connected)
            {
                m_BackgroundWorker.RunWorkerAsync();
            }
            m_FrameCallback = new pfnCameraGrabberFrameCallback(CameraGrabberFrameCallback);
            m_SaveImageComplete[0] = new pfnCameraGrabberSaveImageComplete(CameraGrabberSaveImageComplete);

            MvApi.CameraEnumerateDevice(out m_DevInfo);
            int NumDev = (m_DevInfo != null ? Math.Min(m_DevInfo.Length, 1) : 0);

            IntPtr[] hDispWnds = { this.DispWnd1.Handle };
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

                    // MvApi.CameraGrabber_SetHWnd(m_Grabber[i], hDispWnds[i]);
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
        private Stopwatch stopwatch = new Stopwatch();
        private void M_BackgroundWorker_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {

            if (!m_BackgroundWorker.IsBusy)
            {
                m_BackgroundWorker.RunWorkerAsync();

            }
            //MethodInvoker method1 = delegate { label11.Text = "******"; }; this.Invoke(method1);
        }
        private bool M22 = false;
        private bool Get_max = false;
        private int Home_p = 0;
        private void M_BackgroundWorker_DoWork(object sender, DoWorkEventArgs e)
        {
            if (m_BackgroundWorker.CancellationPending)
            {
                e.Cancel = true;
                Error_PLC = true;
                MethodInvoker method = delegate { button3.Visible = true; }; this.Invoke(method);
            }
            else
            {
                try
                {
                    if (modbusClient.Connected == true)
                    {
                        try
                        {
                            M20 = modbusClient.ReadCoils(8212, 3);
                        }
                        catch (Exception ex)
                        {
                            MethodInvoker method = delegate { label11.Text = ex.Message; }; this.Invoke(method);
                            m_BackgroundWorker.CancelAsync();
                        }

                        if (Found_line != true)
                        {
                            try
                            {
                                var M_get_max = modbusClient.ReadCoils(8238, 1)[0]; // Biến M46
                                if (M_get_max)
                                {
                                    Get_max = true;

                                    modbusClient.WriteSingleCoil(8239, true);
                                    MethodInvoker method = delegate { label2.Text = "Finding Line...";
                                        if (pictureBox1.Image != null) pictureBox1.Image.Dispose();
                                        dt.Rows.Clear();
                                        dt2.Rows.Clear();
                                        chart1.Series["Water Level"].Points.DataBind(dt.DefaultView, "T", "H", "");
                                        chart2.Series["Servo"].Points.DataBind(dt2.DefaultView, "T", "P", "");
                                    }; this.Invoke(method);
                                    MethodInvoker method2 = delegate { label12.Text = "0.000 sec"; }; this.Invoke(method2);
                                }
                            }
                            catch (Exception ex)
                            {
                                MethodInvoker method = delegate { label11.Text = ex.Message; }; this.Invoke(method);
                                m_BackgroundWorker.CancelAsync();
                            }

                        }
                        if (M20[0] == true)
                        {

                            try
                            {
                                if (!Found_line)
                                {
                                    //find_line = true;
                                    stopwatch = new Stopwatch();

                                    stopwatch.Start();
                                    Found_line = true;
                                    Write_D260 = false;
                                    //D260 = 0;
                                    lastD260 = 300;
                                    //Ref_Line = 0;

                                    modbusClient.WriteSingleCoil(8213, true); // M21

                                }
                            }
                            catch (Exception ex)
                            {

                                MethodInvoker method = delegate { label11.Text = ex.Message; }; this.Invoke(method);
                                m_BackgroundWorker.CancelAsync();
                            }


                        }

                        if (M20[2] == true)
                        {

                            Ref_Line = 0;
                            Home_p = modbusClient.ReadHoldingRegisters(360, 1)[0];
                            iDCaculater.Possition_Max = Home_p + Range_max;
                            iDCaculater.Position_Min = Home_p + Range_min;
                            modbusClient.WriteSingleCoil(8214, false); //M22


                            iDCaculater.Possition_Output = (int)iDCaculater.Possition_Max;
                            MethodInvoker method = delegate
                            {
                                textBox5.Text = Home_p.ToString();
                                textBox6.Text = iDCaculater.Possition_Max.ToString();
                                textBox7.Text = iDCaculater.Position_Min.ToString();
                                chart2.ChartAreas[0].AxisY.Minimum = iDCaculater.Position_Min;


                            }; this.Invoke(method);
                        }
                        if (Write_D260)
                        {
                            modbusClient.WriteSingleRegister(260, (Int16)D260);

                        }
                        if (StopPID)
                        {
                           
                            Ref_Line = 0;
                            iDCaculater.Water_Level_Current = iDCaculater.Water_Level_Max;
                            modbusClient.WriteSingleCoil(8215, true); // M23
                            if (iDCaculater.Started) iDCaculater.Stop_Caculate();
                            MethodInvoker method = delegate
                            {
                                label2.Text = "Finished";
                            }; this.Invoke(method);
                            StopPID = false;
                            Write_D260 = false;
                            stopwatch.Stop();
                            TimeSpan timeSpan = TimeSpan.FromMilliseconds(stopwatch.ElapsedMilliseconds);
                            MethodInvoker method2 = delegate
                            {
                                label12.Text = timeSpan.ToString("ss\\.fff") + " sec";
                                

                            }; this.Invoke(method2);

                        }
                        MethodInvoker method1 = delegate { label11.Text = "Reading PLC"; }; this.Invoke(method1);

                    }
                }
                catch (Exception ex)
                {
                    //MethodInvoker method = delegate { label11.Text = ex.InnerException.ToString(); }; this.Invoke(method);
                    //m_BackgroundWorker.CancelAsync();
                    Console.WriteLine(ex.Message);
                }

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
        private bool StopPID = false;
        private bool test_cam = false;
        private bool Found_line = false;
        private int Ref_Line = 0;
        private int count = 0;
        private bool Aprox = false;
        private int count_check = 0;
        private int plot = 0;
        private Image<Bgr, byte> TrackerImage;
        private void CameraGrabberFrameCallback(
            IntPtr Grabber,
            IntPtr pFrameBuffer,
            ref tSdkFrameHead pFrameHead,
            IntPtr Context)
        {
            try
            {
                image = (Bitmap)MvApi.CSharpImageFromFrame(pFrameBuffer, ref pFrameHead);
                ImgInput = new Image<Bgr, byte>(image);
                ImgInput2 = new Image<Bgr, byte>(image);
                roi = new Rectangle(740, 300, 320, 420);
                ImgInput2.ROI = roi;
                ImgRoi = ImgInput2.Copy();
                ImgInput.Draw(roi, new Bgr(255, 0, 0), 4);
                //_imgCanny = new Image<Gray, byte>(ImgRoi.Width, ImgRoi.Height, new Gray(0));
                //_imgCanny = new Image<Bgr, byte>(ImgRoi.Width, ImgRoi.Height,new Bgr(255,100,255));


                Mat blurredImage = new Mat();

                Size kernelSize = new Size(25, 25);
                double sigmaX = 3.0;
                double sigmaY = 3.0;

                CvInvoke.GaussianBlur(ImgRoi, blurredImage, kernelSize, sigmaX, sigmaY);

                Mat gray = new Mat();
                CvInvoke.CvtColor(blurredImage, gray, ColorConversion.Bgr2Gray);
                Mat binary = new Mat();
                CvInvoke.Threshold(gray, binary, 0, 50, ThresholdType.Binary | ThresholdType.Otsu);
                Mat edges = new Mat();
                //Cv2.Canny(binary, edges, Convert.ToInt32(textBox3.Text), Convert.ToInt32(textBox4.Text));
                CvInvoke.Canny(binary, edges, 0, 100);

                Mat kernel = CvInvoke.GetStructuringElement(ElementShape.Cross, new Size(5, 5), new Point(-1, -1));
                Mat dilated = new Mat();
                CvInvoke.Dilate(edges, dilated, kernel, new Point(-1, -1), 1, BorderType.Default, new MCvScalar());
                //_imgCanny = ImgRoi.Canny(117, 244);
                _imgCanny = dilated.ToImage<Gray, byte>();

                //CvInvoke.FindContours(_imgCanny, contours, m, Emgu.CV.CvEnum.RetrType.External, Emgu.CV.CvEnum.ChainApproxMethod.ChainApproxSimple);
                CvInvoke.FindContours(dilated, contours, m, Emgu.CV.CvEnum.RetrType.External, Emgu.CV.CvEnum.ChainApproxMethod.ChainApproxSimple);

                for (int i = 0; i < contours.Size; i++)
                {

                    perimeter = CvInvoke.ArcLength(contours[i], true);
                    if (perimeter > 150)
                    {
                        //CvInvoke.DrawContours(_imgCanny, contours, i, new MCvScalar(255, 255, 0), 5);
                        //CvInvoke.DrawContours(ImgRoi, contours, i, new MCvScalar(255, 255, 0), 5);

                    }

                }
                //dem = dem1 = max1 = max2 = max3 = 0;
                dem = dem1 = max1 = max3 = 0;

                bool tesst = true;
                for (int k = 0; k < _imgCanny.Width; k++)//backup _i
                {
                    for (int l = 0; l < _imgCanny.Height; l++)
                    {
                        if (_imgCanny.Data[l, k, 0] == 255)
                        {
                            if (tesst)
                            {
                                toadoy[dem] = l;
                                toadox[dem] = k;
                                dem++;

                            }
                            else
                            {
                                if (toadoy[dem - 1] - l < 10)
                                {
                                    toadoy[dem] = l;
                                    toadox[dem] = k;
                                    dem++;
                                    break;
                                }
                            }
                            tesst = false;

                        }
                    }
                }

                if (testmin == false)
                {

                    if (toadoy.Length > 0)
                    {
                        min = toadoy.Where(num => num > 5).Min();
                        

                    }
                }

                if (find_line)
                {
                    MethodInvoker method = delegate { label2.Text = "Re-Initialize"; }; this.Invoke(method);

                    if (mintest[50] == 0)
                    {
                        mintest[countfild_line] = min;
                        countfild_line++;

                    }
                    if (mintest[50] > 0)
                    {
                        for (int i = 0; i < 50; i++)
                        {
                            max = mintest[i];
                            for (int j = 1; j < 50; j++)
                            {
                                if (max <= mintest[j] + 5 || max >= mintest[j] - 5)
                                {
                                    max2++;
                                    if (max2 > max1)
                                    {
                                        max3 = mintest[j];
                                        max1 = max2;
                                    }
                                }
                            }
                            max2 = 0;
                        }
                        min = max3;
                        max = max1 = max2 = max3 = 0;
                        testmin = true;

                        find_line = false;
                        Found_line = true;



                    }
                }
                else
                {
                    //LineSegment2D line1 = new LineSegment2D(new Point(0, min), new Point(800, min));
                    //_imgCanny.Draw(line1, new Gray(255), 5);
                    //DispWnd5.Image = _imgCanny.Bitmap;
                    //find_line = false;
                    //Found_line = true;
                    if (Found_line)
                    {

                        if (iDCaculater.Started == false)
                        {
                            Thread thread = new Thread(() =>
                            {
                                iDCaculater.Start_Caculate();

                            });
                            if (thread.IsAlive == false)
                            {
                                thread.Start();
                            }
                        }

                    }
                    if (min < roi.Height / 2 && Ref_Line == 0 && Get_max && min !=0)
                    {
                        plot++;
                        if(plot >= 10) 
                        {
                            Ref_Line = toadoy.Where(num => Array.IndexOf(toadoy, num) > toadoy.Length - (toadoy.Length - 100) && Array.IndexOf(toadoy, num) < toadoy.Length - 100 && num != 0).Min();
                            //Ref_Line = min;
                            Get_max = false;
                            plot = 0;
                        }
                       

                    }

                    for (int k = 0; k < _imgCanny.Width; k++)
                    {
                        for (int l = _imgCanny.Height - 1; l > min; l--)
                        {
                            if (_imgCanny.Data[l, k, 0] == 255)
                            {
                                toadox1[dem1] = k;
                                toadoy1[dem1] = l;
                                dem1++;
                                break;
                            }

                        }
                    }
                    //for (int i = 0; i < dem1; i++)
                    //{
                    //    max = toadoy1[i];
                    //    if (toadox1[i] == minx )
                    //    {
                    //        for (int j = i - 1; j <= i + 1; j++)
                    //        {
                    //            if (j <= dem && max == toadoy1[j])
                    //            {
                    //                max2++;
                    //                if (max2 > max1)
                    //                {
                    //                    max3 = toadoy1[j];
                    //                    max1 = max2;
                    //                }
                    //            }
                    //            else 
                    //            {
                    //                max3 = (int)toadoy1.Average();
                    //            }

                    //        }
                    //        max2 = 0;
                    //        break;
                    //    }

                    //}
                    try
                    {
                        if (toadoy1.Length > 0) 
                        {
                            int deviationThreshold = 10;
                            double average = 0;
                            if (Ref_Line != 0) 
                            {
                               average = toadoy1.Where(num => num >= Ref_Line).Average();
                            }
                            else
                            {
                                average = toadoy1.Where(num => num > min).Average();
                            }
                            //int[] filteredElements = toadoy1.Where(num => Math.Abs(num - average) <= deviationThreshold).ToArray();

                            List<int> currentSequence = new List<int>();
                            List<List<int>> filteredGroups = new List<List<int>>();

                            foreach (int num in toadoy1)
                            {
                                if (currentSequence.Count > 0 && Math.Abs(num - currentSequence.Last()) <= deviationThreshold && num < average)
                                {
                                    currentSequence.Add(num);
                                }
                                else
                                {
                                    if (currentSequence.Count > 1)
                                    {
                                        filteredGroups.Add(currentSequence);
                                    }
                                    currentSequence = new List<int> { num };
                                }
                            }

                            if (currentSequence.Count > 1)
                            {
                                filteredGroups.Add(currentSequence);
                            }




                            if (filteredGroups.Count > 0) 
                            {
                                var groupWithMaxCount = filteredGroups.Where(group => group.Count < 700 && group.Count>10).OrderBy(group => group.Count).LastOrDefault();
                                if(groupWithMaxCount.Max()==0) groupWithMaxCount = filteredGroups.Where(group => group.Count < 700 && group.Count>10).OrderBy(group => group.Count).ElementAt(filteredGroups.Count-2);
                                if (groupWithMaxCount != null)
                                {
                                    double targetAverage = groupWithMaxCount.Min();
                                    double targetAverage1 = groupWithMaxCount.Max();
                                    double avg = (targetAverage + targetAverage1) / 2;
                                    //max3 = (int)targetAverage;
                                    if((int)avg != 0) 
                                    {
                                        max3 = (int)avg;

                                        max2 = max3;
                                    }
                                    else 
                                    {
                                        max3 = max2;
                                    }
                                   
                                    Console.WriteLine(max3);
                                    if (Ref_Line != 0)
                                    {
                                        MethodInvoker method1 = delegate
                                        {
                                            label13.Text = string.Format("max:{0}-Ref:{1}-min:{2}", max3.ToString(), Ref_Line.ToString(), min.ToString());
                                            label14.Text = string.Format("Edge Group:{0}", filteredGroups.Count);
                                        }; this.Invoke(method1);

                                    }
                                    else
                                    {
                                        MethodInvoker method1 = delegate
                                        {
                                            label13.Text = string.Format("max:{0}-min:{1}-high:{2}", max3.ToString(), min.ToString(), (max3 - min).ToString());
                                            label14.Text = string.Format("Edge Group:{0}", filteredGroups.Count);

                                        }; this.Invoke(method1);

                                    }
                                }
                                else 
                                {
                                    max3 = max2;
                                }
                               
                            }

                           
                        }
                        else 
                        {
                            max3 = max2;
                        }
                       
                    }
                    catch (Exception ex )
                    {
                        Console.WriteLine("AVR  "+ ex.Message);
                    }
                    if (max3 == 0) max3 = max2;
                    //LineSegment2D line2 = new LineSegment2D(new Point(0, max3), new Point(800, max3));
                    //_imgCanny.Draw(line2, new Gray(255), 5);

                    //if (Get_max)
                    //{
                    //    Get_max = false;

                    //}
                    if (Ref_Line != 0)
                    {
                        LineSegment2D line_lim = new LineSegment2D(new Point(roi.X, Ref_Line + 300), new Point(roi.X + roi.Width, Ref_Line + 300));
                        ImgInput.Draw(line_lim, new Bgr(0, 255, 0), 6);
                        LineSegment2D line_lim1 = new LineSegment2D(new Point(roi.X, max3 + 300), new Point(roi.X + roi.Width, max3 + 300));
                        ImgInput.Draw(line_lim1, new Bgr(255, 0, 255), 8);
                        LineSegment2D line3 = new LineSegment2D(new Point(900, Ref_Line + 300), new Point(900, max3 + 300));
                        ImgInput.Draw(line3, new Bgr(0, 255, 255), 4);
                        
                        
                    }
                    else
                    {
                        LineSegment2D line3 = new LineSegment2D(new Point(900, min + 300), new Point(900, max3 + 300));
                        ImgInput.Draw(line3, new Bgr(0, 0, 255), 2);
                        LineSegment2D line_lim1 = new LineSegment2D(new Point(roi.X, max3 + 300), new Point(roi.X + roi.Width, max3 + 300));
                        ImgInput.Draw(line_lim1, new Bgr(255, 0, 255), 8);
                    }


                    MethodInvoker method = delegate
                    {
                        DispWnd5.Image = _imgCanny.Bitmap;

                        //DispWnd5.Image = dilated.Bitmap;
                        DispWnd1.Image = ImgInput.Bitmap;
                        if (Ref_Line != 0)
                        {
                            label1.Text = (max3 - Ref_Line).ToString();
                            if (dt.Rows.Count > 300) dt.Rows.Remove(dt.Rows[0]);
                            dt.Rows.Add(count, max3 - Ref_Line);
                            if (dt2.Rows.Count > 300) dt2.Rows.Remove(dt2.Rows[0]);
                            dt2.Rows.Add(count, iDCaculater.Possition_Output);
                            chart1.Series["Water Level"].Points.DataBind(dt.DefaultView, "T", "H", "");
                            chart2.Series["Servo"].Points.DataBind(dt2.DefaultView, "T", "P", "");
                            count++;
                        }
                        else
                        {
                            label1.Text = (max3 - min).ToString();

                        }
                        //label1.Text = (max3 - min).ToString();

                    };
                    this.Invoke(method);
                    if (max3 - Ref_Line <= 37 && Found_line)
                    {
                        count_check++;
                        if (count_check > 10)
                        {
                            Aprox = true;
                        }

                    }
                    else
                    {
                        count_check = 0;
                    }
                    if (Ref_Line == 0 && max3 - min <= 300 && Found_line && test_cam == false)
                    {
                        if (lastD260 - (max3 - min) < 50)
                        {

                        }
                        //iDCaculater.Water_Level_Current = max3 - max1;
                        //D260 = iDCaculater.Possition_Output;
                        //lastD260 = max3 - max1;
                        //Write_D260 = true;
                        var a = DateTime.Now;
                        if (a.Second % 2 == 0)
                        {
                            MethodInvoker invoker = delegate { label2.Text = "Finding Line...."; }; this.Invoke(invoker);
                        }
                        else
                        {
                            MethodInvoker invoker = delegate { label2.Text = "Finding Line.."; }; this.Invoke(invoker);

                        }


                    }
                    else if (Ref_Line != 0 && max3 - Ref_Line <= 300 && max3 - Ref_Line >= iDCaculater.Water_Level_Target && Found_line && test_cam == false)
                    {

                        iDCaculater.Water_Level_Current = max3 - Ref_Line;
                        D260 = (Int16)iDCaculater.Possition_Output;
                        lastD260 = max3 - max1;
                        Write_D260 = true;
                        if (max3 - Ref_Line == iDCaculater.Water_Level_Target)
                        {
                            StopPID = true;
                            CapFinish(ImgInput.Bitmap, _imgCanny.Bitmap, iDCaculater.Water_Level_Target,Ref_Line,"OK");

                            Found_line = false;
                            D260 = (Int16)iDCaculater.Position_Min;
                            iDCaculater.Water_Level_Current = iDCaculater.Water_Level_Max;
                            //find_line = false;
                            MethodInvoker method1 = delegate
                            {
                                label2.Text = "0";
                            }; this.Invoke(method);
                        }
                        else if (max3 - Ref_Line <= iDCaculater.Water_Level_Target + 2 && max3 - Ref_Line>iDCaculater.Water_Level_Target && Ref_Line - min >= 5)
                        {
                            StopPID = true;
                            CapFinish(ImgInput.Bitmap,_imgCanny.Bitmap, max3 - Ref_Line,Ref_Line, "R-M >= 5");
                            Found_line = false;
                            D260 = (Int16)iDCaculater.Position_Min;

                            iDCaculater.Water_Level_Current = iDCaculater.Water_Level_Max;
                            //find_line = false;
                            MethodInvoker method1 = delegate
                            {
                                label2.Text = "0";
                            }; this.Invoke(method);
                        }
                       else if(max3 - Ref_Line <= iDCaculater.Water_Level_Target + 10 && max3 - Ref_Line > iDCaculater.Water_Level_Target && Ref_Line - min >= 10) 
                        {
                            StopPID = true;
                            CapFinish(ImgInput.Bitmap, _imgCanny.Bitmap, max3 - Ref_Line, Ref_Line,"R-M>10");
                            Found_line = false;
                            D260 = (Int16)iDCaculater.Position_Min;

                            iDCaculater.Water_Level_Current = iDCaculater.Water_Level_Max;
                            //find_line = false;
                            MethodInvoker method1 = delegate
                            {
                                label2.Text = "0";
                            }; this.Invoke(method);
                        }
                        MethodInvoker invoker = delegate { label2.Text = D260.ToString(); }; this.Invoke(invoker);

                    }
                    else if (Ref_Line != 0 && max3 - Ref_Line <= 300 && max3 - Ref_Line <= iDCaculater.Water_Level_Target && Found_line && test_cam == false)
                    {
                        StopPID = true;
                        CapFinish(ImgInput.Bitmap, _imgCanny.Bitmap, max3 - Ref_Line,Ref_Line,"<Target");
                        Found_line = false;
                        D260 = (Int16)iDCaculater.Position_Min;
                        Write_D260 = true;
                        iDCaculater.Water_Level_Current = iDCaculater.Water_Level_Max;

                    }
                    else if (Ref_Line != 0 && max3 - Ref_Line <= 300 && Aprox && Ref_Line-min>=10 && Found_line && test_cam == false)
                    {
                        StopPID = true;
                        CapFinish(ImgInput.Bitmap, _imgCanny.Bitmap, max3 - Ref_Line, Ref_Line,"NG");
                        Found_line = false;
                        D260 = (Int16)iDCaculater.Position_Min;
                        Write_D260 = true;
                        iDCaculater.Water_Level_Current = iDCaculater.Water_Level_Max;
                        Aprox = false;
                        count_check = 0;

                    }
                    
                }
            }
            catch (Exception ex)
            {
                //MethodInvoker method = delegate { label11.Text = ex.InnerException.ToString(); }; this.Invoke(method);
                Console.WriteLine(ex.Message);
            }

        }
        private void CapFinish(Bitmap bitmap,Bitmap bitmap1, double value,int Ref_L,string msg = "")
        {
            TrackerImage = new Image<Bgr, byte>(bitmap);
            var TrackerImage1 = new Image<Bgr, byte>(bitmap1);
            string text = value.ToString();

            // Set the font type, scale, color, and thickness
            FontFace font = FontFace.HersheySimplex;
            double fontScale = 3.0;
            MCvScalar color = new MCvScalar(0, 200, 255); // Red color
            int thickness = 8;

            // Set the position where the text will be drawn
            Point position = new Point(150, 150);
            Point position1 = new Point(150, 250);
            Point position2 = new Point(150, 350);
            Point position3 = new Point(150, 450);


            // Draw the text on the image
            CvInvoke.PutText(TrackerImage, text, position, font, fontScale, color, thickness, LineType.AntiAlias);
            CvInvoke.PutText(TrackerImage, Ref_L.ToString(), position1, font, fontScale, color, thickness, LineType.AntiAlias);
            CvInvoke.PutText(TrackerImage, min.ToString(), position2, font, fontScale, color, thickness, LineType.AntiAlias);
            if (msg != "") 
            {
                CvInvoke.PutText(TrackerImage, msg, position3, font, fontScale, color, thickness, LineType.AntiAlias);
            }
            try
            {
                MethodInvoker method3 = delegate
                {
                    if (pictureBox1.Image != null) pictureBox1.Image.Dispose();
                    var picture_temp = (Bitmap)TrackerImage.Bitmap.Clone();
                    pictureBox1.Image = picture_temp;
                    if (pictureBox2.Image != null) pictureBox2.Image.Dispose();
                    var picture_temp1 = (Bitmap)TrackerImage1.Bitmap.Clone();
                    pictureBox2.Image = picture_temp1;

                    TrackerImage.Dispose();
                    TrackerImage1.Dispose();

                }; this.Invoke(method3);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);

            }
           
        }
        private void CameraGrabberSaveImageComplete(
            IntPtr Grabber,
            IntPtr Image,
            CameraSdkStatus Status,
            IntPtr Context)
        {

            if (Image != IntPtr.Zero)
            {



            }

            MvApi.CameraImage_Destroy(Image);
            //Rectangle roi = new Rectangle(180, 200, 300, 250);
            //ImgInput2.ROI = roi;
            //imgROI = ImgInput2.Copy();
            //ImgInput1.Draw(roi, new Bgra(0, 0, 255, 255));
            //pictureBox1.Image = ImgInput1.Bitmap;
            //Image<Gray, byte> _imgCanny = new Image<Gray, byte>(imgROI.Width, imgROI.Height, new Gray(0));

            //_imgCanny = imgROI.Canny(trackBar1.Value, trackBar2.Value);
            //pictureBox2.Image = _imgCanny.Bitmap;

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


            tSdkGrabberStat stat;
            MvApi.CameraGrabber_GetStat(m_Grabber[0], out stat);
            string info = String.Format("| Size:{0}*{1}|| DispFps:{2} || CapFPS:{3} |",
                stat.Width, stat.Height, stat.DispFps, stat.CapFps);
            label10.Text = info;


        }
        private void SoftTrigger(int a)
        {
            if (m_Grabber[a] != IntPtr.Zero)
            {
                MvApi.CameraSetTriggerMode(m_hCamera[0], 1);
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
                SoftTrigger(0);
                enable = true;
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

        private void button2_Click(object sender, EventArgs e)
        {
            countfild_line = 0;
            find_line = true;
        }

        private void Form1_Load(object sender, EventArgs e)
        {

        }

        private void button3_Click_1(object sender, EventArgs e)
        {
            //try
            //{
            //    iDCaculater.Possition_Max = Convert.ToDouble(textBox1.Text);
            //    iDCaculater.Position_Min = Convert.ToDouble(textBox2.Text);
            //    iDCaculater.Water_Level_Target = Convert.ToDouble(textBox3.Text);
            //    iDCaculater.Dt = Convert.ToInt32(textBox4.Text);
            //}
            //catch (Exception)
            //{


            //}
            try
            {
                m_BackgroundWorker.RunWorkerAsync();
                button3.Visible = false;
            }
            catch (Exception ex)
            {

                MessageBox.Show(ex.Message);
            }

        }

        private void textBox3_TextChanged(object sender, EventArgs e)
        {
            iDCaculater.Water_Level_Target = Convert.ToDouble(textBox3.Text);
        }

        private void label3_Click(object sender, EventArgs e)
        {

        }
        private int Range_max = 0;
        private int Range_min = 0;
        private void textBox1_TextChanged(object sender, EventArgs e)
        {
            try
            {
                Range_max = Convert.ToInt32(textBox1.Text);
                iDCaculater.Possition_Max = Home_p + Range_max;
                MethodInvoker method = delegate
                {
                    //textBox5.Text = Home_p.ToString();
                    textBox6.Text = iDCaculater.Possition_Max.ToString();
                    //textBox7.Text = iDCaculater.Position_Min.ToString();

                }; this.Invoke(method);
            }
            catch (Exception)
            {

            }

        }

        private void textBox2_TextChanged(object sender, EventArgs e)
        {
            try
            {
                Range_min = Convert.ToInt32(textBox2.Text);
                iDCaculater.Position_Min = Home_p + Range_min;
                MethodInvoker method = delegate
                {
                    //textBox5.Text = Home_p.ToString();
                    //textBox6.Text = iDCaculater.Possition_Max.ToString();
                    textBox7.Text = iDCaculater.Position_Min.ToString();
                    chart2.ChartAreas[0].AxisY.Minimum = iDCaculater.Position_Min;

                }; this.Invoke(method);
            }
            catch (Exception)
            {

            }

        }

        private void pictureBox1_Click(object sender, EventArgs e)
        {

        }

        private void label13_Click(object sender, EventArgs e)
        {

        }

        private void button4_Click(object sender, EventArgs e)
        {
            Ref_Line = 0;
            iDCaculater.Water_Level_Current = iDCaculater.Water_Level_Max;
            modbusClient.WriteSingleCoil(8215, true); // M23
            if (iDCaculater.Started) iDCaculater.Stop_Caculate();
            MethodInvoker method = delegate
            {
                label2.Text = "Finished";
            }; this.Invoke(method);
            StopPID = false;
            Write_D260 = false;
            stopwatch.Stop();
            TimeSpan timeSpan = TimeSpan.FromMilliseconds(stopwatch.ElapsedMilliseconds);
            MethodInvoker method2 = delegate
            {
                label12.Text = timeSpan.ToString("ss\\.fff") + " sec";


            }; this.Invoke(method2);
        }

        private void button5_Click(object sender, EventArgs e)
        {
            try
            {
                var R = Convert.ToInt32(textBox1.Text);
                R = R - 1;
                MethodInvoker method = delegate
                {
                    textBox1.Text = R.ToString();
                };this.Invoke(method);
                
            }
            catch (Exception)
            {

            }

        }

        private void button6_Click(object sender, EventArgs e)
        {
            try
            {
                var R = Convert.ToInt32(textBox1.Text);
                R = R + 1;
                MethodInvoker method = delegate
                {
                    textBox1.Text = R.ToString();
                }; this.Invoke(method);

            }
            catch (Exception)
            {

            }
        }

        private void button7_Click(object sender, EventArgs e)
        {
            try
            {
                var R = Convert.ToInt32(textBox2.Text);
                R = R - 1;
                MethodInvoker method = delegate
                {
                    textBox2.Text = R.ToString();
                }; this.Invoke(method);

            }
            catch (Exception)
            {

            }
        }

        private void button8_Click(object sender, EventArgs e)
        {
            try
            {
                var R = Convert.ToInt32(textBox2.Text);
                R = R + 1;
                MethodInvoker method = delegate
                {
                    textBox2.Text = R.ToString();
                }; this.Invoke(method);

            }
            catch (Exception)
            {

            }
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


        }

        private void button1_Click(object sender, EventArgs e)
        {


            //timer2.Enabled = true;
            CameraSdkStatus iStatus;
            int x1, x2, y1, y2;

            x1 = 100;
            x2 = 50;
            y1 = 100;
            y2 = 50;
            tResolution.iHOffsetFOV = 648;
            tResolution.iVOffsetFOV = 486;

            if (MvApi.CameraCustomizeResolution(m_hCamera[0], ref tResolution) == CameraSdkStatus.CAMERA_STATUS_SUCCESS)
            {
                MvApi.CameraSetImageResolution(m_hCamera[0], ref tResolution);
            }

        }
    }
}
