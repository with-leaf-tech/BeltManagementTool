using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BeltManagementTool.OCR {
    public class TesseractOCR : OcrBase {
        Tesseract.TesseractEngine tesseract = null;

        public TesseractOCR() {

        }

        [STAThread]
        public override void initialize(string accessKey) {
            tesseract = new Tesseract.TesseractEngine(@"C:\Users\tsutsumi\source\repos\with-leaf-tech\BeltManagementTool\tesseract-ocr\tessdata", "jpn");
        }

        [STAThread]
        public override string GetTextFromImage(System.Drawing.Bitmap bitmap, string baseLang) {
            /*
            var img = new System.Drawing.Bitmap(@"C:\Tools\test2.png");

            // OCRの実行と表示
            var page2 = tesseract.Process(img);
            var ttt = page2.GetText();
            page2.Dispose();
            */
            // OCRの実行と表示
            Tesseract.Page page = tesseract.Process(bitmap);
            string retText = page.GetText();
            page.Dispose();

            return retText;
            //return "";
        }
    }
}
