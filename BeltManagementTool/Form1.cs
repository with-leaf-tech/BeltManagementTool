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
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace BeltManagementTool {
    public partial class Form1 : Form {

        System.Drawing.Bitmap captureImage = null;
        GoogleVisionApiOCR ocr = null;

        public Form1() {
            InitializeComponent();

            ocr = new GoogleVisionApiOCR(@"C:\Users\tsutsumi\Downloads\try-apis-8b2095f28b0e.json");
        }


        private BeltEntity GetOCRTest() {
            string text = ocr.GetTextFromImage(captureImage);
            return new BeltEntity(text);
        }

        private void button1_Click(object sender, EventArgs e) {
            BeltEntity entity = GetOCRTest();
            /*
            var credential = Google.Apis.Auth.OAuth2.GoogleCredential.FromFile(@"C:\Users\tsutsumi\Downloads\try-apis-8b2095f28b0e.json");
            credential = credential.CreateScoped(new[] { VisionService.Scope.CloudPlatform });


            var visionService = new VisionService(new BaseClientService.Initializer {
                HttpClientInitializer = credential,
                GZipEnabled = false
            });
            */
            /*
            //ファイルを開く
            System.IO.FileStream fs = new System.IO.FileStream(
                @"C:\Users\tsutsumi\Downloads\e89239da.jpg",
                System.IO.FileMode.Open,
                System.IO.FileAccess.Read);
            //ファイルを読み込むバイト型配列を作成する
            byte[] bs = new byte[fs.Length];
            //ファイルの内容をすべて読み込む
            fs.Read(bs, 0, bs.Length);
            //閉じる
            fs.Close();
            */

            /*
            // スクリーンショット
            System.Drawing.Bitmap bmp = new System.Drawing.Bitmap(300, 400); // 取り込むサイズ
            System.Drawing.Graphics g = System.Drawing.Graphics.FromImage(bmp);
            //画面全体をコピーする
            g.CopyFromScreen(new System.Drawing.Point(0, 0), new System.Drawing.Point(0, 0), bmp.Size);
            byte[] bs = ImageToByte(bmp);
            //解放
            g.Dispose();
            */
            /*
            byte[] bs = ImageToByte(captureImage);
            string text = "";
            int result = DetectTextWord(visionService, bs, ref text);
            */



















            /*
            //Bitmapの作成
            Bitmap bmp = new Bitmap(30, 40); // 取り込むサイズ
            Point leftTopPoint = new Point(1200, 0); // 画面上の左上の位置

            // デバッグ用画像
            bmp = (Bitmap)System.Drawing.Image.FromFile(@"C:\Users\tsutsumi\Downloads\e89239da.jpg");

            //OcrClassLibrary.OcrClassLibrary lib = new OcrClassLibrary.OcrClassLibrary();
            //string text = lib.OcrFromImage(@"C:\Users\tsutsumi\Downloads\e89239da.jpg", "en-US");
            */
            /*
            // スクリーンショット
            //Graphicsの作成
            Graphics g = Graphics.FromImage(bmp);
            //画面全体をコピーする
            g.CopyFromScreen(leftTopPoint, new Point(0, 0), bmp.Size);
            //解放
            g.Dispose();
            */
            /*
            //表示
            pictureBox1.SizeMode = PictureBoxSizeMode.StretchImage;
            pictureBox1.Image = bmp;


            // OCRを行うオブジェクトの生成
            //  言語データの場所と言語名を引数で指定する
            var tesseract = new Tesseract.TesseractEngine(
                @"C:\Program Files\Tesseract-OCR\tessdata", // 言語ファイルを「C:\tessdata」に置いた場合
                "jpn");         // 英語なら"eng" 「○○.traineddata」の○○の部分

            // 画像ファイルの読み込み
            var img = new System.Drawing.Bitmap(@"C:\Users\tsutsumi\Downloads\e89239da.jpg");
            // OCRの実行と表示
            var page = tesseract.Process(img);
            System.Console.Write(page.GetText());
            */


        }


        private void Form1_Load(object sender, EventArgs e) {

        }

        private void button2_Click(object sender, EventArgs e) {
            if(numericUpDown1.Value != 0) {
                numericUpDown1.Value -= 1;
            }
        }

        private void button4_Click(object sender, EventArgs e) {
            numericUpDown1.Value += 1;
        }

        private void button3_Click(object sender, EventArgs e) {
            if (numericUpDown2.Value != 0) {
                numericUpDown2.Value -= 1;
            }
        }

        private void button5_Click(object sender, EventArgs e) {
            numericUpDown2.Value += 1;
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
            //BeltEntity entity = GetOCRTest();
            timer1.Enabled = true;
        }

        private void button7_Click(object sender, EventArgs e) {
            timer1.Enabled = false;
            button1.Enabled = true;
            button6.Enabled = true;
            button7.Enabled = false;
        }
    }
}
