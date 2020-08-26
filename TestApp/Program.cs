using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Tesseract;

namespace TestApp {
    class Program {
        [STAThread]
        static void Main(string[] args) {

            // OCRを行うオブジェクトの生成
            //  言語データの場所と言語名を引数で指定する
            var tesseract = new TesseractEngine(
                @"C:\Program Files\Tesseract-OCR\tessdata", // 言語ファイルを「C:\tessdata」に置いた場合
                "jpn");         // 英語なら"eng" 「○○.traineddata」の○○の部分

            // 画像ファイルの読み込み
            var img = new System.Drawing.Bitmap(@"C:\Tools\test.png");

            // OCRの実行と表示
            var page = tesseract.Process(img);
            var ttt = page.GetText();





            OcrClassLibrary.OcrClassLibrary lib = new OcrClassLibrary.OcrClassLibrary();

            //string text = lib.OcrFromClipBoard("ja-JP");

            string text = lib.OcrFromImage(@"C:\Tools\test.png", "ja-JP");
        }
    }
}
