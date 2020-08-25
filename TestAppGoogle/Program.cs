using Google.Apis.Services;
using Google.Apis.Vision.v1;
using Google.Apis.Vision.v1.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace TestAppGoogle {
    class Program {
        static void Main(string[] args) {
            var credential = Google.Apis.Auth.OAuth2.GoogleCredential.FromFile(@"C:\Users\tsutsumi\Downloads\try-apis-8b2095f28b0e.json");
            credential = credential.CreateScoped(new[] { VisionService.Scope.CloudPlatform });


            var visionService = new VisionService(new BaseClientService.Initializer {
                HttpClientInitializer = credential,
                GZipEnabled = false
            });

            //ファイルを開く
            System.IO.FileStream fs = new System.IO.FileStream(
                @"C:\Tools\test.png",
                System.IO.FileMode.Open,
                System.IO.FileAccess.Read);
            //ファイルを読み込むバイト型配列を作成する
            byte[] bs = new byte[fs.Length];
            //ファイルの内容をすべて読み込む
            fs.Read(bs, 0, bs.Length);
            //閉じる
            fs.Close();

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

            string text = "";
            int result = DetectTextWord(visionService, bs, ref text);

        }
        static public byte[] ImageToByte(System.Drawing.Image img) {
            System.Drawing.ImageConverter converter = new System.Drawing.ImageConverter();
            return (byte[])converter.ConvertTo(img, typeof(byte[]));
        }

        static public byte[] BitmapToByteArray(System.Drawing.Bitmap bmp) {
            System.Drawing.Rectangle rect = new System.Drawing.Rectangle(0, 0, bmp.Width, bmp.Height);
            System.Drawing.Imaging.BitmapData bmpData =
                bmp.LockBits(rect, System.Drawing.Imaging.ImageLockMode.ReadWrite,
                System.Drawing.Imaging.PixelFormat.Format32bppArgb);

            // Bitmapの先頭アドレスを取得
            IntPtr ptr = bmpData.Scan0;

            // 32bppArgbフォーマットで値を格納
            int bytes = bmp.Width * bmp.Height * 4;
            byte[] rgbValues = new byte[bytes];

            // Bitmapをbyte[]へコピー
            Marshal.Copy(ptr, rgbValues, 0, bytes);

            bmp.UnlockBits(bmpData);
            return rgbValues;
        }

        static private int DetectTextWord(VisionService vision, byte[] getImage, ref string FullText) {
            int result = 1;
            Console.WriteLine("Detecting image to texts...");
            // Convert image to Base64 encoded for JSON ASCII text based request
            string imageContent = Convert.ToBase64String(getImage);

            try {
                // Post text detection request to the Vision API
                var responses = vision.Images.Annotate(
                    new BatchAnnotateImagesRequest() {
                        Requests = new[]
                        {
                          new AnnotateImageRequest()
                          {
                            Features = new []
                            { new Feature()
                              {
                                Type = "TEXT_DETECTION"
                              }
                            },
                            Image = new Image()
                            {
                              Content = imageContent
                            }
                          }
                        }
                    }).Execute();

                if (responses.Responses != null) {
                    FullText = responses.Responses[0].TextAnnotations[0].Description;

                    Console.WriteLine("SUCCESS：Cloud Vision API Access.");
                    result = 0;
                }
                else {
                    FullText = "";
                    Console.WriteLine("ERROR : No text found.");
                    result = -1;
                }
            }
            catch {
                FullText = "";
                Console.WriteLine("ERROR : Not Access Cloud Vision API.");
                result = -1;
            }

            return result;
        }

    }
}
