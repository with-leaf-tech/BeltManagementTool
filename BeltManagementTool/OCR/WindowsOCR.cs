using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BeltManagementTool.OCR {
    public class WindowsOCR : OcrBase {
        OcrClassLibrary.OcrClassLibrary lib = null;

        public WindowsOCR() {
            
        }

        public override void initialize(string accessKey) {
            lib = new OcrClassLibrary.OcrClassLibrary();
        }

        public override string GetTextFromImage(System.Drawing.Bitmap bitmap, string baseLang) {
            string returnText = lib.OcrFromBitmap(bitmap, baseLang);
            return returnText;
        }

    }
}
