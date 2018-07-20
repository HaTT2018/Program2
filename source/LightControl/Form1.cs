using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Microsoft.CognitiveServices.Speech;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Net.Http.Headers;
using Microsoft.Scripting.Utils;

namespace search_image
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
            button1.Text = "start";
        }

        // 语音识别器
        SpeechRecognizer recognizer;
        bool isRecording = false;
        string text;
        public static string folder_path;

        private void textBox3_TextChanged(object sender, EventArgs e)
        {
            folder_path = textBox3.Text;
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            try
            {
                // 第一步
                // 初始化语音服务SDK并启动识别器，进行语音转文本
                // 密钥和区域可在 https://azure.microsoft.com/zh-cn/try/cognitive-services/my-apis/?api=speech-services 中找到
                // 密钥示例: 5ee7ba6869f44321a40751967accf7a9
                // 区域示例: westus
                SpeechFactory speechFactory = SpeechFactory.FromSubscription("2caea042ef27427189fd56be10425585", "westus");

                // 识别中文
                recognizer = speechFactory.CreateSpeechRecognizer("en-US");
                //等待识别中
                recognizer.IntermediateResultReceived += Recognizer_IntermediateResultReceived;
                // 识别的最终结果
                recognizer.FinalResultReceived += Recognizer_FinalResultReceived;
                // 出错时的处理
                recognizer.RecognitionErrorRaised += Recognizer_RecognitionErrorRaised;
            }
            catch (Exception ex)
            {
                if (ex is System.TypeInitializationException)
                {
                    Log("语音SDK不支持Any CPU, 请更改为x64");
                }
                else
                {
                    Log("初始化出错，请确认麦克风工作正常");
                    Log("已降级到文本语言理解模式");

                    TextBox inputBox = new TextBox();
                    inputBox.Text = "";
                    inputBox.Size = new Size(300, 26);
                    inputBox.Location = new Point(10, 10);
                    inputBox.KeyDown += inputBox_KeyDown;
                    Controls.Add(inputBox);

                    button1.Visible = false;
                }
            }
        }

        private async void button1_Click(object sender, EventArgs e)
        //以下代码已经实现了识别语音内容（中文），现在应该把语音内容与照片数据集中的caption注解进行匹配，
        //将结果显示在picturebox中，同时在最右边的textbox2中按照匹配率排序，显示各个图片的图片名（**.jpg|png|...）
        //最后能实现点一个名字，显示一张图片的功能（我不确定是不是应该用textbox，或许是用listbox或者checkbox…）
        {
            if(textBox3.Text=="")
            {
                MessageBox.Show("Please input the folder_path!");
            }
            else
            {
                button1.Enabled = false;
                isRecording = !isRecording;
                if (isRecording)
                {
                    // 启动识别器
                    await recognizer.StartContinuousRecognitionAsync();
                    button1.Text = "stop";
                }
                else
                {
                    // 停止识别器
                    await recognizer.StopContinuousRecognitionAsync();
                    button1.Text = "start";
                }

                button1.Enabled = true;
            }
        }

        //等待识别中
        private void Recognizer_IntermediateResultReceived(object sender, SpeechRecognitionResultEventArgs e)
        {
            if (!string.IsNullOrEmpty(e.Result.Text))
            {
                Log("recognizing...");
            }
        }
        // 识别的最终结果
        private void Recognizer_FinalResultReceived(object sender, SpeechRecognitionResultEventArgs e)
        {
            if (!string.IsNullOrEmpty(e.Result.Text))
            {
                Log("final result: " + e.Result.Text);
                text = e.Result.Text;
                showpicture(folder_path + "\\Imagedata\\", text.ToLower());
            }
        }

        // 出错时的处理
        private void Recognizer_RecognitionErrorRaised(object sender, RecognitionErrorEventArgs e)
        {
            Log("error: " + e.FailureReason);
        }


        #region 界面操作

        private void Log(string message)
        {
            MakesureRunInUI(() =>
            {
                textBox1.AppendText(message + "\r\n");
            });
        }


        private void MakesureRunInUI(Action action)
        {
            if (InvokeRequired)
            {
                MethodInvoker method = new MethodInvoker(action);
                Invoke(action, null);
            }
            else
            {
                action();
            }
        }

        #endregion

        private void inputBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter && sender is TextBox)
            {
                TextBox textBox = sender as TextBox;
                e.Handled = true;
                Log(textBox.Text);
                textBox.Text = string.Empty;
            }
        }


        private void showpicture(String Path, String text) //匹配算法
        {
            Dictionary<string, int> score = new Dictionary<string, int>();//保存每张图片分数
            List<String> list = new List<string>();
            DirectoryInfo theFolder = new DirectoryInfo(Path);
            FileInfo[] thefileInfo = theFolder.GetFiles("*.*", SearchOption.TopDirectoryOnly);
            foreach (FileInfo NextFile in thefileInfo)  //获得文件夹下全部文件，名称存到list里
            {
                list.Add(NextFile.FullName);
                score.Add(NextFile.FullName, 0);
            }
            for (int a = 0; a < list.Count; a++)
            {
                string content = System.IO.File.ReadAllText(@list[a]);
                dynamic result = JsonConvert.DeserializeObject<dynamic>(content); //将string转化为json
                int size = 0;
                int res = 0;

                size = result.description.tags.Count;//匹配细节，若有加1分
                for (int i = 0; i < size; i++)
                {
                    string miad = ((string)result.description.tags[i]).Replace("{", "");
                    miad = miad.Replace("}", "");
                    res = KMP(miad, text);
                    if (res != -1) score[list[a]]++;
                }
            }
            //对list进行排序，匹配度最大的index最小
            for (int j = 0; j < list.Count - 1; j++)
            {
                for (int i = 0; i < list.Count - 1 - j; i++)
                {
                    if (score[list[i]] < score[list[i + 1]])
                    {
                        String temp = list[i];
                        list[i] = list[i + 1];
                        list[i + 1] = temp;
                    }
                }
            }
            System.Windows.Forms.Control.CheckForIllegalCrossThreadCalls = false;
            listBox1.Items.Clear();
            string temp_name;
            foreach (string file_name in list)
            {
                temp_name = file_name.Replace((folder_path + "\\Imagedata\\"), "");
                temp_name = temp_name.Substring(0, temp_name.Length - 4) + ".jpg";
                listBox1.Items.Add(temp_name);
            }
            //输出匹配度最大图片
            if (score[list[0]] == 0)
            {
                pictureBox1.Load(folder_path+"\\notfound\\notfound.jpg");
            }
            else
            {
                int k = 0;
                for (int i = 0; i < list.Count; i++)//若多张照片分数相同，随机输出一张
                {
                    if (score[list[0]] == score[list[i]]) k++;
                    else break;
                }
                Random num = new Random();
                int a = num.Next(0, k);
                string mid = list[a].Replace(folder_path+"\\Imagedata\\", "");
                string finalpath = folder_path+"\\Pictures\\" + mid;
                finalpath = finalpath.Substring(0, finalpath.Length - 4) + ".jpg";
                pictureBox1.Load(finalpath);
            }
        }
        static int KMP(string pad, string patd)//排序算法
        {
            pad = pad.Trim();
            char[] C_pad = pad.ToCharArray();
            char[] C_patd = patd.ToCharArray();

            //分别得到匹配字符串的长度
            int a = patd.Length;
            int b = pad.Length;
            if (patd.Contains(pad))
            {
                int offset = 0;
                int sum = 0;   //匹配计数
                int limit = 0;  //界限
                int i = 0;  //控制C-pad的数组下标
                int j = 0;  //控制C_patd的数组下标
                int s = 0;  //控制C-pad开始搜索的起始索引
                do
                {     //匹配上
                    if (C_pad[i] == C_patd[j])
                    {
                        i++;
                        j++;
                        sum++;
                        limit++;
                    }
                    else
                    {
                        j++;
                        i = 0;
                    }
                    offset++;
                    if (i == b)
                    {
                        s = offset - b;
                        break;
                    }
                } while (true);
                return s;
            }
            else
            {
                return -1;
            }
        }
        private void button2_Click(object sender, EventArgs e)
        {
            //导入图片按钮应该链接一个装有很多图片的文件夹，并在最右边的textbox中显示这些文件的名称
            //然后同时再调用图像识别的代码，将图片与其caption一一对应
            //最后生成一个数据集（库），保存在本地或者内存中（不知道这么说对不对）
            ////////////
            if (textBox3.Text == "")
            {
                MessageBox.Show("Please input the folder path!");
            }
            else
            {
                Log("image importing...");
                Program2.readfile();
                Log("all of the image have been imported!");

                string Path;
                Path = folder_path + "\\Pictures";
                List<String> list = new List<string>();
                DirectoryInfo theFolder = new DirectoryInfo(Path);
                FileInfo[] thefileInfo = theFolder.GetFiles("*.*", SearchOption.TopDirectoryOnly);
                string temp_name;
                foreach (FileInfo NextFile in thefileInfo)  //获得文件夹下全部文件，名称存到list里
                {
                    temp_name = NextFile.FullName.Replace((folder_path + "\\Pictures\\"), "");
                    temp_name = temp_name.Substring(0, temp_name.Length - 4) + ".jpg";
                    listBox1.Items.Add(temp_name);
                }
            }
        }
        
        private void listBox1_MouseDown(object sender, MouseEventArgs e)
        {
            pictureBox1.Image = Image.FromFile(folder_path + "\\Pictures\\" + listBox1.Text);
        }
    }
}
