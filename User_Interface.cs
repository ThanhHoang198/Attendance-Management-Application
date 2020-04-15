using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using AForge.Video;
using AForge.Video.DirectShow;
using System.IO;
using System.Net;
using System.Threading;
using System.Speech.Synthesis;



namespace Attendance_System
{
    public partial class User_Interface : Form
    {
        private FilterInfoCollection cameras;
        private VideoCaptureDevice cam;
        SpeechSynthesizer speech;

        public User_Interface()
        {  
            InitializeComponent();
            
            Control.CheckForIllegalCrossThreadCalls = false;
            cameras = new FilterInfoCollection(FilterCategory.VideoInputDevice);
            foreach(FilterInfo info in cameras)
            {
                cbxCam.Items.Add(info.Name);
            }
            cbxCam.SelectedIndex = 0;
                 
            cam = new VideoCaptureDevice(cameras[cbxCam.SelectedIndex].MonikerString);
            cam.VideoResolution = cam.VideoCapabilities[1];
            cam.NewFrame += Cam_NewFrame;
           
        }

        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            DialogResult ret = MessageBox.Show("Bạn có muốn thoát", "Hỏi", MessageBoxButtons.OKCancel, MessageBoxIcon.Question);
            if (ret == DialogResult.OK)
            {
                e.Cancel = false;
                cam.Stop();

            }
            else
                e.Cancel = true;
        }


        Bitmap bitmap = null;
        private void Cam_NewFrame(object sender, NewFrameEventArgs eventArgs)
        {
           bitmap=(Bitmap)eventArgs.Frame.Clone();
            ptCam.Image = bitmap;

        }
        int click = 0;
        
        private void BtnStart_Click_1(object sender,EventArgs e)
        {
            if (cam.IsRunning == false)
                cam.Start();
            if (txtName.Text == ""||txtID.Text=="")    //Name or Id is empty
            {
                MessageBox.Show("Vui Lòng Điền Đầy Đủ Thông Tin !!!");

            }
      
            else
            {
                click = click + 1;

                if(click>=6)                 //take more than 5 pics, break
                {
                    MessageBox.Show("Bạn đã chụp đủ 5 ảnh !!!");
                }
                else      
                { 

                    string B64 = ConvertImageToBase64String(bitmap);
                    string server_ip = "192.168.8.9";
                    string server_path = "http://" + server_ip + ":8000/capture?name=" + txtName.Text + "&id=" + txtID.Text + "&pic=" + click.ToString();
                    string receive = sendPOST(server_path, B64);

                    Graphics newGraphics = Graphics.FromImage(bitmap);
                   
                    String[] val = receive.Split(',');
                    // Draw it
                    Pen blackPen = new Pen(Color.Green, 6);

                        if (int.Parse(val[1]) == 0)       //Id is existed
                        {
                            MessageBox.Show("ID này đã tồn tại, vui lòng nhập ID khác");
                            click = click - 1;
                        }
                        else if (int.Parse(val[0]) == 0)   //Can't detect face
                        {
                            MessageBox.Show("Không nhận được mặt, mời bạn chụp lại" + val[0]);
                            click = click - 1;

                        }
                        else          //display saved images(display only faces)
                        {
                            // Create rectangle.
                            Rectangle rect = new Rectangle(int.Parse(val[1]), int.Parse(val[2]), int.Parse(val[3]), int.Parse(val[4]));

                            Bitmap target = new Bitmap(rect.Width, rect.Height);

                            using (Graphics g = Graphics.FromImage(target))
                            {
                                g.DrawImage(bitmap, new Rectangle(0, 0, target.Width, target.Height),
                                                rect,
                                                    GraphicsUnit.Pixel);
                                switch (click)
                                {
                                    case 1:
                                        ptImage1.Image = target;
                                        break;
                                    case 2:
                                        ptImage2.Image = target;
                                        break;
                                    case 3:
                                        ptImage3.Image = target;
                                        break;

                                    case 4:
                                        ptImage4.Image = target;
                                        break;
                                    case 5:
                                        ptImage5.Image = target;
                                        break;
                                }
                            }
                        }
                    }          
            
                }
        }

        // Convert Image to Base 64
        public static string ConvertImageToBase64String(Image image)
        {
            using (MemoryStream ms = new MemoryStream())
            {

                image.Save(ms, System.Drawing.Imaging.ImageFormat.Bmp);
                return Convert.ToBase64String(ms.ToArray());   //covert tư array ve string Ascii
            }
        }

        // Convert B64 To String
        private String EscapeData(String B64)
        {
            int B64_length = B64.Length;
            if (B64_length <= 32000)
            {
                return Uri.EscapeDataString(B64);     //nếu chuỗi nhỏ thì gửi luôn
            }


            int idx = 0;
            StringBuilder builder = new StringBuilder();   //chuỗi có thể nối vào
            String substr = B64.Substring(idx, 32000);          //chia nhỏ string thành các đoạn 3200
            while (idx < B64_length)
            {
                builder.Append(Uri.EscapeDataString(substr));
                idx += 32000;

                if (idx < B64_length)
                {

                    substr = B64.Substring(idx, Math.Min(32000, B64_length - idx));
                }

            }
            return builder.ToString();

        }

