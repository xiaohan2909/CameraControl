using System;
using System.IO;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Drawing.Imaging;
using System.Text;
using System.Windows.Forms;
using System.Threading;

using AForge;
using AForge.Video;
using AForge.Video.FFMPEG;
using AForge.Video.DirectShow;
using AForge.Imaging;
using AForge.Imaging.Filters;

namespace Camera
{
    public partial class Form1 : Form
    {
        private FilterInfoCollection videoDevices;
        private VideoCaptureDevice videoSource;

        int seqNum = 0;//序列值(读取自硬盘)
        private bool stopRec = true; //判断是否在录像
        private bool createNewFile = true;        
        VideoFileWriter videoWriter = null; //可以用来保存视频
        private string dirc = System.AppDomain.CurrentDomain.BaseDirectory + "Images"; //截图保存的目录 
        private string vidirc = System.AppDomain.CurrentDomain.BaseDirectory + "Videos"; //录像保存的目录 
        public Form1()
        {
            InitializeComponent();
        }
        //窗口加载
        private void Form1_Load(object sender, EventArgs e)
        {
            if (!Directory.Exists(dirc))
                Directory.CreateDirectory(dirc);
            if (!Directory.Exists(vidirc))
                Directory.CreateDirectory(vidirc);
            if (!Directory.Exists("./Data"))
                Directory.CreateDirectory("./Data");
            seqNum = ReadSeqNum();//读取序列
            seqShow.Text = seqNum.ToString();
            try
            {
                // 枚举所有视频输入设备
                videoDevices = new FilterInfoCollection(FilterCategory.VideoInputDevice);

                if (videoDevices.Count == 0)
                    throw new ApplicationException();

                foreach (FilterInfo device in videoDevices)
                {
                    tscbxCameras.Items.Add(device.Name);
                }

                tscbxCameras.SelectedIndex = 0;
            }
            catch (ApplicationException)
            {
                tscbxCameras.Items.Add("No local capture devices");
                videoDevices = null;
            }
        }
        //点击连接摄像头
        private void toolStripButton1_Click(object sender, EventArgs e)
        {
            CameraConn();
            cutPic.Enabled = true;
            Record.Enabled = true;
            saveData.Enabled = true;
        }
        //连接摄像头
        private void CameraConn()
        {
            videoSource = new VideoCaptureDevice(videoDevices[tscbxCameras.SelectedIndex].MonikerString);
            videoSource.DesiredFrameSize = new Size(640,480);
            videoSource.DesiredFrameRate = 1;

            videPlayer.VideoSource = videoSource;
            videPlayer.Start();
        }
        //关闭摄像头
        private void toolStripButton2_Click(object sender, EventArgs e)
        {
            videPlayer.SignalToStop();
            videPlayer.WaitForStop();
            cutPic.Enabled = false;
            Record.Enabled = false;
            saveData.Enabled = false;
        }
        //关闭窗口
        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            toolStripButton2_Click(null, null);
            WriteSeqNum(seqNum.ToString());//序列存盘
        }
        //截图操作
        private void cutPic_Click(object sender, EventArgs e)
        {
            Bitmap snapshot = videPlayer.GetCurrentVideoFrame();
            string img = dirc + "/" +"data_"+seqNum.ToString()+"_"+ DateTime.Now.ToString("hhmmssff") + ".jpg";
            snapshot.Save(img);
        }
        /// <summary>
        /// 每一帧的事件,保存视频帧
        /// </summary>
        private void videPlayer_NewFrame(object sender, ref Bitmap image)
        {
            if (stopRec)
            {
                stopRec = true;
                createNewFile = true;

                if (videoWriter != null)
                {
                    videoWriter.Close();
                }
            }
            else
            {
                if (createNewFile)
                {

                    createNewFile = false;
                    if (videoWriter != null)
                    {
                        videoWriter.Close();
                        videoWriter.Dispose();
                    }
                    videoWriter = new VideoFileWriter();
                    videoWriter.Open(vidirc+"/"+"data_"+seqNum.ToString()+"_"+DateTime.Now.ToString("HHmmssff") + ".avi", 640,480, 20, VideoCodec.MPEG4);

                    // add the image as a new frame of video file
                    videoWriter.WriteVideoFrame(image);
                }
                else
                {
                    videoWriter.WriteVideoFrame(image);
                }
            }
        }
        //保存录像的逻辑
        private void Record_Click(object sender, EventArgs e)
        {
            if (Record.Text == "开始录像")
            {
                stopRec = false;
                Record.Text = "停止录像";
            }
            else if (Record.Text == "停止录像")
            {
                stopRec = true;
               // seqNum++;
              //  seqShow.Text = seqNum.ToString();
                Record.Text = "开始录像";
            }
        }
        //读取序列,在窗口初始化执行
        public int ReadSeqNum()
        {
            string num;
            
            try
            {
                FileStream fs = new FileStream("./seq.sec", FileMode.Open);
                StreamReader sr = new StreamReader(fs, Encoding.Default);
                num = sr.ReadLine();

                return Convert.ToInt32(num);
            }
            catch (System.Exception ex)
            {
                MessageBox.Show("找不到顺序种子，自动创建从0开始！");
                return 0;
            }

        }
        //序列值存盘，在窗口关闭时执行
        public void WriteSeqNum(string text)
        {
            using (FileStream fs = new FileStream("./seq.sec", FileMode.Create))
            {
                StreamWriter sw = new StreamWriter(fs, Encoding.Default);
                sw.WriteLine(text);
                sw.Close();
                fs.Close();
                fs.Dispose();
            }

        }
        //遍历一个GroupBox中的RadioButton
        public int checkAny(GroupBox box)
        {
            foreach (Control c in box.Controls)
            {
                if (c is RadioButton)
                {
                    if ((c as RadioButton).Checked == true)
                    {
                        return Convert.ToInt32((c as RadioButton).Tag); 
                    }
                }
            }
            return -1;
        }
        //记录数据
        public void RecordData(string text)
        {
            using (FileStream fs = new FileStream("./Data/data.csv", FileMode.Append))
            {
                StreamWriter sw = new StreamWriter(fs, Encoding.Default);
                sw.WriteLine(text);
                sw.Close();
                fs.Close();
                fs.Dispose();
            }

        }
        //测试(废弃了)
        private void button1_Click(object sender, EventArgs e)
        {

        }
        //保存信息
        private void saveData_Click(object sender, EventArgs e)
        {
            string str = "";
            //序列,前缀,材料,颜色1,颜色2,花纹,类型,风格,季节 保存为CSV格式便于处理
            str += seqNum.ToString() + ",";
            str += "data_" + seqNum.ToString() + ",";
            str += checkAny(Material).ToString() + ",";
            str += checkAny(Color1).ToString() + ",";
            str += checkAny(Color2).ToString() + ",";
            str += checkAny(Texture).ToString() + ",";
            str += checkAny(Type).ToString() + ",";
            str += checkAny(Style).ToString() + ",";
            str += checkAny(Season).ToString();
            
            RecordData(str);
            seqNum++; //序列增长
            seqShow.Text = seqNum.ToString();
        }
    }
}
