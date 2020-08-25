using BeltManagementTool.Entity;
using BeltManagementTool.OCR;
using Google.Apis.Services;
using Google.Apis.Vision.v1;
using Google.Apis.Vision.v1.Data;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace BeltManagementTool {
    public partial class Form1 : Form {

        private static readonly log4net.ILog logger = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        System.Drawing.Bitmap captureImage = null;
        GoogleVisionApiOCR ocr = null;

        string settingFileName = "setting.txt";
        string replaceWordFileName = "replaceWord.txt";

        Dictionary<string, string> replaceWordDic = new Dictionary<string, string>();

        public Form1() {
            InitializeComponent();

            if(File.Exists(settingFileName)) {
                string[] settingValue = File.ReadAllText(settingFileName).Split(new char[] { ',' });
                numericUpDown1.Value = int.Parse(settingValue[0]);
                numericUpDown2.Value = int.Parse(settingValue[1]);
                numericUpDown3.Value = int.Parse(settingValue[2]);
                numericUpDown4.Value = int.Parse(settingValue[3]);
            }

            if(File.Exists(replaceWordFileName)) {
                string[] replaceWords = File.ReadAllLines(replaceWordFileName);
                for(int i = 0; i < replaceWords.Length; i++) {
                    string[] words = replaceWords[i].Split(new string[] { "!_!" }, StringSplitOptions.RemoveEmptyEntries);
                    replaceWordDic[words[0]] = words[1];
                }
            }

            ocr = new GoogleVisionApiOCR(@"C:\Users\tsutsumi\Downloads\try-apis-8b2095f28b0e.json");
        }

        private void outputReplaceWords() {
            StringBuilder sb = new StringBuilder();
            foreach(string key in replaceWordDic.Keys) {
                sb.Append(key);
                sb.Append("!_!");
                sb.Append(replaceWordDic[key]);
                sb.Append(Environment.NewLine);
            }

            File.WriteAllText(replaceWordFileName, sb.ToString());
        }

        private BeltEntity GetOCRTest() {
            string text = ocr.GetTextFromImage(captureImage).Replace("\n", "\r\n");
            foreach(string key in replaceWordDic.Keys) {
                text = text.Replace(key, replaceWordDic[key]);
            }
            logTextBox.Text = text;

            logger.Info("読み取り結果：" + Environment.NewLine + logTextBox.Text);

            return new BeltEntity(text);
        }

        private void button1_Click(object sender, EventArgs e) {
            screenCapture();
            BeltEntity entity = GetOCRTest();
        }


        private void Form1_Load(object sender, EventArgs e) {

        }

        private void button2_Click(object sender, EventArgs e) {
            if(numericUpDown1.Value != 0) {
                numericUpDown1.Value -= 1;
            }
            outputSetting();
        }

        private void button4_Click(object sender, EventArgs e) {
            numericUpDown1.Value += 1;
            outputSetting();
        }

        private void button3_Click(object sender, EventArgs e) {
            if (numericUpDown2.Value != 0) {
                numericUpDown2.Value -= 1;
            }
            outputSetting();
        }

        private void button5_Click(object sender, EventArgs e) {
            numericUpDown2.Value += 1;
            outputSetting();
        }

        private void screenCapture() {
            this.SendToBack();

            // スクリーンショット
            captureImage = new System.Drawing.Bitmap((int)numericUpDown3.Value, (int)numericUpDown4.Value);
            //Graphicsの作成
            Graphics g = Graphics.FromImage(captureImage);
            //画面全体をコピーする
            g.CopyFromScreen(new Point((int)numericUpDown1.Value, (int)numericUpDown2.Value), new Point(0, 0), captureImage.Size);
            //解放
            g.Dispose();

            //表示
            pictureBox1.SizeMode = PictureBoxSizeMode.StretchImage;
            pictureBox1.Image = captureImage;

            this.TopMost = true;
            this.TopMost = false;
        }

        private void numericUpDown_ValueChanged(object sender, EventArgs e) {
            screenCapture();
            outputSetting();
        }

        private void outputSetting() {
            File.WriteAllText(settingFileName, numericUpDown1.Value + "," + numericUpDown2.Value + "," + numericUpDown3.Value + "," + numericUpDown4.Value);
        }

        private void button6_Click(object sender, EventArgs e) {
            timer1.Interval = (int)numericUpDown5.Value * 1000;
            timer1.Enabled = true;
            button1.Enabled = false;
            button6.Enabled = false;
            button7.Enabled = true;
        }

        private void timer1_Tick(object sender, EventArgs e) {
            timer1.Enabled = false;
            screenCapture();
            BeltEntity entity = GetOCRTest();
            timer1.Enabled = true;
        }

        private void button7_Click(object sender, EventArgs e) {
            timer1.Enabled = false;
            button1.Enabled = true;
            button6.Enabled = true;
            button7.Enabled = false;
        }

        private void button8_Click(object sender, EventArgs e) {
            captureImage = (Bitmap)System.Drawing.Image.FromFile(@"C:\Test\sample1.bmp");
            BeltEntity entity = GetOCRTest();
        }

        private void button10_Click(object sender, EventArgs e) {
            string str = Microsoft.VisualBasic.Interaction.InputBox("入力してください", "ユーザー登録", default, 300, 400);
            if(str.Length > 0) {
                listBox1.Items.Add(str);
            }
        }

        private void button11_Click(object sender, EventArgs e) {
            if(listBox1.SelectedIndex != -1) {
                if(MessageBox.Show("削除していいですか？", "確認", MessageBoxButtons.OKCancel, MessageBoxIcon.Question) == DialogResult.OK) {
                    listBox1.Items.RemoveAt(listBox1.SelectedIndex);
                }
            }
        }

        private void button9_Click(object sender, EventArgs e) {
            logTextBox.Text = logTextBox.Text.Replace(textBox1.Text, textBox2.Text);
            replaceWordDic[textBox1.Text] = textBox2.Text;
            outputReplaceWords();
        }
    }
}