        // Send Request To Server 
        private string sendPOST(string url, string B64)
        {
            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);
            var postData = "image=" + EscapeData(B64);
           
            var data = Encoding.ASCII.GetBytes(postData);
            request.Method = "POST";
            request.ContentLength = data.Length;
            request.ContentType = "application/x-www-form-urlencoded";
            using (var stream = request.GetRequestStream())
            {
                stream.Write(data, 0, data.Length);
            }
            HttpWebResponse response = (HttpWebResponse)request.GetResponse();
            var responseString = new StreamReader(response.GetResponseStream()).ReadToEnd();
            return responseString;


        }
        private string sendGet(string url)
        {
            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);
            HttpWebResponse response = (HttpWebResponse)request.GetResponse();
            using (Stream datastream = response.GetResponseStream() )
            using (StreamReader reader = new StreamReader(datastream))
                { 
                   return reader.ReadToEnd();
                }
        }

        private void BtnClear_Click(object sender, EventArgs e)
        {
            txtID.Text = "";
            txtName.Text = "";
            ptImage1.Image = null;
            ptImage2.Image = null;
            ptImage3.Image = null;
            ptImage4.Image = null;
            ptImage5.Image = null;
            ptCam.Image = bitmap;
            click = 0;
            ProgressPercent.Value = 0;
            lblStatus.Text = "Adding information...";
            lblProgress.Text = "";
            if (cam.IsRunning == false)
                cam.Start();

        }

        public void train()
        {
            ProgressPercent.Value = 0;
            string server_ip = "192.168.8.9";
            string server_path = "http://" + server_ip + ":8000/train";
            string receive = sendGet(server_path);
            txtID.Text = (receive);
        }

       
        private void BtnTrain_Click(object sender, EventArgs e)
        {
            if (cam.IsRunning)
                cam.Stop();
            Thread thr1 = new Thread(train);
            thr1.Start();
            backgroundWorker1.RunWorkerAsync();  

        }

     
        // Check data training progress and update to user interface
        int k = 0;
        public void check()
        {
                 
            lblStatus.Text = "Training...";
            while (k <=100)
            {
                Thread.Sleep(100);
                string server_ip = "192.168.8.9";
                string server_path = "http://" + server_ip + ":8000/progress";
                string receive = sendGet(server_path);
                k = int.Parse(receive) ;
                lblProgress.Text = k.ToString() + " %";
                backgroundWorker1.ReportProgress(k, k);
                
               

                if (k == 100)
                {
                    k = 0;

                    break;
                }
               
            }
            if (k > 100)
                MessageBox.Show("WARNING");

        }

        //Display checking progress 
        private void BackgroundWorker1_DoWork(object sender, DoWorkEventArgs e)
        {
            check();
           
        }

        private void BackgroundWorker1_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            ProgressPercent.Value = e.ProgressPercentage;

        }

        private void BackgroundWorker1_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            MessageBox.Show("Trained successfully !");

        }



        private void BtnRecog_Click(object sender, EventArgs e)
        {   if(!cam.IsRunning)
                cam.Start();
            timer1.Start();
           
        }

        private void Timer1_Tick(object sender, EventArgs e)
        {

            string B64 = ConvertImageToBase64String(bitmap);

            cam.Stop();
            string server_ip = "192.168.8.9";
            string server_path = "http://" + server_ip + ":8000/detect";
            string receive = sendPOST(server_path, B64);
          
            String[] items = receive.Split('|');
            if (items[0] != "")
            {
                String[] val = items[0].Split(',');
                txtID.Text = val[0].Split('_')[0];
                txtName.Text = val[0].Split('_')[1];
                Pen blackPen = new Pen(Color.Green, 6);
                Graphics newGraphics = Graphics.FromImage(bitmap);

                // Create rectangle.
                Rectangle rect = new Rectangle(int.Parse(val[1]), int.Parse(val[2]), int.Parse(val[3]), int.Parse(val[4]));
                // Draw rectangle to screen.
                newGraphics.DrawRectangle(blackPen, rect);
                newGraphics.DrawString(val[0].Split('_')[1], new Font("Tahoma", 25), Brushes.Yellow, int.Parse(val[1]), int.Parse(val[2]) - 50);
                
                speech = new SpeechSynthesizer();
                speech.SpeakAsync("Hello" + (val[0].Split('_')[1]));
                ptCam.Image = bitmap;
                timer1.Stop();





            }

        }

        private void PtCam_Click(object sender, EventArgs e)
        {
            cam.Start();
        }
    }
}
